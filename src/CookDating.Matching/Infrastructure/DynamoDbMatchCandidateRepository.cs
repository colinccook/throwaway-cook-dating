using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CookDating.Matching.Domain;
using CookDating.SharedKernel.Infrastructure;

namespace CookDating.Matching.Infrastructure;

public class DynamoDbMatchCandidateRepository : DynamoDbRepository<MatchCandidate, string>, IMatchCandidateRepository
{
    protected override string TableName => "MatchCandidates";

    public DynamoDbMatchCandidateRepository(IAmazonDynamoDB dynamoDb) : base(dynamoDb) { }

    protected override Dictionary<string, AttributeValue> GetKey(string id) =>
        new() { ["UserId"] = new AttributeValue { S = id } };

    protected override Dictionary<string, AttributeValue> MapToAttributes(MatchCandidate candidate)
    {
        var swipesJson = JsonSerializer.Serialize(candidate.Swipes.Select(s => new
        {
            targetUserId = s.TargetUserId,
            direction = s.Direction.ToString(),
            swipedAt = s.SwipedAt.ToString("O")
        }));

        return new()
        {
            ["UserId"] = new AttributeValue { S = candidate.Id },
            ["DisplayName"] = new AttributeValue { S = candidate.DisplayName },
            ["Gender"] = new AttributeValue { S = candidate.Gender },
            ["PreferredGender"] = candidate.PreferredGender != null
                ? new AttributeValue { S = candidate.PreferredGender }
                : new AttributeValue { NULL = true },
            ["MinAge"] = new AttributeValue { N = candidate.MinAge.ToString() },
            ["MaxAge"] = new AttributeValue { N = candidate.MaxAge.ToString() },
            ["MaxDistanceKm"] = new AttributeValue { N = candidate.MaxDistanceKm.ToString() },
            ["IsActive"] = new AttributeValue { BOOL = candidate.IsActive },
            ["Swipes"] = new AttributeValue { S = swipesJson }
        };
    }

    protected override MatchCandidate MapFromAttributes(Dictionary<string, AttributeValue> attrs)
    {
        var swipes = new List<Swipe>();
        if (attrs.TryGetValue("Swipes", out var swipesAttr) && !string.IsNullOrEmpty(swipesAttr.S))
        {
            using var doc = JsonDocument.Parse(swipesAttr.S);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                swipes.Add(new Swipe(
                    el.GetProperty("targetUserId").GetString()!,
                    Enum.Parse<SwipeDirection>(el.GetProperty("direction").GetString()!),
                    DateTime.Parse(el.GetProperty("swipedAt").GetString()!)
                ));
            }
        }

        return MatchCandidate.Rehydrate(
            userId: attrs["UserId"].S,
            displayName: attrs["DisplayName"].S,
            gender: attrs["Gender"].S,
            preferredGender: attrs.TryGetValue("PreferredGender", out var pg) && pg.NULL != true ? pg.S : null,
            minAge: int.Parse(attrs["MinAge"].N),
            maxAge: int.Parse(attrs["MaxAge"].N),
            maxDistanceKm: int.Parse(attrs["MaxDistanceKm"].N),
            isActive: attrs["IsActive"].BOOL ?? false,
            swipes: swipes
        );
    }

    public async Task<List<MatchCandidate>> GetActiveCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var request = new ScanRequest
        {
            TableName = TableName,
            FilterExpression = "IsActive = :active",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":active"] = new AttributeValue { BOOL = true }
            }
        };

        var response = await DynamoDb.ScanAsync(request, cancellationToken);
        return response.Items.Select(MapFromAttributes).ToList();
    }
}
