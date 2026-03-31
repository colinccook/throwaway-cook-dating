using CookDating.SharedKernel.Domain;

namespace CookDating.Profile.Domain.Events;

public sealed record LookingStatusChanged : DomainEvent
{
    public override string EventType => "LookingStatusChanged";
    public string UserId { get; init; } = default!;
    public LookingStatus NewStatus { get; init; }
    public LookingStatus PreviousStatus { get; init; }
}
