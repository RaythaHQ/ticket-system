using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Contacts.Utils;
using App.Domain.Entities;
using FluentValidation;
using Mediator;

namespace App.Application.Contacts.Commands;

public class CreateContact
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public string Name { get; init; } = null!;
        public string? Email { get; init; }
        public List<string>? PhoneNumbers { get; init; }
        public string? Address { get; init; }
        public string? OrganizationAccount { get; init; }
        public Dictionary<string, string>? DmeIdentifiers { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
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
            var contact = new Contact
            {
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
