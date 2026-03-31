using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CookDating.SharedKernel.Infrastructure;

/// <summary>
/// Creates the required AWS resources (DynamoDB tables, SNS topics, SQS queues)
/// on floci at startup so the application has everything it needs.
/// </summary>
public static class AwsBootstrapper
{
    public static async Task BootstrapAsync(string serviceUrl, ILogger logger, CancellationToken cancellationToken = default)
    {
        var credentials = new BasicAWSCredentials("test", "test");

        using var dynamoClient = new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig { ServiceURL = serviceUrl });
        using var snsClient = new AmazonSimpleNotificationServiceClient(credentials, new AmazonSimpleNotificationServiceConfig { ServiceURL = serviceUrl });
        using var sqsClient = new AmazonSQSClient(credentials, new AmazonSQSConfig { ServiceURL = serviceUrl });

        await CreateDynamoDbTablesAsync(dynamoClient, logger, cancellationToken);
        var topicArns = await CreateSnsTopicsAsync(snsClient, logger, cancellationToken);
        var queueDetails = await CreateSqsQueuesAsync(sqsClient, logger, cancellationToken);
        await SubscribeQueuesToTopicsAsync(snsClient, topicArns, queueDetails, logger, cancellationToken);
    }

    private static async Task CreateDynamoDbTablesAsync(IAmazonDynamoDB client, ILogger logger, CancellationToken ct)
    {
        var tables = new[]
        {
            SimpleTable("Profiles", "UserId"),
            SimpleTable("MatchCandidates", "UserId"),
            MatchesTable(),
            ConversationsTable()
        };

        foreach (var table in tables)
        {
            try
            {
                await client.CreateTableAsync(table, ct);
                logger.LogInformation("Created DynamoDB table {TableName}", table.TableName);
            }
            catch (ResourceInUseException)
            {
                logger.LogInformation("DynamoDB table {TableName} already exists", table.TableName);
            }
        }
    }

    private static CreateTableRequest SimpleTable(string name, string pk) => new()
    {
        TableName = name,
        KeySchema = [new KeySchemaElement { AttributeName = pk, KeyType = KeyType.HASH }],
        AttributeDefinitions = [new AttributeDefinition { AttributeName = pk, AttributeType = ScalarAttributeType.S }],
        BillingMode = BillingMode.PAY_PER_REQUEST
    };

    private static CreateTableRequest MatchesTable() => new()
    {
        TableName = "Matches",
        KeySchema = [new KeySchemaElement { AttributeName = "MatchId", KeyType = KeyType.HASH }],
        AttributeDefinitions =
        [
            new AttributeDefinition { AttributeName = "MatchId", AttributeType = ScalarAttributeType.S },
            new AttributeDefinition { AttributeName = "User1Id", AttributeType = ScalarAttributeType.S },
            new AttributeDefinition { AttributeName = "User2Id", AttributeType = ScalarAttributeType.S }
        ],
        GlobalSecondaryIndexes =
        [
            new GlobalSecondaryIndex
            {
                IndexName = "User1Id-index",
                KeySchema = [new KeySchemaElement { AttributeName = "User1Id", KeyType = KeyType.HASH }],
                Projection = new Projection { ProjectionType = ProjectionType.ALL }
            },
            new GlobalSecondaryIndex
            {
                IndexName = "User2Id-index",
                KeySchema = [new KeySchemaElement { AttributeName = "User2Id", KeyType = KeyType.HASH }],
                Projection = new Projection { ProjectionType = ProjectionType.ALL }
            }
        ],
        BillingMode = BillingMode.PAY_PER_REQUEST
    };

    private static CreateTableRequest ConversationsTable() => new()
    {
        TableName = "Conversations",
        KeySchema = [new KeySchemaElement { AttributeName = "ConversationId", KeyType = KeyType.HASH }],
        AttributeDefinitions =
        [
            new AttributeDefinition { AttributeName = "ConversationId", AttributeType = ScalarAttributeType.S },
            new AttributeDefinition { AttributeName = "MatchId", AttributeType = ScalarAttributeType.S },
            new AttributeDefinition { AttributeName = "Participant1Id", AttributeType = ScalarAttributeType.S },
            new AttributeDefinition { AttributeName = "Participant2Id", AttributeType = ScalarAttributeType.S }
        ],
        GlobalSecondaryIndexes =
        [
            new GlobalSecondaryIndex
            {
                IndexName = "MatchIdIndex",
                KeySchema = [new KeySchemaElement { AttributeName = "MatchId", KeyType = KeyType.HASH }],
                Projection = new Projection { ProjectionType = ProjectionType.ALL }
            },
            new GlobalSecondaryIndex
            {
                IndexName = "Participant1IdIndex",
                KeySchema = [new KeySchemaElement { AttributeName = "Participant1Id", KeyType = KeyType.HASH }],
                Projection = new Projection { ProjectionType = ProjectionType.ALL }
            },
            new GlobalSecondaryIndex
            {
                IndexName = "Participant2IdIndex",
                KeySchema = [new KeySchemaElement { AttributeName = "Participant2Id", KeyType = KeyType.HASH }],
                Projection = new Projection { ProjectionType = ProjectionType.ALL }
            }
        ],
        BillingMode = BillingMode.PAY_PER_REQUEST
    };

    private static async Task<Dictionary<string, string>> CreateSnsTopicsAsync(
        IAmazonSimpleNotificationService client, ILogger logger, CancellationToken ct)
    {
        var topicArns = new Dictionary<string, string>();
        string[] topicNames = ["profile-events", "matching-events"];

        foreach (var name in topicNames)
        {
            var response = await client.CreateTopicAsync(new CreateTopicRequest { Name = name }, ct);
            topicArns[name] = response.TopicArn;
            logger.LogInformation("Created SNS topic {TopicName} ({TopicArn})", name, response.TopicArn);
        }

        return topicArns;
    }

    private static async Task<Dictionary<string, (string QueueUrl, string QueueArn)>> CreateSqsQueuesAsync(
        IAmazonSQS client, ILogger logger, CancellationToken ct)
    {
        var queues = new Dictionary<string, (string QueueUrl, string QueueArn)>();
        string[] queueNames = ["matching-queue", "conversation-queue"];

        foreach (var name in queueNames)
        {
            var createResponse = await client.CreateQueueAsync(new CreateQueueRequest { QueueName = name }, ct);

            var attrsResponse = await client.GetQueueAttributesAsync(new GetQueueAttributesRequest
            {
                QueueUrl = createResponse.QueueUrl,
                AttributeNames = ["QueueArn"]
            }, ct);

            var queueArn = attrsResponse.Attributes["QueueArn"];
            queues[name] = (createResponse.QueueUrl, queueArn);
            logger.LogInformation("Created SQS queue {QueueName} ({QueueArn})", name, queueArn);
        }

        return queues;
    }

    private static async Task SubscribeQueuesToTopicsAsync(
        IAmazonSimpleNotificationService snsClient,
        Dictionary<string, string> topicArns,
        Dictionary<string, (string QueueUrl, string QueueArn)> queues,
        ILogger logger,
        CancellationToken ct)
    {
        // matching-queue subscribes to profile-events
        await SubscribeAsync(snsClient, topicArns["profile-events"], queues["matching-queue"].QueueArn, logger, ct);

        // conversation-queue subscribes to matching-events
        await SubscribeAsync(snsClient, topicArns["matching-events"], queues["conversation-queue"].QueueArn, logger, ct);
    }

    private static async Task SubscribeAsync(
        IAmazonSimpleNotificationService snsClient,
        string topicArn, string queueArn,
        ILogger logger, CancellationToken ct)
    {
        var response = await snsClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn
        }, ct);

        logger.LogInformation(
            "Subscribed {QueueArn} to {TopicArn} (SubscriptionArn: {SubscriptionArn})",
            queueArn, topicArn, response.SubscriptionArn);
    }
}

/// <summary>
/// Hosted service that runs the AWS bootstrapper on startup with retry logic
/// to handle the case where floci hasn't finished starting yet.
/// </summary>
public class AwsBootstrapHostedService(
    IConfiguration configuration,
    ILogger<AwsBootstrapHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceUrl = configuration["AWS:ServiceURL"];
        if (string.IsNullOrEmpty(serviceUrl))
        {
            logger.LogWarning("AWS:ServiceURL is not configured; skipping AWS bootstrapping");
            return;
        }

        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("AWS bootstrapping attempt {Attempt}/{MaxRetries}…", attempt, maxRetries);
                await AwsBootstrapper.BootstrapAsync(serviceUrl, logger, stoppingToken);
                logger.LogInformation("AWS bootstrapping completed successfully");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                logger.LogWarning(ex,
                    "AWS bootstrapping attempt {Attempt} failed, retrying in {Delay}s…",
                    attempt, delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
