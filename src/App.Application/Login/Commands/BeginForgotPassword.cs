using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Login.Commands;

public class BeginForgotPassword
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string EmailAddress { get; init; } = null!;
        public bool SendEmail { get; init; } = true;
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
            var emailAddress = request.EmailAddress.ToLower().Trim();
            var entity = await _db.Users.FirstOrDefaultAsync(
                p => p.EmailAddress.ToLower() == emailAddress,
                cancellationToken
            );

            if (entity == null || !entity.IsActive)
            {
                return new CommandResponseDto<ShortGuid>(ShortGuid.NewGuid());
            }

            // Check auth scheme eligibility without revealing to the caller
            var authScheme = await _db.AuthenticationSchemes.FirstAsync(
                p => p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName,
                cancellationToken
            );

            if (
                (entity.IsAdmin && !authScheme.IsEnabledForAdmins)
                || (!entity.IsAdmin && !authScheme.IsEnabledForUsers)
            )
            {
                return new CommandResponseDto<ShortGuid>(ShortGuid.NewGuid());
            }

            ShortGuid guid = ShortGuid.NewGuid();
            var otp = new OneTimePassword
            {
                Id = PasswordUtility.Hash(guid),
                IsUsed = false,
                UserId = entity.Id,
                ExpiresAt = DateTime.UtcNow.AddSeconds(900),
            };

            _db.OneTimePasswords.Add(otp);

            entity.AddDomainEvent(new BeginForgotPasswordEvent(entity, request.SendEmail, guid));

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(guid);
        }
    }
}
