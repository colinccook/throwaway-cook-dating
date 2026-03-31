using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CookDating.Profile.Domain;
using CookDating.SharedKernel.Infrastructure;

namespace CookDating.Profile.Infrastructure;

public class DynamoDbProfileRepository : DynamoDbRepository<UserProfile, string>, IProfileRepository
{
    protected override string TableName => "Profiles";

    public DynamoDbProfileRepository(IAmazonDynamoDB dynamoDb) : base(dynamoDb) { }

    protected override Dictionary<string, AttributeValue> GetKey(string id) =>
        new() { ["UserId"] = new AttributeValue { S = id } };

    protected override Dictionary<string, AttributeValue> MapToAttributes(UserProfile profile) =>
        new()
        {
            ["UserId"] = new AttributeValue { S = profile.Id },
            ["DisplayName"] = new AttributeValue { S = profile.DisplayName },
            ["Bio"] = new AttributeValue { S = profile.Bio },
            ["DateOfBirth"] = new AttributeValue { S = profile.DateOfBirth.ToString("yyyy-MM-dd") },
            ["Gender"] = new AttributeValue { S = profile.Gender.ToString() },
            ["PreferredGender"] = profile.Preferences.PreferredGender.HasValue
                ? new AttributeValue { S = profile.Preferences.PreferredGender.Value.ToString() }
                : new AttributeValue { NULL = true },
            ["MinAge"] = new AttributeValue { N = profile.Preferences.MinAge.ToString() },
            ["MaxAge"] = new AttributeValue { N = profile.Preferences.MaxAge.ToString() },
            ["MaxDistanceKm"] = new AttributeValue { N = profile.Preferences.MaxDistanceKm.ToString() },
            ["PhotoUrls"] = profile.PhotoUrls.Count > 0
                ? new AttributeValue { L = profile.PhotoUrls.Select(u => new AttributeValue { S = u }).ToList() }
                : new AttributeValue { L = [] },
            ["LookingStatus"] = new AttributeValue { S = profile.LookingStatus.ToString() },
            ["CreatedAt"] = new AttributeValue { S = profile.CreatedAt.ToString("O") },
            ["UpdatedAt"] = new AttributeValue { S = profile.UpdatedAt.ToString("O") }
        };

    protected override UserProfile MapFromAttributes(Dictionary<string, AttributeValue> attrs)
    {
        var preferredGender = attrs.TryGetValue("PreferredGender", out var pg) && pg.NULL is not true
            ? Enum.Parse<Gender>(pg.S)
            : (Gender?)null;

        var preferences = new DatingPreferences(
            preferredGender,
            int.Parse(attrs["MinAge"].N),
            int.Parse(attrs["MaxAge"].N),
            int.Parse(attrs["MaxDistanceKm"].N)
        );

        var photoUrls = attrs.TryGetValue("PhotoUrls", out var photos) && photos.L != null
            ? photos.L.Select(a => a.S).ToList()
            : new List<string>();

        return UserProfile.Rehydrate(
            userId: attrs["UserId"].S,
            displayName: attrs["DisplayName"].S,
            bio: attrs.TryGetValue("Bio", out var bio) ? bio.S : string.Empty,
            dateOfBirth: DateOnly.Parse(attrs["DateOfBirth"].S),
            gender: Enum.Parse<Gender>(attrs["Gender"].S),
            preferences: preferences,
            photoUrls: photoUrls,
            lookingStatus: Enum.Parse<LookingStatus>(attrs["LookingStatus"].S),
            createdAt: DateTime.Parse(attrs["CreatedAt"].S),
            updatedAt: DateTime.Parse(attrs["UpdatedAt"].S)
        );
    }
}
