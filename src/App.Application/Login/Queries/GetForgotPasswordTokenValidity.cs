using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Login.Queries;

public class GetForgotPasswordTokenValidity
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<bool>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<bool>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<bool>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var authScheme = _db
                .AuthenticationSchemes.AsNoTracking()
                .First(p =>
                    p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword
                );

            if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                return new QueryResponseDto<bool>(
                    Constants.VALIDATION_SUMMARY,
                    "Authentication scheme is disabled"
                );

            var entity = _db
                .OneTimePasswords.AsNoTracking()
                .Include(p => p.User)
                .ThenInclude(p => p.AuthenticationScheme)
                .FirstOrDefault(p => p.Id == PasswordUtility.Hash(request.Id));

            if (entity == null)
                return new QueryResponseDto<bool>(Constants.VALIDATION_SUMMARY, "Invalid token.");

            if (entity.IsUsed || entity.ExpiresAt < DateTime.UtcNow)
                return new QueryResponseDto<bool>(
                    Constants.VALIDATION_SUMMARY,
                    "Token is consumed or expired."
                );

            if (!entity.User.IsActive)
                return new QueryResponseDto<bool>(
                    Constants.VALIDATION_SUMMARY,
                    "User has been deactivated."
                );

            if (entity.User.IsAdmin && !authScheme.IsEnabledForAdmins)
                return new QueryResponseDto<bool>(
                    Constants.VALIDATION_SUMMARY,
                    "Authentication scheme disabled for administrators."
                );

            if (!entity.User.IsAdmin && !authScheme.IsEnabledForUsers)
                return new QueryResponseDto<bool>(
                    Constants.VALIDATION_SUMMARY,
                    "Authentication scheme disabled for public users."
                );

            return new QueryResponseDto<bool>(true);
        }
    }
}
