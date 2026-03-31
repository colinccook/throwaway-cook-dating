using CookDating.SharedKernel.Domain;

namespace CookDating.Matching.Domain.Events;

public sealed record SwipeRecorded : DomainEvent
{
    public override string EventType => "SwipeRecorded";
    public string UserId { get; init; } = default!;
    public string TargetUserId { get; init; } = default!;
    public SwipeDirection Direction { get; init; }
}
