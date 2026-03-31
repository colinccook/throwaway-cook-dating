using CookDating.SharedKernel.Domain;

namespace CookDating.Conversation.Domain.Events;

public sealed record MessageSent : DomainEvent
{
    public override string EventType => "MessageSent";
    public string ConversationId { get; init; } = default!;
    public string MessageId { get; init; } = default!;
    public string SenderId { get; init; } = default!;
    public string RecipientId { get; init; } = default!;
    public string Content { get; init; } = default!;
}
