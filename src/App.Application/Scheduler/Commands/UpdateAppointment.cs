using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Commands;

public class UpdateAppointment
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long AppointmentId { get; init; }
        public string? Notes { get; init; }
        public string? MeetingLink { get; init; }

        // Per-appointment contact fields
        public string? ContactFirstName { get; init; }
        public string? ContactLastName { get; init; }
        public string? ContactEmail { get; init; }
        public string? ContactPhone { get; init; }
        public string? ContactAddress { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.AppointmentId)
                .GreaterThan(0)
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        return await db
                            .Appointments.AsNoTracking()
                            .AnyAsync(a => a.Id == id, cancellationToken);
                    }
                )
                .WithMessage("Appointment not found.");

            // Appointment must not be in a terminal status
            RuleFor(x => x.AppointmentId)
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        var appointment = await db
                            .Appointments.AsNoTracking()
                            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

                        if (appointment == null)
                            return true; // Already caught by existence check

                        return !appointment.StatusValue.IsTerminal;
                    }
                )
                .WithMessage("Cannot edit an appointment in a terminal status.");

            // If mode is virtual, meeting link must remain present
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        var appointment = await db
                            .Appointments.AsNoTracking()
                            .FirstOrDefaultAsync(
                                a => a.Id == cmd.AppointmentId,
                                cancellationToken
                            );

                        if (appointment == null)
                            return true;

                        if (appointment.Mode == AppointmentMode.VIRTUAL)
                        {
                            // MeetingLink was explicitly provided (even if empty) or we keep the old one
                            // If MeetingLink is null in the command, we don't clear it
                            // If MeetingLink is empty string, that's invalid for virtual
                            if (cmd.MeetingLink != null && string.IsNullOrWhiteSpace(cmd.MeetingLink))
                                return false;
                        }

                        return true;
                    }
                )
                .WithMessage(
                    "Meeting link cannot be cleared for virtual appointments."
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ISchedulerPermissionService permissionService
        )
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var appointment = await _db.Appointments.FirstOrDefaultAsync(
                a => a.Id == request.AppointmentId,
                cancellationToken
            );

            if (appointment == null)
                throw new NotFoundException("Appointment", request.AppointmentId);

            if (appointment.StatusValue.IsTerminal)
                throw new BusinessException(
                    "Cannot edit an appointment in a terminal status."
                );

            // Track changes for history
            var changes = new List<string>();

            if (request.Notes != null && request.Notes != appointment.Notes)
            {
                appointment.Notes = request.Notes;
                changes.Add("notes");
            }

            if (request.MeetingLink != null && request.MeetingLink != appointment.MeetingLink)
            {
                appointment.MeetingLink = request.MeetingLink;
                changes.Add("meeting link");
            }

            // Per-appointment contact fields
            if (request.ContactFirstName != null && request.ContactFirstName != appointment.ContactFirstName)
            {
                appointment.ContactFirstName = request.ContactFirstName;
                changes.Add("contact first name");
            }

            if (request.ContactLastName != null && request.ContactLastName != appointment.ContactLastName)
            {
                appointment.ContactLastName = request.ContactLastName;
                changes.Add("contact last name");
            }

            if (request.ContactEmail != null && request.ContactEmail != appointment.ContactEmail)
            {
                appointment.ContactEmail = request.ContactEmail;
                changes.Add("contact email");
            }

            if (request.ContactPhone != null && request.ContactPhone != appointment.ContactPhone)
            {
                appointment.ContactPhone = request.ContactPhone;
                changes.Add("contact phone");
            }

            if (request.ContactAddress != null && request.ContactAddress != appointment.ContactAddress)
            {
                appointment.ContactAddress = request.ContactAddress;
                changes.Add("contact address");
            }

            if (changes.Any())
            {
                var history = new AppointmentHistory
                {
                    AppointmentId = appointment.Id,
                    ChangeType = "edited",
                    NewValue = $"Updated: {string.Join(", ", changes)}",
                    ChangedByUserId = _currentUser.UserId!.Value.Guid,
                };
                _db.AppointmentHistories.Add(history);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(appointment.Id);
        }
    }
}
