namespace CookDating.Bff.Dtos;

public record UpdateProfileRequest(
    string DisplayName,
    string Bio,
    string? DateOfBirth = null,
    string? Gender = null,
    string? PreferredGender = null,
    int? MinAge = null,
    int? MaxAge = null,
    int? MaxDistanceKm = null,
    List<string>? PhotoUrls = null
);
