using CookDating.Profile.Domain;

namespace CookDating.Profile.Application.Commands;

public record UpdateProfileCommand(
    string UserId,
    string DisplayName,
    string Bio,
    List<string> PhotoUrls,
    DateOnly? DateOfBirth = null,
    Gender? Gender = null,
    Gender? PreferredGender = null,
    int? MinAge = null,
    int? MaxAge = null,
    int? MaxDistanceKm = null
);
