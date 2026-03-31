using CookDating.SharedKernel.Domain;

namespace CookDating.Matching.Domain;

public interface IMatchCandidateRepository : IRepository<MatchCandidate, string>
{
    Task<List<MatchCandidate>> GetActiveCandidatesAsync(CancellationToken cancellationToken = default);
}
