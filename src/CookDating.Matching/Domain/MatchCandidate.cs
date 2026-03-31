using CookDating.SharedKernel.Domain;
using CookDating.Matching.Domain.Events;

namespace CookDating.Matching.Domain;

public class MatchCandidate : AggregateRoot<string>
{
    public bool IsActive { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Gender { get; private set; } = string.Empty;
    public string? PreferredGender { get; private set; }
    public int MinAge { get; private set; }
    public int MaxAge { get; private set; }
    public int MaxDistanceKm { get; private set; }
    private readonly List<Swipe> _swipes = [];
    public IReadOnlyList<Swipe> Swipes => _swipes.AsReadOnly();

    private MatchCandidate() { }

    public static MatchCandidate Create(string userId, string displayName, string gender,
        string? preferredGender, int minAge, int maxAge, int maxDistanceKm)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required", nameof(userId));

        return new MatchCandidate
        {
            Id = userId,
            DisplayName = displayName,
            Gender = gender,
            PreferredGender = preferredGender,
            MinAge = minAge,
            MaxAge = maxAge,
            MaxDistanceKm = maxDistanceKm,
            IsActive = false
        };
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Records a swipe and returns a Match if mutual like is detected.
    /// The caller must provide whether the target has already liked this user.
    /// </summary>
    public Match? RecordSwipe(string targetUserId, SwipeDirection direction, bool targetHasLikedMe)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            throw new ArgumentException("Target user ID required", nameof(targetUserId));
        if (targetUserId == Id)
            throw new InvalidOperationException("Cannot swipe on yourself");
        if (_swipes.Any(s => s.TargetUserId == targetUserId))
            throw new InvalidOperationException($"Already swiped on user {targetUserId}");
        if (!IsActive)
            throw new InvalidOperationException("Cannot swipe when not actively looking");

        var swipe = new Swipe(targetUserId, direction);
        _swipes.Add(swipe);

        RaiseDomainEvent(new SwipeRecorded
        {
            UserId = Id,
            TargetUserId = targetUserId,
            Direction = direction
        });

        // Check for mutual like → create match
        if (direction == SwipeDirection.Right && targetHasLikedMe)
        {
            var match = Match.Create(Id, targetUserId);
            // Raise the match event on this aggregate since it initiated the match
            RaiseDomainEvent(new MatchCreated
            {
                MatchId = match.Id,
                User1Id = Id,
                User2Id = targetUserId
            });
            return match;
        }

        return null;
    }

    public bool HasSwipedOn(string targetUserId) =>
        _swipes.Any(s => s.TargetUserId == targetUserId);

    public bool HasLiked(string targetUserId) =>
        _swipes.Any(s => s.TargetUserId == targetUserId && s.Direction == SwipeDirection.Right);

    // Rehydration factory
    public static MatchCandidate Rehydrate(string userId, string displayName, string gender,
        string? preferredGender, int minAge, int maxAge, int maxDistanceKm,
        bool isActive, List<Swipe> swipes)
    {
        var candidate = new MatchCandidate
        {
            Id = userId,
            DisplayName = displayName,
            Gender = gender,
            PreferredGender = preferredGender,
            MinAge = minAge,
            MaxAge = maxAge,
            MaxDistanceKm = maxDistanceKm,
            IsActive = isActive
        };
        candidate._swipes.AddRange(swipes);
        return candidate;
    }
}
