using System.Text.RegularExpressions;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class AddTicketComment
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public long TicketId { get; init; }
        public string Body { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).GreaterThan(0);
            RuleFor(x => x.Body).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        // Regex to match @mentions - captures text after @ until a non-name character
        // Matches: @TeamName, @First Last, @FirstName
        private static readonly Regex MentionRegex = new(
            @"@([A-Za-z][A-Za-z0-9\s]*?)(?=\s@|[^\w\s]|$)",
            RegexOptions.Compiled
        );

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
            var ticket = await _db.Tickets.FirstOrDefaultAsync(
                t => t.Id == request.TicketId,
                cancellationToken
            );

            if (ticket == null)
                throw new NotFoundException("Ticket", request.TicketId);

            var authorId =
                _currentUser.UserIdAsGuid
                ?? throw new ForbiddenAccessException("User not authenticated.");

            var comment = new TicketComment
            {
                Id = Guid.NewGuid(),
                TicketId = request.TicketId,
                AuthorStaffId = authorId,
                Body = request.Body,
            };

            _db.TicketComments.Add(comment);

            // Parse mentions from the comment body
            var (mentionedUserIds, mentionedTeamIds) = await ParseMentionsAsync(
                request.Body,
                cancellationToken
            );

            // Fire the comment added event for notifications
            ticket.AddDomainEvent(
                new TicketCommentAddedEvent(comment, ticket, mentionedUserIds, mentionedTeamIds)
            );

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(comment.Id);
        }

        private async Task<(List<Guid> userIds, List<Guid> teamIds)> ParseMentionsAsync(
            string body,
            CancellationToken cancellationToken
        )
        {
            var mentionedUserIds = new List<Guid>();
            var mentionedTeamIds = new List<Guid>();

            if (string.IsNullOrWhiteSpace(body))
                return (mentionedUserIds, mentionedTeamIds);

            // Extract all potential mention names
            var matches = MentionRegex.Matches(body);
            if (matches.Count == 0)
                return (mentionedUserIds, mentionedTeamIds);

            var mentionNames = matches
                .Select(m => m.Groups[1].Value.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!mentionNames.Any())
                return (mentionedUserIds, mentionedTeamIds);

            // Load all teams and active admin users for matching
            var teams = await _db
                .Teams.AsNoTracking()
                .Select(t => new { t.Id, t.Name })
                .ToListAsync(cancellationToken);

            var users = await _db
                .Users.AsNoTracking()
                .Where(u => u.IsAdmin && u.IsActive)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync(cancellationToken);

            foreach (var mentionName in mentionNames)
            {
                // Check if it matches a team name
                var team = teams.FirstOrDefault(t =>
                    t.Name.Equals(mentionName, StringComparison.OrdinalIgnoreCase)
                );
                if (team != null)
                {
                    mentionedTeamIds.Add(team.Id);
                    continue;
                }

                // Check if it matches a user's full name
                var user = users.FirstOrDefault(u =>
                    u.FullName.Equals(mentionName, StringComparison.OrdinalIgnoreCase)
                );
                if (user != null)
                {
                    mentionedUserIds.Add(user.Id);
                }
            }

            return (mentionedUserIds, mentionedTeamIds);
        }
    }
}
