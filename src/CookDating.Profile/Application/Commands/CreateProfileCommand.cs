using CookDating.Profile.Domain;

namespace CookDating.Profile.Application.Commands;

public record CreateProfileCommand(
    string UserId,
    string DisplayName,
    DateOnly DateOfBirth,
    Gender Gender,
    Gender? PreferredGender,
    int MinAge,
    int MaxAge,
    int MaxDistanceKm
);
