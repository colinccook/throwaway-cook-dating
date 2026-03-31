using CookDating.SharedKernel.Domain;

namespace CookDating.Matching.Domain;

public class Match : AggregateRoot<string>
{
    public string User1Id { get; private set; } = default!;
    public string User2Id { get; private set; } = default!;
    public DateTime MatchedAt { get; private set; }

    private Match() { }

    public static Match Create(string user1Id, string user2Id)
    {
        if (string.IsNullOrWhiteSpace(user1Id))
            throw new ArgumentException("User1Id is required", nameof(user1Id));
        if (string.IsNullOrWhiteSpace(user2Id))
            throw new ArgumentException("User2Id is required", nameof(user2Id));
        if (user1Id == user2Id)
            throw new InvalidOperationException("Cannot match with yourself");

        // Ensure consistent ordering for deduplication
        var (first, second) = string.Compare(user1Id, user2Id, StringComparison.Ordinal) < 0
            ? (user1Id, user2Id)
            : (user2Id, user1Id);

        return new Match
        {
            Id = $"{first}:{second}",
            User1Id = first,
            User2Id = second,
            MatchedAt = DateTime.UtcNow
        };
    }

    public bool InvolvesUser(string userId) => User1Id == userId || User2Id == userId;
    
    public string GetOtherUserId(string userId)
    {
        if (User1Id == userId) return User2Id;
        if (User2Id == userId) return User1Id;
        throw new InvalidOperationException($"User {userId} is not part of this match");
    }

    // Rehydration
    public static Match Rehydrate(string matchId, string user1Id, string user2Id, DateTime matchedAt)
    {
        return new Match
        {
            Id = matchId,
            User1Id = user1Id,
            User2Id = user2Id,
            MatchedAt = matchedAt
        };
    }
}
