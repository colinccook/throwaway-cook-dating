using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CookDating.Conversation.Domain;
using CookDating.SharedKernel.Infrastructure;

namespace CookDating.Conversation.Infrastructure;

public class DynamoDbConversationRepository : DynamoDbRepository<Domain.Conversation, string>, IConversationRepository
{
    protected override string TableName => "Conversations";

    public DynamoDbConversationRepository(IAmazonDynamoDB dynamoDb) : base(dynamoDb) { }

    protected override Dictionary<string, AttributeValue> GetKey(string id) =>
        new() { ["ConversationId"] = new AttributeValue { S = id } };

    protected override Dictionary<string, AttributeValue> MapToAttributes(Domain.Conversation conversation)
    {
        var messagesJson = JsonSerializer.Serialize(conversation.Messages.Select(m => new
        {
            id = m.Id,
            senderId = m.SenderId,
            content = m.Content,
            sentAt = m.SentAt.ToString("O"),
            isRead = m.IsRead
        }));

        return new()
        {
            ["ConversationId"] = new AttributeValue { S = conversation.Id },
            ["MatchId"] = new AttributeValue { S = conversation.MatchId },
            ["Participant1Id"] = new AttributeValue { S = conversation.Participant1Id },
            ["Participant2Id"] = new AttributeValue { S = conversation.Participant2Id },
            ["CreatedAt"] = new AttributeValue { S = conversation.CreatedAt.ToString("O") },
            ["LastMessageAt"] = conversation.LastMessageAt.HasValue
                ? new AttributeValue { S = conversation.LastMessageAt.Value.ToString("O") }
                : new AttributeValue { NULL = true },
            ["Messages"] = new AttributeValue { S = messagesJson }
        };
    }

    protected override Domain.Conversation MapFromAttributes(Dictionary<string, AttributeValue> attrs)
    {
        var messages = new List<Message>();
        if (attrs.TryGetValue("Messages", out var msgAttr) && !string.IsNullOrEmpty(msgAttr.S))
        {
            using var doc = JsonDocument.Parse(msgAttr.S);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                messages.Add(Message.Rehydrate(
                    id: el.GetProperty("id").GetString()!,
                    senderId: el.GetProperty("senderId").GetString()!,
                    content: el.GetProperty("content").GetString()!,
                    sentAt: DateTime.Parse(el.GetProperty("sentAt").GetString()!),
                    isRead: el.GetProperty("isRead").GetBoolean()
                ));
            }
        }

        DateTime? lastMessageAt = attrs.TryGetValue("LastMessageAt", out var lma) && lma.NULL != true
            ? DateTime.Parse(lma.S!)
            : null;

        return Domain.Conversation.Rehydrate(
            id: attrs["ConversationId"].S,
            matchId: attrs["MatchId"].S,
            participant1Id: attrs["Participant1Id"].S,
            participant2Id: attrs["Participant2Id"].S,
            createdAt: DateTime.Parse(attrs["CreatedAt"].S),
            lastMessageAt: lastMessageAt,
            messages: messages
        );
    }

    public async Task<Domain.Conversation?> GetByMatchIdAsync(string matchId, CancellationToken cancellationToken = default)
    {
        var results = await QueryByIndexAsync(
            "MatchIdIndex",
            "MatchId = :matchId",
            new() { [":matchId"] = new AttributeValue { S = matchId } },
            cancellationToken);

        return results.FirstOrDefault();
    }

    public async Task<List<Domain.Conversation>> GetConversationsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var convos1 = await QueryByIndexAsync(
            "Participant1IdIndex",
            "Participant1Id = :userId",
            new() { [":userId"] = new AttributeValue { S = userId } },
            cancellationToken);

        var convos2 = await QueryByIndexAsync(
            "Participant2IdIndex",
            "Participant2Id = :userId",
            new() { [":userId"] = new AttributeValue { S = userId } },
            cancellationToken);

        return convos1.Concat(convos2).DistinctBy(c => c.Id).ToList();
    }
}
