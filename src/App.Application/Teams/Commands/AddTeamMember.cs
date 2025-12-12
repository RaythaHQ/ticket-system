using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Teams.Commands;

public class AddTeamMember
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid TeamId { get; init; }
        public ShortGuid StaffAdminId { get; init; }
        public bool IsAssignable { get; init; } = true;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.TeamId)
                .MustAsync(async (teamId, cancellationToken) =>
                {
                    return await db.Teams.AsNoTracking().AnyAsync(t => t.Id == teamId.Guid, cancellationToken);
                })
                .WithMessage("Team not found.");

            RuleFor(x => x.StaffAdminId)
                .MustAsync(async (userId, cancellationToken) =>
                {
                    return await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId.Guid, cancellationToken);
                })
                .WithMessage("User not found.");

            RuleFor(x => x)
                .MustAsync(async (cmd, cancellationToken) =>
                {
                    var exists = await db.TeamMemberships.AsNoTracking()
                        .AnyAsync(m => m.TeamId == cmd.TeamId.Guid && m.StaffAdminId == cmd.StaffAdminId.Guid, cancellationToken);
                    return !exists;
                })
                .WithMessage("User is already a member of this team.");
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

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                TeamId = request.TeamId.Guid,
                StaffAdminId = request.StaffAdminId.Guid,
                IsAssignable = request.IsAssignable
            };

            _db.TeamMemberships.Add(membership);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(membership.Id);
        }
    }
}

