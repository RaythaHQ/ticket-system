using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Teams.Commands;

public class ToggleRoundRobin
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public bool Enabled { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageTeams();

            var team = await _db.Teams
                .FirstOrDefaultAsync(t => t.Id == request.Id.Guid, cancellationToken);

            if (team == null)
                throw new NotFoundException("Team", request.Id);

            team.RoundRobinEnabled = request.Enabled;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(team.Id);
        }
    }
}

