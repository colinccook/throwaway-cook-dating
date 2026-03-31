using CookDating.SharedKernel.Domain;
using CookDating.Conversation.Domain.Events;

namespace CookDating.Conversation.Domain;

public class Conversation : AggregateRoot<string>
{
    public string MatchId { get; private set; } = default!;
    public string Participant1Id { get; private set; } = default!;
    public string Participant2Id { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastMessageAt { get; private set; }

    private readonly List<Message> _messages = [];
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    private Conversation() { }

    /// <summary>
    /// Creates a conversation for a matched pair. Called when a match is detected.
    /// </summary>
    public static Conversation StartForMatch(string matchId, string participant1Id, string participant2Id)
    {
        if (string.IsNullOrWhiteSpace(matchId))
            throw new ArgumentException("MatchId is required", nameof(matchId));
        if (string.IsNullOrWhiteSpace(participant1Id))
            throw new ArgumentException("Participant1Id is required", nameof(participant1Id));
        if (string.IsNullOrWhiteSpace(participant2Id))
            throw new ArgumentException("Participant2Id is required", nameof(participant2Id));
        if (participant1Id == participant2Id)
            throw new InvalidOperationException("Cannot create conversation with yourself");

        var conversation = new Conversation
        {
            Id = Guid.NewGuid().ToString(),
            MatchId = matchId,
            Participant1Id = participant1Id,
            Participant2Id = participant2Id,
            CreatedAt = DateTime.UtcNow
        };

        conversation.RaiseDomainEvent(new ConversationStarted
        {
            ConversationId = conversation.Id,
            MatchId = matchId,
            Participant1Id = participant1Id,
            Participant2Id = participant2Id
        });

        return conversation;
    }

    /// <summary>
    /// Sends a message. ENFORCES that only participants can send messages (match constraint).
    /// </summary>
    public Message SendMessage(string senderId, string content)
    {
        if (!IsParticipant(senderId))
            throw new InvalidOperationException($"User {senderId} is not a participant in this conversation. Only matched users can send messages.");

        var message = Message.Create(senderId, content);
        _messages.Add(message);
        LastMessageAt = message.SentAt;

        var recipientId = GetOtherParticipant(senderId);

        RaiseDomainEvent(new MessageSent
        {
            ConversationId = Id,
            MessageId = message.Id,
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content
        });

        return message;
    }

    public void MarkMessagesAsRead(string readerId)
    {
        if (!IsParticipant(readerId))
            throw new InvalidOperationException($"User {readerId} is not a participant in this conversation");

        foreach (var message in _messages.Where(m => m.SenderId != readerId && !m.IsRead))
        {
            message.MarkAsRead();
        }
    }

    public bool IsParticipant(string userId) =>
        Participant1Id == userId || Participant2Id == userId;

    public string GetOtherParticipant(string userId)
    {
        if (Participant1Id == userId) return Participant2Id;
        if (Participant2Id == userId) return Participant1Id;
        throw new InvalidOperationException($"User {userId} is not a participant");
    }

    public int UnreadCountFor(string userId)
    {
        if (!IsParticipant(userId))
            throw new InvalidOperationException($"User {userId} is not a participant");
        return _messages.Count(m => m.SenderId != userId && !m.IsRead);
    }

    public static Conversation Rehydrate(string id, string matchId, string participant1Id, string participant2Id,
        DateTime createdAt, DateTime? lastMessageAt, List<Message> messages)
    {
        var conversation = new Conversation
        {
            Id = id,
            MatchId = matchId,
            Participant1Id = participant1Id,
            Participant2Id = participant2Id,
            CreatedAt = createdAt,
            LastMessageAt = lastMessageAt
        };
        conversation._messages.AddRange(messages);
        return conversation;
    }
}
