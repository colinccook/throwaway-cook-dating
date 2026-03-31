using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CookDating.SharedKernel.Infrastructure;

public abstract class SqsMessageConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger _logger;
    protected abstract string QueueUrl { get; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected SqsMessageConsumer(IAmazonSQS sqsClient, ILogger logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting SQS consumer for queue: {QueueUrl}", QueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = QueueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20, // Long polling
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
                        if (body.Contains("\"Type\":\"Notification\""))
                        {
                            var snsEnvelope = JsonSerializer.Deserialize<SnsEnvelope>(body, JsonOptions);
                            body = snsEnvelope?.Message ?? body;
                        }

                        var eventType = ExtractEventType(message, body);
                        await HandleMessageAsync(eventType, body, stoppingToken);

                        await _sqsClient.DeleteMessageAsync(QueueUrl, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing SQS message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from SQS");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private static string ExtractEventType(Message message, string body)
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
        catch { }

        return "Unknown";
    }

    protected abstract Task HandleMessageAsync(string eventType, string messageBody, CancellationToken cancellationToken);

    private sealed record SnsEnvelope(string Type, string Message, string MessageId, string TopicArn);
}
