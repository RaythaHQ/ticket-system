using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Teams.Commands;

public class UpdateTeam
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public bool RoundRobinEnabled { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x)
                .MustAsync(async (cmd, cancellationToken) =>
                {
                    var exists = await db.Teams.AsNoTracking()
                        .AnyAsync(t => t.Name.ToLower() == cmd.Name.ToLower() && t.Id != cmd.Id.Guid, cancellationToken);
                    return !exists;
                })
                .WithMessage("A team with this name already exists.");
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

            team.Name = request.Name;
            team.Description = request.Description;
            team.RoundRobinEnabled = request.RoundRobinEnabled;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(team.Id);
        }
    }
}

