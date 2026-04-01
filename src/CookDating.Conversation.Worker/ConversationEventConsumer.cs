using System.Text.Json;
using Amazon.SQS;
using CookDating.Conversation.Application.Commands;
using CookDating.Conversation.Application.Handlers;
using CookDating.SharedKernel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CookDating.Conversation.Worker;

public partial class ConversationEventConsumer : SqsMessageConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConversationEventConsumer> _logger;

    protected override string QueueName => "conversation-queue";

    public ConversationEventConsumer(
        IAmazonSQS sqsClient,
        IServiceScopeFactory scopeFactory,
        ILogger<ConversationEventConsumer> logger)
        : base(sqsClient, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task HandleMessageAsync(string eventType, string messageBody, CancellationToken cancellationToken)
    {
        LogProcessingEvent(eventType);

        switch (eventType)
        {
            case "MatchCreated":
                await HandleMatchCreated(messageBody, cancellationToken);
                break;
            default:
                LogUnknownEventType(eventType);
                break;
        }
    }

    private async Task HandleMatchCreated(string body, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var matchId = root.GetProperty("matchId").GetString()!;
        var user1Id = root.GetProperty("user1Id").GetString()!;
        var user2Id = root.GetProperty("user2Id").GetString()!;

        using var scope = _scopeFactory.CreateScope();

        // Set tenant context from message attributes
        if (MessageTenantId is not null
            && scope.ServiceProvider.GetRequiredService<ITenantContext>() is TenantContext tc)
        {
            tc.TenantId = MessageTenantId;
        }

        var handlers = scope.ServiceProvider.GetRequiredService<ConversationCommandHandlers>();

        var command = new StartConversationCommand(matchId, user1Id, user2Id);
        var conversation = await handlers.HandleAsync(command, ct);

        LogConversationCreated(conversation.Id, matchId, user1Id, user2Id);
    }

    [LoggerMessage(EventId = 6001, Level = LogLevel.Information, Message = "Processing event: {EventType}")]
    private partial void LogProcessingEvent(string eventType);

    [LoggerMessage(EventId = 6002, Level = LogLevel.Warning, Message = "Unknown event type: {EventType}")]
    private partial void LogUnknownEventType(string eventType);

    [LoggerMessage(EventId = 6003, Level = LogLevel.Information, Message = "Created conversation {ConversationId} for match {MatchId} between {User1} and {User2}")]
    private partial void LogConversationCreated(string conversationId, string matchId, string user1, string user2);
}
