using CookDating.SharedKernel.Domain;

namespace CookDating.Profile.Domain.Events;

public sealed record ProfileCreated : DomainEvent
{
    public override string EventType => "ProfileCreated";
    public string UserId { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public Gender Gender { get; init; }
    public DatingPreferences Preferences { get; init; } = default!;
}
