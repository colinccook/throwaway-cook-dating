namespace CookDating.Matching.Application.Commands;

public record ProcessLookingStatusCommand(
    string UserId,
    string DisplayName,
    string Gender,
    string? PreferredGender,
    int MinAge,
    int MaxAge,
    int MaxDistanceKm,
    string NewStatus // "ActivelyLooking" or "NotLooking"
);
