using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Login.Commands;

/// <summary>
/// Command to begin impersonating another user. Only Super Admins can impersonate.
/// Security best practices:
/// - Only super_admin role can initiate impersonation
/// - Cannot impersonate yourself
/// - Cannot impersonate another Super Admin
/// - All impersonation events are logged for audit
/// - Original admin identity is preserved in claims for end-session
/// </summary>
public class BeginImpersonation
{
    public record Command : LoggableRequest<CommandResponseDto<ImpersonationResultDto>>
    {
        /// <summary>
        /// The ID of the user to impersonate.
        /// </summary>
        public ShortGuid TargetUserId { get; init; }
    }

    public record ImpersonationResultDto
    {
        /// <summary>
        /// The impersonated user's login information.
        /// </summary>
        public LoginDto ImpersonatedUser { get; init; } = null!;

        /// <summary>
        /// The original admin's user ID (to restore later).
        /// </summary>
        public ShortGuid OriginalUserId { get; init; }

        /// <summary>
        /// The original admin's email address.
        /// </summary>
        public string OriginalUserEmail { get; init; } = null!;

        /// <summary>
        /// The original admin's full name.
        /// </summary>
        public string OriginalUserFullName { get; init; } = null!;

        /// <summary>
        /// When the impersonation session started.
        /// </summary>
        public DateTime ImpersonationStartedAt { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db, ICurrentUser currentUser)
        {
            RuleFor(x => x.TargetUserId)
                .NotEmpty()
                .WithMessage("Target user ID is required.");

            RuleFor(x => x)
                .CustomAsync(async (request, context, cancellationToken) =>
                {
                    // Must be authenticated
                    if (!currentUser.IsAuthenticated)
                    {
                        context.AddFailure("You must be authenticated to impersonate.");
                        return;
                    }

                    // Must be an admin
                    if (!currentUser.IsAdmin)
                    {
                        context.AddFailure("Only administrators can impersonate users.");
                        return;
                    }

                    // Must have super_admin role
                    var isSuperAdmin = currentUser.Roles?.Contains(BuiltInRole.SuperAdmin.DeveloperName) ?? false;
                    if (!isSuperAdmin)
                    {
                        context.AddFailure("Only Super Admins can impersonate other users.");
                        return;
                    }

                    // Cannot already be impersonating
                    if (currentUser.IsImpersonating)
                    {
                        context.AddFailure("You are already impersonating another user. End the current impersonation first.");
                        return;
                    }

                    // Cannot impersonate yourself
                    if (currentUser.UserId == request.TargetUserId)
                    {
                        context.AddFailure("You cannot impersonate yourself.");
                        return;
                    }

                    // Target user must exist and be active
                    var targetUser = await db.Users
                        .Include(u => u.Roles)
                        .FirstOrDefaultAsync(u => u.Id == request.TargetUserId.Guid, cancellationToken);

                    if (targetUser == null)
                    {
                        context.AddFailure("Target user not found.");
                        return;
                    }

                    if (!targetUser.IsActive)
                    {
                        context.AddFailure("Cannot impersonate a deactivated user.");
                        return;
                    }

                    // Cannot impersonate another Super Admin
                    var targetIsSuperAdmin = targetUser.Roles?.Any(r => r.DeveloperName == BuiltInRole.SuperAdmin.DeveloperName) ?? false;
                    if (targetIsSuperAdmin)
                    {
                        context.AddFailure("Cannot impersonate another Super Admin.");
                        return;
                    }
                });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ImpersonationResultDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<ImpersonationResultDto>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var targetUser = await _db.Users
                .Include(u => u.Roles)
                .Include(u => u.UserGroups)
                .Include(u => u.AuthenticationScheme)
                .FirstAsync(u => u.Id == request.TargetUserId.Guid, cancellationToken);

            var impersonationStartedAt = DateTime.UtcNow;

            var loginDto = new LoginDto
            {
                Id = targetUser.Id,
                FirstName = targetUser.FirstName,
                LastName = targetUser.LastName,
                EmailAddress = targetUser.EmailAddress,
                LastModificationTime = targetUser.LastModificationTime,
                AuthenticationScheme = targetUser.AuthenticationScheme?.DeveloperName ?? string.Empty,
                SsoId = targetUser.SsoId ?? string.Empty,
                IsAdmin = targetUser.IsAdmin,
                IsActive = targetUser.IsActive,
                Roles = targetUser.Roles.Select(r => new Roles.RoleDto
                {
                    Id = r.Id,
                    Label = r.Label,
                    DeveloperName = r.DeveloperName,
                }),
                UserGroups = targetUser.UserGroups.Select(ug => new UserGroups.UserGroupDto
                {
                    Id = ug.Id,
                    Label = ug.Label,
                    DeveloperName = ug.DeveloperName,
                }),
            };

            return new CommandResponseDto<ImpersonationResultDto>(new ImpersonationResultDto
            {
                ImpersonatedUser = loginDto,
                OriginalUserId = _currentUser.UserId!.Value,
                OriginalUserEmail = _currentUser.EmailAddress,
                OriginalUserFullName = _currentUser.FullName,
                ImpersonationStartedAt = impersonationStartedAt,
            });
        }
    }
}

