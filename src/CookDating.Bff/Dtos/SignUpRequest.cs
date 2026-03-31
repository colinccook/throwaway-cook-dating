namespace CookDating.Bff.Dtos;

public record SignUpRequest(
    string Email,
    string Password,
    string DisplayName,
    string DateOfBirth, // yyyy-MM-dd
    string Gender,
    string? PreferredGender,
    int MinAge,
    int MaxAge,
    int MaxDistanceKm
);
