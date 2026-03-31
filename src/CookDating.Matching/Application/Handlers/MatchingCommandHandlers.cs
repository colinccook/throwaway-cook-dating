using CookDating.Matching.Application.Commands;
using CookDating.Matching.Domain;
using CookDating.SharedKernel.Domain;

namespace CookDating.Matching.Application.Handlers;

public class MatchingCommandHandlers
{
    private readonly IMatchCandidateRepository _candidateRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IEventPublisher _eventPublisher;
    private const string MatchingEventsTopicArn = "arn:aws:sns:us-east-1:000000000000:matching-events";

    public MatchingCommandHandlers(
        IMatchCandidateRepository candidateRepository,
        IMatchRepository matchRepository,
        IEventPublisher eventPublisher)
    {
        _candidateRepository = candidateRepository;
        _matchRepository = matchRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<(Match? match, bool isMatch)> HandleAsync(SwipeCommand command, CancellationToken ct = default)
    {
        var candidate = await _candidateRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Candidate not found: {command.UserId}");

        var targetCandidate = await _candidateRepository.GetByIdAsync(command.TargetUserId, ct);
        var targetHasLikedMe = targetCandidate?.HasLiked(command.UserId) ?? false;

        var match = candidate.RecordSwipe(command.TargetUserId, command.Direction, targetHasLikedMe);

        await _candidateRepository.SaveAsync(candidate, ct);

        if (match != null)
        {
            await _matchRepository.SaveAsync(match, ct);
            await _eventPublisher.PublishAsync(candidate.DomainEvents, MatchingEventsTopicArn, ct);
        }

        candidate.ClearDomainEvents();
        return (match, match != null);
    }

    public async Task HandleAsync(ProcessLookingStatusCommand command, CancellationToken ct = default)
    {
        var candidate = await _candidateRepository.GetByIdAsync(command.UserId, ct);

        if (candidate == null)
        {
            candidate = MatchCandidate.Create(
                command.UserId, command.DisplayName, command.Gender,
                command.PreferredGender, command.MinAge, command.MaxAge, command.MaxDistanceKm);
        }

        if (command.NewStatus == "ActivelyLooking")
            candidate.Activate();
        else
            candidate.Deactivate();

        await _candidateRepository.SaveAsync(candidate, ct);
    }

    public async Task HandleAsync(ProcessProfileCreatedCommand command, CancellationToken ct = default)
    {
        var existing = await _candidateRepository.GetByIdAsync(command.UserId, ct);
        if (existing != null) return;

        var candidate = MatchCandidate.Create(
            command.UserId, command.DisplayName, command.Gender,
            command.PreferredGender, command.MinAge, command.MaxAge, command.MaxDistanceKm);

        await _candidateRepository.SaveAsync(candidate, ct);
    }

    public async Task<List<MatchCandidate>> HandleAsync(GetCandidatesCommand command, CancellationToken ct = default)
    {
        var currentUser = await _candidateRepository.GetByIdAsync(command.UserId, ct);
        if (currentUser == null)
            return []; // Not yet indexed by the Matching Worker — return empty

        var allActive = await _candidateRepository.GetActiveCandidatesAsync(ct);

        return allActive
            .Where(c => c.Id != command.UserId && !currentUser.HasSwipedOn(c.Id))
            .ToList();
    }
}
