using CookDating.SharedKernel.Domain;

namespace CookDating.Conversation.Domain.Events;

public sealed record ConversationStarted : DomainEvent
{
    public override string EventType => "ConversationStarted";
    public string ConversationId { get; init; } = default!;
    public string MatchId { get; init; } = default!;
    public string Participant1Id { get; init; } = default!;
    public string Participant2Id { get; init; } = default!;
}
