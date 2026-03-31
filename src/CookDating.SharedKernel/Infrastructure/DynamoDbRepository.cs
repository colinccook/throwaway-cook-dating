using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CookDating.SharedKernel.Domain;

namespace CookDating.SharedKernel.Infrastructure;

public abstract class DynamoDbRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    protected readonly IAmazonDynamoDB DynamoDb;
    protected abstract string TableName { get; }

    protected DynamoDbRepository(IAmazonDynamoDB dynamoDb)
    {
        DynamoDb = dynamoDb;
    }

    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest
        {
            TableName = TableName,
            Key = GetKey(id)
        };

        var response = await DynamoDb.GetItemAsync(request, cancellationToken);
        if (!response.IsItemSet) return null;

        return MapFromAttributes(response.Item);
    }

    public async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = MapToAttributes(aggregate)
        };

        await DynamoDb.PutItemAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        var request = new DeleteItemRequest
        {
            TableName = TableName,
            Key = GetKey(id)
        };

        await DynamoDb.DeleteItemAsync(request, cancellationToken);
    }

    // Query helper for GSI lookups
    protected async Task<List<TAggregate>> QueryByIndexAsync(
        string indexName,
        string keyConditionExpression,
        Dictionary<string, AttributeValue> expressionAttributeValues,
        CancellationToken cancellationToken = default)
    {
        var request = new QueryRequest
        {
            TableName = TableName,
            IndexName = indexName,
            KeyConditionExpression = keyConditionExpression,
            ExpressionAttributeValues = expressionAttributeValues
        };

        var response = await DynamoDb.QueryAsync(request, cancellationToken);
        return response.Items.Select(MapFromAttributes).ToList()!;
    }

    protected abstract Dictionary<string, AttributeValue> GetKey(TId id);
    protected abstract Dictionary<string, AttributeValue> MapToAttributes(TAggregate aggregate);
    protected abstract TAggregate MapFromAttributes(Dictionary<string, AttributeValue> attributes);
}
