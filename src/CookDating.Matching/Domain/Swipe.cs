using CookDating.SharedKernel.Domain;

namespace CookDating.Matching.Domain;

public class Swipe : ValueObject
{
    public string TargetUserId { get; }
    public SwipeDirection Direction { get; }
    public DateTime SwipedAt { get; }

    public Swipe(string targetUserId, SwipeDirection direction, DateTime? swipedAt = null)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            throw new ArgumentException("Target user ID is required", nameof(targetUserId));
        
        TargetUserId = targetUserId;
        Direction = direction;
        SwipedAt = swipedAt ?? DateTime.UtcNow;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TargetUserId;
        yield return Direction;
    }
}
