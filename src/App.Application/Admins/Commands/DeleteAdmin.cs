using CSharpVitamins;
using FluentValidation;
using Mediator;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;

namespace App.Application.Admins.Commands;

public class DeleteAdmin
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(ICurrentUser currentUser)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (request.Id == currentUser.UserId)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot remove your own account."
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
            var entity = _db.Users.FirstOrDefault(p => p.Id == request.Id.Guid && p.IsAdmin);
            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            // Null out foreign key references to prevent constraint violations
            var emailTemplates = _db.EmailTemplates.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (emailTemplates.Any())
            {
                foreach (var emailTemplate in emailTemplates)
                {
                    emailTemplate.CreatorUserId = null;
                    emailTemplate.LastModifierUserId = null;
                }
            }
            _db.EmailTemplates.UpdateRange(emailTemplates);

            var userGroups = _db.UserGroups.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (userGroups.Any())
            {
                foreach (var userGroup in userGroups)
                {
                    userGroup.CreatorUserId = null;
                    userGroup.LastModifierUserId = null;
                }
            }
            _db.UserGroups.UpdateRange(userGroups);

            var apiKeys = _db.ApiKeys.Where(p => p.CreatorUserId == request.Id.Guid);

            if (apiKeys.Any())
            {
                foreach (var apiKey in apiKeys)
                {
                    apiKey.CreatorUserId = null;
                }
            }
            _db.ApiKeys.UpdateRange(apiKeys);

            var roles = _db.Roles.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (roles.Any())
            {
                foreach (var role in roles)
                {
                    role.CreatorUserId = null;
                    role.LastModifierUserId = null;
                }
            }
            _db.Roles.UpdateRange(roles);

            var authenticationSchemes = _db.AuthenticationSchemes.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (authenticationSchemes.Any())
            {
                foreach (var authenticationScheme in authenticationSchemes)
                {
                    authenticationScheme.CreatorUserId = null;
                    authenticationScheme.LastModifierUserId = null;
                }
            }
            _db.AuthenticationSchemes.UpdateRange(authenticationSchemes);

            var verificationCodes = _db.VerificationCodes.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (verificationCodes.Any())
            {
                foreach (var verificationCode in verificationCodes)
                {
                    verificationCode.CreatorUserId = null;
                    verificationCode.LastModifierUserId = null;
                }
            }
            _db.VerificationCodes.UpdateRange(verificationCodes);

            var users = _db.Users.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (users.Any())
            {
                foreach (var user in users)
                {
                    user.CreatorUserId = null;
                    user.LastModifierUserId = null;
                }
            }
            _db.Users.UpdateRange(users);

            _db.Users.Remove(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
