using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CookDating.SharedKernel.Domain;

namespace CookDating.SharedKernel.Infrastructure;

public class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public SnsEventPublisher(IAmazonSimpleNotificationService snsClient)
    {
        _snsClient = snsClient;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, string topicArn, CancellationToken cancellationToken = default)
    {
        var message = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);

        await _snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = topicArn,
            Message = message,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new()
                {
                    DataType = "String",
                    StringValue = domainEvent.EventType
                }
            }
        }, cancellationToken);
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, string topicArn, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await PublishAsync(domainEvent, topicArn, cancellationToken);
        }
    }
}
