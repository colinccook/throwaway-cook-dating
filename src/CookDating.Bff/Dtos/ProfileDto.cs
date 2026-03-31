namespace CookDating.Bff.Dtos;

public record ProfileDto(
    string UserId,
    string DisplayName,
    string Bio,
    string DateOfBirth,
    string Gender,
    string? PreferredGender,
    int MinAge,
    int MaxAge,
    int MaxDistanceKm,
    List<string> PhotoUrls,
    string LookingStatus
);
