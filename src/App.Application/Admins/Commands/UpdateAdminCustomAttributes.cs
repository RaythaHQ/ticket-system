using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Admins.Commands;

public class UpdateAdminCustomAttributes
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
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
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Admin ID is required.");

            RuleFor(x => x.CustomAttribute1).MaximumLength(500);
            RuleFor(x => x.CustomAttribute2).MaximumLength(500);
            RuleFor(x => x.CustomAttribute3).MaximumLength(500);
            RuleFor(x => x.CustomAttribute4).MaximumLength(500);
            RuleFor(x => x.CustomAttribute5).MaximumLength(500);

            RuleFor(x => x)
                .CustomAsync(async (request, context, cancellationToken) =>
                {
                    var entity = await db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == request.Id.Guid && p.IsAdmin, cancellationToken);

                    if (entity == null)
                        throw new NotFoundException("Admin", request.Id);
                });
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
            var entity = await _db.Users
                .FirstAsync(p => p.Id == request.Id.Guid && p.IsAdmin, cancellationToken);

            entity.CustomAttribute1 = request.CustomAttribute1?.Trim();
            entity.CustomAttribute2 = request.CustomAttribute2?.Trim();
            entity.CustomAttribute3 = request.CustomAttribute3?.Trim();
            entity.CustomAttribute4 = request.CustomAttribute4?.Trim();
            entity.CustomAttribute5 = request.CustomAttribute5?.Trim();

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}

