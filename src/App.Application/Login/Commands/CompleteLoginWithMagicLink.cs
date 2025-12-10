using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Login.Commands;

public class CompleteLoginWithMagicLink
{
    public record Command : LoggableEntityRequest<CommandResponseDto<LoginDto>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x)
                .CustomAsync(
                    async (request, context, cancellationToken) =>
                    {
                        var authScheme = await db
                            .AuthenticationSchemes.AsNoTracking()
                            .FirstAsync(
                                p =>
                                    p.AuthenticationSchemeType
                                    == AuthenticationSchemeType.MagicLink.DeveloperName,
                                cancellationToken
                            );

                        if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme is disabled."
                            );
                            return;
                        }

                        var entity = await db
                            .OneTimePasswords.AsNoTracking()
                            .Include(p => p.User)
                            .ThenInclude(p => p.AuthenticationScheme)
                            .FirstOrDefaultAsync(
                                p => p.Id == PasswordUtility.Hash(request.Id),
                                cancellationToken
                            );

                        if (entity == null)
                        {
                            context.AddFailure(Constants.VALIDATION_SUMMARY, "Invalid token.");
                            return;
                        }

                        if (entity.IsUsed || entity.ExpiresAt < DateTime.UtcNow)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Token is consumed or expired."
                            );
                            return;
                        }

                        if (!entity.User.IsActive)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "User has been deactivated."
                            );
                            return;
                        }

                        if (entity.User.IsAdmin && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled for administrators."
                            );
                            return;
                        }

                        if (!entity.User.IsAdmin && !authScheme.IsEnabledForUsers)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled for public users."
                            );
                            return;
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<LoginDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<LoginDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var authScheme = await _db.AuthenticationSchemes.FirstAsync(
                p =>
                    p.AuthenticationSchemeType
                    == AuthenticationSchemeType.EmailAndPassword.DeveloperName,
                cancellationToken
            );

            var entity = await _db
                .OneTimePasswords.Include(p => p.User)
                .ThenInclude(p => p.AuthenticationScheme)
                .FirstAsync(p => p.Id == PasswordUtility.Hash(request.Id), cancellationToken);

            entity.IsUsed = true;
            entity.User.LastLoggedInTime = DateTime.UtcNow;
            entity.User.AuthenticationSchemeId = authScheme.Id;
            entity.User.SsoId = (ShortGuid)entity.UserId;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<LoginDto>(
                new LoginDto
                {
                    Id = entity.User.Id,
                    FirstName = entity.User.FirstName,
                    LastName = entity.User.LastName,
                    EmailAddress = entity.User.EmailAddress,
                    LastModificationTime = entity.User.LastModificationTime,
                }
            );
        }
    }
}
