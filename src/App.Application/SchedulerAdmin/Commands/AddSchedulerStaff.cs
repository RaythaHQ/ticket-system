using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class AddSchedulerStaff
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid UserId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required.");

            RuleFor(x => x.UserId)
                .MustAsync(
                    async (userId, cancellationToken) =>
                    {
                        return await db.Users.AnyAsync(
                            u => u.Id == userId.Guid && u.IsAdmin && u.IsActive,
                            cancellationToken
                        );
                    }
                )
                .WithMessage("User must be an active admin.");

            RuleFor(x => x.UserId)
                .MustAsync(
                    async (userId, cancellationToken) =>
                    {
                        return !await db.SchedulerStaffMembers.AnyAsync(
                            s => s.UserId == userId.Guid,
                            cancellationToken
                        );
                    }
                )
                .WithMessage("This user is already a scheduler staff member.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = new SchedulerStaffMember
            {
                UserId = request.UserId.Guid,
                CanManageOthersCalendars = false,
                IsActive = true,
                AvailabilityJson = null, // defaults to org-wide hours
                CoverageZonesJson = null, // defaults to org-wide coverage zones
            };

            _db.SchedulerStaffMembers.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
