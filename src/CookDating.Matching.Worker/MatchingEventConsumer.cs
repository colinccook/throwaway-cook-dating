using System.Text.Json;
using Amazon.SQS;
using CookDating.Matching.Application.Commands;
using CookDating.Matching.Application.Handlers;
using CookDating.SharedKernel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CookDating.Matching.Worker;

public partial class MatchingEventConsumer : SqsMessageConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchingEventConsumer> _logger;

    protected override string QueueName => "matching-queue";

    public MatchingEventConsumer(
        IAmazonSQS sqsClient,
        IServiceScopeFactory scopeFactory,
        ILogger<MatchingEventConsumer> logger)
        : base(sqsClient, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task HandleMessageAsync(string eventType, string messageBody, CancellationToken cancellationToken)
    {
        LogProcessingEvent(eventType);

        using var scope = _scopeFactory.CreateScope();

        // Set tenant context from message attributes
        if (MessageTenantId is not null
            && scope.ServiceProvider.GetRequiredService<ITenantContext>() is TenantContext tc)
        {
            tc.TenantId = MessageTenantId;
        }

        var handlers = scope.ServiceProvider.GetRequiredService<MatchingCommandHandlers>();

        switch (eventType)
        {
            case "ProfileCreated":
                await HandleProfileCreated(handlers, messageBody, cancellationToken);
                break;
            case "LookingStatusChanged":
                await HandleLookingStatusChanged(handlers, messageBody, cancellationToken);
                break;
            default:
                LogUnknownEventType(eventType);
                break;
        }
    }

    private async Task HandleProfileCreated(MatchingCommandHandlers handlers, string body, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var hasPrefs = root.TryGetProperty("preferences", out var prefs);

        var command = new ProcessProfileCreatedCommand(
            UserId: root.GetProperty("userId").GetString()!,
            DisplayName: root.GetProperty("displayName").GetString()!,
            Gender: root.GetProperty("gender").GetString()!,
            PreferredGender: hasPrefs && prefs.TryGetProperty("preferredGender", out var pg) && pg.ValueKind != JsonValueKind.Null
                ? pg.GetString()
                : null,
            MinAge: hasPrefs && prefs.TryGetProperty("minAge", out var minAge) ? minAge.GetInt32() : 18,
            MaxAge: hasPrefs && prefs.TryGetProperty("maxAge", out var maxAge) ? maxAge.GetInt32() : 99,
            MaxDistanceKm: hasPrefs && prefs.TryGetProperty("maxDistanceKm", out var dist) ? dist.GetInt32() : 50
        );

        await handlers.HandleAsync(command, ct);
        LogProfileCreatedProcessed(command.UserId);
    }

    private async Task HandleLookingStatusChanged(MatchingCommandHandlers handlers, string body, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var userId = root.GetProperty("userId").GetString()!;
        var newStatus = root.GetProperty("newStatus").GetString()!;

        var command = new ProcessLookingStatusCommand(
            UserId: userId,
            DisplayName: "",
            Gender: "",
            PreferredGender: null,
            MinAge: 18,
            MaxAge: 99,
            MaxDistanceKm: 50,
            NewStatus: newStatus
        );

        await handlers.HandleAsync(command, ct);
        LogLookingStatusChangedProcessed(userId, newStatus);
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Processing event: {EventType}")]
    private partial void LogProcessingEvent(string eventType);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Warning, Message = "Unknown event type: {EventType}")]
    private partial void LogUnknownEventType(string eventType);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "Processed ProfileCreated for user {UserId}")]
    private partial void LogProfileCreatedProcessed(string userId);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Information, Message = "Processed LookingStatusChanged for user {UserId}: {Status}")]
    private partial void LogLookingStatusChangedProcessed(string userId, string status);
}
