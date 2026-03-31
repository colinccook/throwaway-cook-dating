namespace CookDating.Matching.Application.Commands;

public record ProcessProfileCreatedCommand(
    string UserId,
    string DisplayName,
    string Gender,
    string? PreferredGender,
    int MinAge,
    int MaxAge,
    int MaxDistanceKm
);
