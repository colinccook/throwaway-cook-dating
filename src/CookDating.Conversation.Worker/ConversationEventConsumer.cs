using System.Text.Json;
using Amazon.SQS;
using CookDating.Conversation.Application.Commands;
using CookDating.Conversation.Application.Handlers;
using CookDating.SharedKernel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CookDating.Conversation.Worker;

public class ConversationEventConsumer : SqsMessageConsumer
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
        _logger.LogInformation("Processing event: {EventType}", eventType);

        switch (eventType)
        {
            case "MatchCreated":
                await HandleMatchCreated(messageBody, cancellationToken);
                break;
            default:
                _logger.LogWarning("Unknown event type: {EventType}", eventType);
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
        var handlers = scope.ServiceProvider.GetRequiredService<ConversationCommandHandlers>();

        var command = new StartConversationCommand(matchId, user1Id, user2Id);
        var conversation = await handlers.HandleAsync(command, ct);

        _logger.LogInformation(
            "Created conversation {ConversationId} for match {MatchId} between {User1} and {User2}",
            conversation.Id, matchId, user1Id, user2Id);
    }
}
