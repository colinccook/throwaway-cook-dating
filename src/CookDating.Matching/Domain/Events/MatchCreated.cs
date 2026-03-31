using CookDating.SharedKernel.Domain;

namespace CookDating.Matching.Domain.Events;

public sealed record MatchCreated : DomainEvent
{
    public override string EventType => "MatchCreated";
    public string MatchId { get; init; } = default!;
    public string User1Id { get; init; } = default!;
    public string User2Id { get; init; } = default!;
}
