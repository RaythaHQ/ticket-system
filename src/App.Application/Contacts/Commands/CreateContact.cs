using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Contacts.Utils;
using App.Domain.Entities;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Commands;

public class CreateContact
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long? Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Email { get; init; }
        public List<string>? PhoneNumbers { get; init; }
        public string? Address { get; init; }
        public string? OrganizationAccount { get; init; }
        public Dictionary<string, string>? DmeIdentifiers { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .When(x => x.Id.HasValue)
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        if (!id.HasValue)
                            return true;
                        var exists = await db.Contacts.AnyAsync(
                            c => c.Id == id.Value,
                            cancellationToken
                        );
                        return !exists;
                    }
                )
                .WithMessage("A contact with this ID already exists.")
                .When(x => x.Id.HasValue);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // If ID is specified, check if it already exists
            if (request.Id.HasValue)
            {
                var exists = await _db.Contacts.AnyAsync(
                    c => c.Id == request.Id.Value,
                    cancellationToken
                );
                if (exists)
                    throw new BusinessException("A contact with this ID already exists.");
            }

            var contact = new Contact
            {
                Name = request.Name,
                Email = request.Email?.Trim().ToLower(),
                PhoneNumbers = PhoneNumberNormalizer.NormalizeMany(request.PhoneNumbers),
                Address = request.Address,
                OrganizationAccount = request.OrganizationAccount,
                DmeIdentifiers = request.DmeIdentifiers ?? new Dictionary<string, string>(),
            };

            // If ID is specified, attempt to set it
            // Note: This may not work with identity columns without additional database configuration
            if (request.Id.HasValue)
            {
                contact.Id = request.Id.Value;
            }

            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(contact.Id);
        }
    }
}
