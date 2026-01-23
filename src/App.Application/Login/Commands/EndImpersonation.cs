using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Login.Commands;

/// <summary>
/// Command to end an impersonation session and restore the original admin's identity.
/// </summary>
public class EndImpersonation
{
    public record Command : LoggableRequest<CommandResponseDto<LoginDto>>
    {
        /// <summary>
        /// The original admin's user ID (from claims).
        /// </summary>
        public ShortGuid OriginalUserId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(ICurrentUser currentUser)
        {
            RuleFor(x => x.OriginalUserId)
                .NotEmpty()
                .WithMessage("Original user ID is required.");

            RuleFor(x => x)
                .Custom((request, context) =>
                {
                    // Must be authenticated
                    if (!currentUser.IsAuthenticated)
                    {
                        context.AddFailure("You must be authenticated.");
                        return;
                    }

                    // Must currently be impersonating
                    if (!currentUser.IsImpersonating)
                    {
                        context.AddFailure("You are not currently impersonating anyone.");
                        return;
                    }

                    // The original user ID from the request must match the one in the claims
                    // This prevents manipulation of the end impersonation request
                    if (currentUser.OriginalUserId != request.OriginalUserId)
                    {
                        context.AddFailure("Invalid original user ID. Security violation detected.");
                        return;
                    }
                });
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
            CancellationToken cancellationToken)
        {
            // Fetch the original admin user to restore their session
            var originalAdmin = await _db.Users
                .Include(u => u.Roles)
                .Include(u => u.UserGroups)
                .Include(u => u.AuthenticationScheme)
                .FirstOrDefaultAsync(u => u.Id == request.OriginalUserId.Guid, cancellationToken);

            if (originalAdmin == null)
            {
                // This should never happen, but handle gracefully
                return new CommandResponseDto<LoginDto>("OriginalUserId", "Original admin account not found.");
            }

            if (!originalAdmin.IsActive)
            {
                return new CommandResponseDto<LoginDto>("OriginalUserId", "Original admin account has been deactivated.");
            }

            var loginDto = new LoginDto
            {
                Id = originalAdmin.Id,
                FirstName = originalAdmin.FirstName,
                LastName = originalAdmin.LastName,
                EmailAddress = originalAdmin.EmailAddress,
                LastModificationTime = originalAdmin.LastModificationTime,
                AuthenticationScheme = originalAdmin.AuthenticationScheme?.DeveloperName ?? string.Empty,
                SsoId = originalAdmin.SsoId ?? string.Empty,
                IsAdmin = originalAdmin.IsAdmin,
                IsActive = originalAdmin.IsActive,
                Roles = originalAdmin.Roles.Select(r => new Roles.RoleDto
                {
                    Id = r.Id,
                    Label = r.Label,
                    DeveloperName = r.DeveloperName,
                }),
                UserGroups = originalAdmin.UserGroups.Select(ug => new UserGroups.UserGroupDto
                {
                    Id = ug.Id,
                    Label = ug.Label,
                    DeveloperName = ug.DeveloperName,
                }),
            };

            return new CommandResponseDto<LoginDto>(loginDto);
        }
    }
}

