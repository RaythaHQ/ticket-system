using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class DeleteTicketPriority
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x)
                .CustomAsync(
                    async (request, context, cancellationToken) =>
                    {
                        var priority = await db
                            .TicketPriorityConfigs.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

                        if (priority == null)
                            throw new NotFoundException("Ticket Priority", request.Id);

                        if (priority.IsBuiltIn)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot delete a built-in priority."
                            );
                            return;
                        }

                        if (priority.IsDefault)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot delete the default priority. Please set another priority as default first."
                            );
                            return;
                        }

                        // Check if any tickets are using this priority
                        var ticketsUsingPriority = await db.Tickets
                            .AsNoTracking()
                            .AnyAsync(t => t.Priority == priority.DeveloperName, cancellationToken);

                        if (ticketsUsingPriority)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Tickets are currently assigned to this priority. Please change them to a different priority before deleting."
                            );
                            return;
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketConfigService _configService;

        public Handler(IAppDbContext db, ITicketConfigService configService)
        {
            _db = db;
            _configService = configService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var priority = await _db.TicketPriorityConfigs.FirstAsync(
                p => p.Id == request.Id.Guid,
                cancellationToken
            );

            _db.TicketPriorityConfigs.Remove(priority);
            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}

