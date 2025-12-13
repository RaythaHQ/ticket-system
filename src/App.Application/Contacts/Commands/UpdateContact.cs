using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Contacts.Utils;
using App.Domain.Entities;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Commands;

public class UpdateContact
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long Id { get; init; }
        public string FirstName { get; init; } = null!;
        public string? LastName { get; init; }
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
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(250);
            RuleFor(x => x.LastName).MaximumLength(250);
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
            var contact = await _db.Contacts
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (contact == null)
                throw new NotFoundException("Contact", request.Id);

            var changes = new Dictionary<string, object>();

            var newFirstName = request.FirstName.Trim();
            if (contact.FirstName != newFirstName)
            {
                changes["FirstName"] = new { OldValue = contact.FirstName, NewValue = newFirstName };
                contact.FirstName = newFirstName;
            }

            var newLastName = request.LastName?.Trim();
            if (contact.LastName != newLastName)
            {
                changes["LastName"] = new { OldValue = contact.LastName ?? "", NewValue = newLastName ?? "" };
                contact.LastName = newLastName;
            }

            var newEmail = request.Email?.Trim().ToLower();
            if (contact.Email != newEmail)
            {
                changes["Email"] = new { OldValue = contact.Email ?? "", NewValue = newEmail ?? "" };
                contact.Email = newEmail;
            }

            var normalizedPhones = PhoneNumberNormalizer.NormalizeMany(request.PhoneNumbers);
            var oldPhonesJson = contact.PhoneNumbersJson;
            contact.PhoneNumbers = normalizedPhones;
            if (oldPhonesJson != contact.PhoneNumbersJson)
            {
                changes["PhoneNumbers"] = new { OldValue = oldPhonesJson ?? "[]", NewValue = contact.PhoneNumbersJson ?? "[]" };
            }

            if (contact.Address != request.Address)
            {
                changes["Address"] = new { OldValue = contact.Address ?? "", NewValue = request.Address ?? "" };
                contact.Address = request.Address;
            }

            if (contact.OrganizationAccount != request.OrganizationAccount)
            {
                changes["OrganizationAccount"] = new { OldValue = contact.OrganizationAccount ?? "", NewValue = request.OrganizationAccount ?? "" };
                contact.OrganizationAccount = request.OrganizationAccount;
            }

            contact.DmeIdentifiers = request.DmeIdentifiers ?? new Dictionary<string, string>();

            if (changes.Any())
            {
                // Build descriptive message with before/after values
                var messageParts = new List<string>();
                
                foreach (var change in changes)
                {
                    var fieldName = change.Key;
                    var changeObj = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(change.Value));
                    var oldValue = changeObj.GetProperty("OldValue").GetString() ?? "";
                    var newValue = changeObj.GetProperty("NewValue").GetString() ?? "";
                    
                    string description = fieldName switch
                    {
                        "FirstName" => $"First name changed from \"{oldValue}\" to \"{newValue}\"",
                        "LastName" => $"Last name changed from \"{oldValue}\" to \"{newValue}\"",
                        "Email" => $"Email changed from \"{oldValue}\" to \"{newValue}\"",
                        "PhoneNumbers" => $"Phone numbers updated",
                        "Address" => $"Address changed from \"{oldValue}\" to \"{newValue}\"",
                        "OrganizationAccount" => $"Organization changed from \"{oldValue}\" to \"{newValue}\"",
                        _ => $"{fieldName} changed from \"{oldValue}\" to \"{newValue}\""
                    };
                    messageParts.Add(description);
                }
                
                var changeLog = new ContactChangeLogEntry
                {
                    ContactId = contact.Id,
                    ActorStaffId = _currentUser.UserId?.Guid,
                    FieldChangesJson = JsonSerializer.Serialize(changes),
                    Message = string.Join("; ", messageParts)
                };
                contact.ChangeLogEntries.Add(changeLog);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(contact.Id);
        }
    }
}

