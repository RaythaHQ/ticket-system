using System.Text.Json.Serialization;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.Events;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Login.Commands;

public class CompleteForgotPassword
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        [JsonIgnore]
        public string NewPassword { get; init; } = null!;

        [JsonIgnore]
        public string ConfirmNewPassword { get; init; } = null!;
        public bool SendEmail { get; init; } = true;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x)
                .CustomAsync(
                    async (request, context, cancellationToken) =>
                    {
                        if (
                            string.IsNullOrEmpty(request.NewPassword)
                            || request.NewPassword.Length
                                < PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH
                        )
                        {
                            context.AddFailure(
                                "NewPassword",
                                $"Password must be at least {PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH} characters."
                            );
                            return;
                        }

                        if (request.NewPassword != request.ConfirmNewPassword)
                        {
                            context.AddFailure(
                                "ConfirmNewPassword",
                                "Confirm Password did not match."
                            );
                            return;
                        }

                        var authScheme = await db
                            .AuthenticationSchemes.AsNoTracking()
                            .FirstAsync(
                                p =>
                                    p.DeveloperName
                                    == AuthenticationSchemeType.EmailAndPassword.DeveloperName,
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
                .OneTimePasswords.Include(p => p.User)
                .ThenInclude(p => p.AuthenticationScheme)
                .FirstAsync(p => p.Id == PasswordUtility.Hash(request.Id), cancellationToken);

            var salt = PasswordUtility.RandomSalt();
            entity.User.Salt = salt;
            entity.User.PasswordHash = PasswordUtility.Hash(request.NewPassword, salt);
            entity.IsUsed = true;

            entity.AddDomainEvent(
                new CompletedForgotPasswordEvent(
                    entity.User,
                    request.SendEmail,
                    request.NewPassword
                )
            );

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.User.Id);
        }
    }
}
