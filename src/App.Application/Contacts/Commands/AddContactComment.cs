using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Commands;

public class AddContactComment
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public long ContactId { get; init; }
        public string Body { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContactId).GreaterThan(0);
            RuleFor(x => x.Body).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Check contact exists without tracking to avoid modification issues
            var contactExists = await _db.Contacts
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.ContactId, cancellationToken);

            if (!contactExists)
                throw new NotFoundException("Contact", request.ContactId);

            var authorId = _currentUser.UserIdAsGuid ?? throw new ForbiddenAccessException("User not authenticated.");

            var comment = new ContactComment
            {
                Id = Guid.NewGuid(),
                ContactId = request.ContactId,
                AuthorStaffId = authorId,
                Body = request.Body
            };

            // Add directly to DbSet instead of navigation property
            _db.ContactComments.Add(comment);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(comment.Id);
        }
    }
}

