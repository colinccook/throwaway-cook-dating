using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CookDating.Matching.Domain;
using CookDating.SharedKernel.Infrastructure;

namespace CookDating.Matching.Infrastructure;

public class DynamoDbMatchRepository : DynamoDbRepository<Match, string>, IMatchRepository
{
    protected override string TableName => "Matches";

    public DynamoDbMatchRepository(IAmazonDynamoDB dynamoDb) : base(dynamoDb) { }

    protected override Dictionary<string, AttributeValue> GetKey(string id) =>
        new() { ["MatchId"] = new AttributeValue { S = id } };

    protected override Dictionary<string, AttributeValue> MapToAttributes(Match match) =>
        new()
        {
            ["MatchId"] = new AttributeValue { S = match.Id },
            ["User1Id"] = new AttributeValue { S = match.User1Id },
            ["User2Id"] = new AttributeValue { S = match.User2Id },
            ["MatchedAt"] = new AttributeValue { S = match.MatchedAt.ToString("O") }
        };

    protected override Match MapFromAttributes(Dictionary<string, AttributeValue> attrs) =>
        Match.Rehydrate(
            matchId: attrs["MatchId"].S,
            user1Id: attrs["User1Id"].S,
            user2Id: attrs["User2Id"].S,
            matchedAt: DateTime.Parse(attrs["MatchedAt"].S)
        );

    public async Task<List<Match>> GetMatchesForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var matches1 = await QueryByIndexAsync(
            "User1IdIndex",
            "User1Id = :userId",
            new() { [":userId"] = new AttributeValue { S = userId } },
            cancellationToken);

        var matches2 = await QueryByIndexAsync(
            "User2IdIndex",
            "User2Id = :userId",
            new() { [":userId"] = new AttributeValue { S = userId } },
            cancellationToken);

        return matches1.Concat(matches2).DistinctBy(m => m.Id).ToList();
    }

    public async Task<Match?> GetMatchBetweenUsersAsync(string userId1, string userId2, CancellationToken cancellationToken = default)
    {
        var (first, second) = string.Compare(userId1, userId2, StringComparison.Ordinal) < 0
            ? (userId1, userId2)
            : (userId2, userId1);

        var matchId = $"{first}:{second}";
        return await GetByIdAsync(matchId, cancellationToken);
    }
}
