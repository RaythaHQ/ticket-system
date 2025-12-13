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
        /// <summary>
        /// Optional custom ID. If not specified, an ID will be auto-generated (min 7 digits).
        /// </summary>
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
                .WithMessage("Contact ID must be a positive number.");
            RuleFor(x => x.Id)
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        if (!id.HasValue)
                            return true;
                        // Check both active and soft-deleted contacts
                        var exists = await db.Contacts
                            .IgnoreQueryFilters()
                            .AnyAsync(c => c.Id == id.Value, cancellationToken);
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
        private readonly INumericIdGenerator _idGenerator;

        public Handler(IAppDbContext db, ICurrentUser currentUser, INumericIdGenerator idGenerator)
        {
            _db = db;
            _currentUser = currentUser;
            _idGenerator = idGenerator;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Determine the ID: use specified ID or generate one
            long contactId;
            if (request.Id.HasValue)
            {
                // Double-check the ID doesn't exist (belt and suspenders)
                var exists = await _db.Contacts
                    .IgnoreQueryFilters()
                    .AnyAsync(c => c.Id == request.Id.Value, cancellationToken);
                if (exists)
                    throw new BusinessException("A contact with this ID already exists.");
                contactId = request.Id.Value;
            }
            else
            {
                // Auto-generate ID (minimum 7 digits)
                contactId = await _idGenerator.GetNextContactIdAsync(cancellationToken);
            }

            var contact = new Contact
            {
                Id = contactId,
                Name = request.Name,
                Email = request.Email?.Trim().ToLower(),
                PhoneNumbers = PhoneNumberNormalizer.NormalizeMany(request.PhoneNumbers),
                Address = request.Address,
                OrganizationAccount = request.OrganizationAccount,
                DmeIdentifiers = request.DmeIdentifiers ?? new Dictionary<string, string>(),
            };

            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(contact.Id);
        }
    }
}
