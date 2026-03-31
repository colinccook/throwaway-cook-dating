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
public static partial class AwsBootstrapper
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
                LogTableCreated(logger, table.TableName);
            }
            catch (ResourceInUseException)
            {
                LogTableAlreadyExists(logger, table.TableName);
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
            LogTopicCreated(logger, name, response.TopicArn);
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
            LogQueueCreated(logger, name, queueArn);
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

        LogSubscriptionCreated(logger, queueArn, topicArn, response.SubscriptionArn);
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Created DynamoDB table {TableName}")]
    private static partial void LogTableCreated(ILogger logger, string tableName);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "DynamoDB table {TableName} already exists")]
    private static partial void LogTableAlreadyExists(ILogger logger, string tableName);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Created SNS topic {TopicName} ({TopicArn})")]
    private static partial void LogTopicCreated(ILogger logger, string topicName, string topicArn);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Created SQS queue {QueueName} ({QueueArn})")]
    private static partial void LogQueueCreated(ILogger logger, string queueName, string queueArn);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Subscribed {QueueArn} to {TopicArn} (SubscriptionArn: {SubscriptionArn})")]
    private static partial void LogSubscriptionCreated(ILogger logger, string queueArn, string topicArn, string subscriptionArn);
}

/// <summary>
/// Hosted service that runs the AWS bootstrapper on startup with retry logic
/// to handle the case where floci hasn't finished starting yet.
/// </summary>
public partial class AwsBootstrapHostedService(
    IConfiguration configuration,
    ILogger<AwsBootstrapHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceUrl = configuration["AWS:ServiceURL"];
        if (string.IsNullOrEmpty(serviceUrl))
        {
            LogServiceUrlNotConfigured(logger);
            return;
        }

        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                LogBootstrapAttempt(logger, attempt, maxRetries);
                await AwsBootstrapper.BootstrapAsync(serviceUrl, logger, stoppingToken);
                LogBootstrapCompleted(logger);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                LogBootstrapRetry(logger, ex, attempt, delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    [LoggerMessage(EventId = 2010, Level = LogLevel.Warning, Message = "AWS:ServiceURL is not configured; skipping AWS bootstrapping")]
    private static partial void LogServiceUrlNotConfigured(ILogger logger);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Information, Message = "AWS bootstrapping attempt {Attempt}/{MaxRetries}…")]
    private static partial void LogBootstrapAttempt(ILogger logger, int attempt, int maxRetries);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Information, Message = "AWS bootstrapping completed successfully")]
    private static partial void LogBootstrapCompleted(ILogger logger);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Warning, Message = "AWS bootstrapping attempt {Attempt} failed, retrying in {Delay}s…")]
    private static partial void LogBootstrapRetry(ILogger logger, Exception ex, int attempt, double delay);
}
