using System.Text.Json.Serialization;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Login.Commands;

public class LoginWithEmailAndPassword
{
    public record Command : LoggableRequest<CommandResponseDto<LoginDto>>
    {
        public string EmailAddress { get; init; } = null!;

        [JsonIgnore]
        public string Password { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
            RuleFor(x => x)
                .CustomAsync(
                    async (request, context, cancellationToken) =>
                    {
                        var authScheme = await db.AuthenticationSchemes.FirstAsync(
                            p =>
                                p.AuthenticationSchemeType
                                == AuthenticationSchemeType.EmailAndPassword,
                            cancellationToken
                        );

                        // Cleanup stale failed login attempts (older than 2x window)
                        var cleanupCutoff = DateTime.UtcNow.AddSeconds(
                            -2 * authScheme.BruteForceProtectionWindowInSeconds
                        );
                        var staleAttempts = await db
                            .FailedLoginAttempts.Where(f => f.LastFailedAttemptAt < cleanupCutoff)
                            .ToListAsync(cancellationToken);
                        if (staleAttempts.Count > 0)
                        {
                            db.DbContext.RemoveRange(staleAttempts);
                            await db.DbContext.SaveChangesAsync(cancellationToken);
                        }

                        if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme is disabled."
                            );
                            return;
                        }

                        if (string.IsNullOrEmpty(request.Password))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Password is required."
                            );
                            return;
                        }

                        var emailAddress = !string.IsNullOrWhiteSpace(request.EmailAddress)
                            ? request.EmailAddress.ToLower().Trim()
                            : null;

                        // Check brute force lockout before proceeding
                        var failedAttempt =
                            emailAddress != null
                                ? await db.FailedLoginAttempts.FirstOrDefaultAsync(
                                    f => f.EmailAddress == emailAddress,
                                    cancellationToken
                                )
                                : null;
                        var windowStart = DateTime.UtcNow.AddSeconds(
                            -authScheme.BruteForceProtectionWindowInSeconds
                        );

                        if (
                            failedAttempt != null
                            && failedAttempt.FailedAttemptCount
                                >= authScheme.BruteForceProtectionMaxFailedAttempts
                            && failedAttempt.LastFailedAttemptAt >= windowStart
                        )
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Too many failed login attempts. Please try again later."
                            );
                            return;
                        }

                        var entity =
                            emailAddress != null
                                ? await db.Users.FirstOrDefaultAsync(
                                    p => p.EmailAddress.ToLower() == emailAddress,
                                    cancellationToken
                                )
                                : null;

                        if (entity == null)
                        {
                            await RecordFailedAttemptAsync(
                                db,
                                emailAddress,
                                failedAttempt,
                                windowStart,
                                cancellationToken
                            );
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Invalid email or password."
                            );
                            return;
                        }

                        if (!entity.IsActive)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Your account has been deactivated."
                            );
                            return;
                        }

                        if (entity.IsAdmin && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled for administrators."
                            );
                            return;
                        }

                        if (!entity.IsAdmin && !authScheme.IsEnabledForUsers)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled for public users."
                            );
                            return;
                        }

                        var passwordToCheck = PasswordUtility.Hash(request.Password, entity.Salt);
                        var passwordsMatch = PasswordUtility.IsMatch(
                            entity.PasswordHash,
                            passwordToCheck
                        );
                        if (!passwordsMatch)
                        {
                            await RecordFailedAttemptAsync(
                                db,
                                emailAddress,
                                failedAttempt,
                                windowStart,
                                cancellationToken
                            );
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Invalid email or password."
                            );
                            return;
                        }
                    }
                );
        }

        private static async Task RecordFailedAttemptAsync(
            IAppDbContext db,
            string? emailAddress,
            FailedLoginAttempt? existing,
            DateTime windowStart,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrEmpty(emailAddress))
                return;

            if (existing == null)
            {
                db.FailedLoginAttempts.Add(
                    new FailedLoginAttempt
                    {
                        Id = Guid.NewGuid(),
                        EmailAddress = emailAddress,
                        FailedAttemptCount = 1,
                        LastFailedAttemptAt = DateTime.UtcNow,
                    }
                );
            }
            else if (existing.LastFailedAttemptAt < windowStart)
            {
                // Window expired, reset count
                existing.FailedAttemptCount = 1;
                existing.LastFailedAttemptAt = DateTime.UtcNow;
            }
            else
            {
                // Within window, increment count
                existing.FailedAttemptCount++;
                existing.LastFailedAttemptAt = DateTime.UtcNow;
            }

            await db.DbContext.SaveChangesAsync(cancellationToken);
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
                p => p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword,
                cancellationToken
            );

            var emailAddress = request.EmailAddress.ToLower().Trim();

            var entity = await _db.Users.FirstAsync(
                p => p.EmailAddress.ToLower() == emailAddress,
                cancellationToken
            );

            // Clear failed login attempts on successful login
            var failedAttempt = await _db.FailedLoginAttempts.FirstOrDefaultAsync(
                f => f.EmailAddress == emailAddress,
                cancellationToken
            );
            if (failedAttempt != null)
            {
                _db.FailedLoginAttempts.Remove(failedAttempt);
            }

            entity.LastLoggedInTime = DateTime.UtcNow;
            entity.AuthenticationSchemeId = authScheme.Id;
            entity.SsoId = (ShortGuid)entity.Id;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<LoginDto>(
                new LoginDto
                {
                    Id = entity.Id,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    EmailAddress = entity.EmailAddress,
                    LastModificationTime = entity.LastModificationTime,
                    AuthenticationScheme = authScheme.DeveloperName,
                    SsoId = entity.SsoId,
                    IsAdmin = entity.IsAdmin,
                }
            );
        }
    }
}
