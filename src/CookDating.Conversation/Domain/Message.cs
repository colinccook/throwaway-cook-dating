using CookDating.SharedKernel.Domain;

namespace CookDating.Conversation.Domain;

public class Message : Entity<string>
{
    public string SenderId { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public DateTime SentAt { get; private set; }
    public bool IsRead { get; private set; }

    private Message() { }

    public static Message Create(string senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(senderId))
            throw new ArgumentException("SenderId is required", nameof(senderId));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content is required", nameof(content));
        if (content.Length > 2000)
            throw new ArgumentException("Message cannot exceed 2000 characters", nameof(content));

        return new Message
        {
            Id = Guid.NewGuid().ToString(),
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }

    public static Message Rehydrate(string id, string senderId, string content, DateTime sentAt, bool isRead)
    {
        return new Message
        {
            Id = id,
            SenderId = senderId,
            Content = content,
            SentAt = sentAt,
            IsRead = isRead
        };
    }
}
