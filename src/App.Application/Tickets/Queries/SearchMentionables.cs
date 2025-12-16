using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class SearchMentionables
{
    public record Query : IRequest<IQueryResponseDto<MentionablesResult>>
    {
        public string SearchTerm { get; init; } = string.Empty;
        public int MaxResults { get; init; } = 10;
    }

    public record MentionablesResult
    {
        public IEnumerable<MentionableTeam> Teams { get; init; } =
            Enumerable.Empty<MentionableTeam>();
        public IEnumerable<MentionableUser> Users { get; init; } =
            Enumerable.Empty<MentionableUser>();
    }

    public record MentionableTeam
    {
        public ShortGuid Id { get; init; }
        public string Name { get; init; } = null!;
        public int MemberCount { get; init; }
    }

    public record MentionableUser
    {
        public ShortGuid Id { get; init; }
        public string Name { get; init; } = null!;
        public string Email { get; init; } = null!;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<MentionablesResult>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<MentionablesResult>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var searchTerm = request.SearchTerm?.Trim().ToLower() ?? string.Empty;
            var maxResults = Math.Min(request.MaxResults, 20); // Cap at 20

            // Search teams
            var teamsQuery = _db.Teams.AsNoTracking();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                teamsQuery = teamsQuery.Where(t => t.Name.ToLower().Contains(searchTerm));
            }

            var teams = await teamsQuery
                .OrderBy(t => t.Name)
                .Take(maxResults)
                .Select(t => new MentionableTeam
                {
                    Id = t.Id,
                    Name = t.Name,
                    MemberCount = t.Memberships.Count,
                })
                .ToListAsync(cancellationToken);

            // Search active admin users
            var usersQuery = _db.Users.AsNoTracking().Where(u => u.IsAdmin && u.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.ToLower().Contains(searchTerm)
                    || u.LastName.ToLower().Contains(searchTerm)
                    || (u.FirstName + " " + u.LastName).ToLower().Contains(searchTerm)
                );
            }

            var users = await usersQuery
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Take(maxResults)
                .Select(u => new MentionableUser
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    Email = u.EmailAddress,
                })
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<MentionablesResult>(
                new MentionablesResult { Teams = teams, Users = users }
            );
        }
    }
}
