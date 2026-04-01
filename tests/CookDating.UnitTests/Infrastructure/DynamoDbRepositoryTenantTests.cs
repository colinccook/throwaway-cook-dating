using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CookDating.SharedKernel.Domain;
using CookDating.SharedKernel.Infrastructure;
using Moq;

namespace CookDating.UnitTests.Infrastructure;

[TestFixture]
public class DynamoDbRepositoryTenantTests
{
    private Mock<IAmazonDynamoDB> _dynamoDb = null!;
    private TenantContext _tenantContext = null!;
    private TestRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _dynamoDb = new Mock<IAmazonDynamoDB>();
        _tenantContext = new TenantContext { TenantId = "cook-dating" };
        _repository = new TestRepository(_dynamoDb.Object, _tenantContext);
    }

    [Test]
    public async Task SaveAsync_AddsTenantIdAttribute()
    {
        PutItemRequest? capturedRequest = null;
        _dynamoDb.Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutItemRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutItemResponse());

        var entity = TestEntity.Create("e1");
        await _repository.SaveAsync(entity);

        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Item.ContainsKey("TenantId"), Is.True);
        Assert.That(capturedRequest.Item["TenantId"].S, Is.EqualTo("cook-dating"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsItem_WhenTenantMatches()
    {
        _dynamoDb.Setup(x => x.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new() { S = "e1" },
                    ["TenantId"] = new() { S = "cook-dating" }
                }
            });

        var result = await _repository.GetByIdAsync("e1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("e1"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenTenantMismatches()
    {
        _dynamoDb.Setup(x => x.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new() { S = "e1" },
                    ["TenantId"] = new() { S = "tech-dating" }
                }
            });

        var result = await _repository.GetByIdAsync("e1");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_ReturnsItem_WhenNoTenantId_LegacyItem()
    {
        _dynamoDb.Setup(x => x.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new() { S = "legacy-1" }
                }
            });

        var result = await _repository.GetByIdAsync("legacy-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("legacy-1"));
    }

    [Test]
    public async Task QueryByIndexAsync_IncludesTenantIdFilter()
    {
        QueryRequest? capturedRequest = null;
        _dynamoDb.Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<QueryRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new QueryResponse { Items = [] });

        await _repository.TestQueryByIndex("test-index", "PK = :pk",
            new Dictionary<string, AttributeValue> { [":pk"] = new() { S = "val" } });

        Assert.That(capturedRequest, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest!.FilterExpression, Is.EqualTo("TenantId = :tenantId"));
            Assert.That(capturedRequest.ExpressionAttributeValues.ContainsKey(":tenantId"), Is.True);
            Assert.That(capturedRequest.ExpressionAttributeValues[":tenantId"].S, Is.EqualTo("cook-dating"));
        });
    }

    // --- Test aggregate and repository ---

    private sealed class TestEntity : AggregateRoot<string>
    {
        public static TestEntity Create(string id)
        {
            var entity = new TestEntity();
            entity.Id = id;
            return entity;
        }
    }

    private sealed class TestRepository(IAmazonDynamoDB dynamoDb, ITenantContext tenantContext)
        : DynamoDbRepository<TestEntity, string>(dynamoDb, tenantContext)
    {
        protected override string TableName => "TestTable";

        protected override Dictionary<string, AttributeValue> GetKey(string id) =>
            new() { ["Id"] = new AttributeValue { S = id } };

        protected override Dictionary<string, AttributeValue> MapToAttributes(TestEntity aggregate) =>
            new() { ["Id"] = new AttributeValue { S = aggregate.Id } };

        protected override TestEntity MapFromAttributes(Dictionary<string, AttributeValue> attributes)
        {
            var entity = new TestEntity();
            typeof(TestEntity).BaseType!.BaseType!.GetProperty("Id")!
                .SetValue(entity, attributes["Id"].S);
            return entity;
        }

        public Task<List<TestEntity>> TestQueryByIndex(
            string indexName,
            string keyCondition,
            Dictionary<string, AttributeValue> values,
            CancellationToken ct = default) =>
            QueryByIndexAsync(indexName, keyCondition, values, ct);
    }
}
