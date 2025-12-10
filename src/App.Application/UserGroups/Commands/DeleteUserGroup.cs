using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.UserGroups.Commands;

public class DeleteUserGroup
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
                        var entity = await db
                            .UserGroups.AsNoTracking()
                            .Include(p => p.Users)
                            .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);
                        if (entity == null)
                            throw new NotFoundException("UserGroup", request.Id);

                        if (entity.Users.Count > 0)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Users are still assigned to this group. Unassign these users before deleting this group."
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

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db
                .UserGroups.Include(p => p.Users)
                .FirstAsync(p => p.Id == request.Id.Guid, cancellationToken);

            _db.UserGroups.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
