using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CookDating.SharedKernel.Infrastructure;

public abstract partial class SqsMessageConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger _logger;
    protected abstract string QueueName { get; }
    private string? _resolvedQueueUrl;
    protected string? MessageTenantId { get; private set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected SqsMessageConsumer(IAmazonSQS sqsClient, ILogger logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
    }

    private async Task<string> GetQueueUrlAsync(CancellationToken ct)
    {
        if (_resolvedQueueUrl != null) return _resolvedQueueUrl;

        for (var attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                var response = await _sqsClient.GetQueueUrlAsync(QueueName, ct);
                _resolvedQueueUrl = response.QueueUrl;
                LogQueueUrlResolved(QueueName, _resolvedQueueUrl);
                return _resolvedQueueUrl;
            }
            catch (Exception ex) when (attempt < 30)
            {
                LogQueueNotFound(ex, QueueName, attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }

        throw new InvalidOperationException($"Could not resolve queue URL for {QueueName}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = await GetQueueUrlAsync(stoppingToken);
        LogConsumerStarted(queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5,
                    MessageSystemAttributeNames = ["All"],
                    MessageAttributeNames = ["All"]
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                foreach (var message in response.Messages)
                {
                    try
                    {
                        // SNS wraps messages in an envelope — unwrap it
                        var body = message.Body;
                        SnsEnvelope? snsEnvelope = null;
                        if (body.Contains("\"Type\":\"Notification\""))
                        {
                            snsEnvelope = JsonSerializer.Deserialize<SnsEnvelope>(body, JsonOptions);
                            body = snsEnvelope?.Message ?? body;
                        }

                        var eventType = ExtractEventType(message, body);
                        MessageTenantId = ExtractTenantId(message, snsEnvelope);
                        await HandleMessageAsync(eventType, body, stoppingToken);

                        await _sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        LogMessageProcessingError(ex, message.MessageId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogReceiveError(ex);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private string ExtractEventType(Message message, string body)
    {
        // Try message attributes first (set by SnsEventPublisher)
        if (message.MessageAttributes.TryGetValue("EventType", out var attr))
            return attr.StringValue;

        // Try to extract from JSON body
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("eventType", out var et))
                return et.GetString() ?? "Unknown";
        }
        catch (Exception ex)
        {
            LogEventTypeExtractionFailed(ex);
        }

        return "Unknown";
    }

    private static string? ExtractTenantId(Message message, SnsEnvelope? snsEnvelope)
    {
        // Try SQS message attributes first
        if (message.MessageAttributes.TryGetValue("TenantId", out var attr))
            return attr.StringValue;

        // Try SNS envelope message attributes
        if (snsEnvelope?.MessageAttributes?.TryGetValue("TenantId", out var snsAttr) == true)
            return snsAttr.Value;

        return null;
    }

    protected abstract Task HandleMessageAsync(string eventType, string messageBody, CancellationToken cancellationToken);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Resolved queue URL for {QueueName}: {QueueUrl}")]
    private partial void LogQueueUrlResolved(string queueName, string queueUrl);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Queue {QueueName} not found (attempt {Attempt}), retrying...")]
    private partial void LogQueueNotFound(Exception ex, string queueName, int attempt);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Starting SQS consumer for queue: {QueueUrl}")]
    private partial void LogConsumerStarted(string queueUrl);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Error processing SQS message {MessageId}")]
    private partial void LogMessageProcessingError(Exception ex, string messageId);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error, Message = "Error receiving messages from SQS")]
    private partial void LogReceiveError(Exception ex);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Debug, Message = "Failed to extract event type from message body")]
    private partial void LogEventTypeExtractionFailed(Exception ex);

    private sealed record SnsEnvelope(
        string Type,
        string Message,
        string MessageId,
        string TopicArn,
        Dictionary<string, SnsMessageAttribute>? MessageAttributes = null);

    private sealed record SnsMessageAttribute(string Type, string Value);
}
