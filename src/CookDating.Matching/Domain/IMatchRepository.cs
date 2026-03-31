using CookDating.SharedKernel.Domain;

namespace CookDating.Matching.Domain;

public interface IMatchRepository : IRepository<Match, string>
{
    Task<List<Match>> GetMatchesForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Match?> GetMatchBetweenUsersAsync(string userId1, string userId2, CancellationToken cancellationToken = default);
}
