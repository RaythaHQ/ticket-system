using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Admins.Commands;

public class EditAdmin
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string EmailAddress { get; init; } = null!;
        public IEnumerable<ShortGuid> Roles { get; init; } = null!;
        
        // Custom attributes
        public string? CustomAttribute1 { get; init; }
        public string? CustomAttribute2 { get; init; }
        public string? CustomAttribute3 { get; init; }
        public string? CustomAttribute4 { get; init; }
        public string? CustomAttribute5 { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Roles).NotEmpty();
            RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
            RuleFor(x => x)
                .CustomAsync(
                    async (request, context, cancellationToken) =>
                    {
                        var entity = await db
                            .Users.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

                        if (entity == null)
                            throw new NotFoundException("Admin", request.Id);

                        if (request.EmailAddress.ToLower() != entity.EmailAddress.ToLower())
                        {
                            var emailAddressToCheck = request.EmailAddress.ToLower();
                            var doesAnotherEmailExist = await db
                                .Users.AsNoTracking()
                                .AnyAsync(
                                    p => p.EmailAddress.ToLower() == emailAddressToCheck,
                                    cancellationToken
                                );
                            if (doesAnotherEmailExist)
                            {
                                context.AddFailure(
                                    "EmailAddress",
                                    "Another user with this email address already exists"
                                );
                                return;
                            }
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
                .Users.Include(p => p.Roles)
                .FirstAsync(p => p.Id == request.Id.Guid && p.IsAdmin, cancellationToken);

            entity.FirstName = request.FirstName;
            entity.LastName = request.LastName;
            entity.EmailAddress = request.EmailAddress;
            
            // Update custom attributes
            entity.CustomAttribute1 = request.CustomAttribute1?.Trim();
            entity.CustomAttribute2 = request.CustomAttribute2?.Trim();
            entity.CustomAttribute3 = request.CustomAttribute3?.Trim();
            entity.CustomAttribute4 = request.CustomAttribute4?.Trim();
            entity.CustomAttribute5 = request.CustomAttribute5?.Trim();

            var currentRoleIds = entity.Roles.Select(p => (ShortGuid)p.Id);

            var rolesToAddIds = request.Roles.Except(currentRoleIds);
            var rolesToDeleteIds = currentRoleIds.Except(request.Roles);

            foreach (var roleToAddId in rolesToAddIds)
            {
                var roleToAdd = await _db.Roles.FirstAsync(
                    p => p.Id == roleToAddId.Guid,
                    cancellationToken
                );
                entity.Roles.Add(roleToAdd);
            }

            foreach (var roleToDeleteId in rolesToDeleteIds)
            {
                entity.Roles.Remove(entity.Roles.First(p => p.Id == roleToDeleteId.Guid));
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
