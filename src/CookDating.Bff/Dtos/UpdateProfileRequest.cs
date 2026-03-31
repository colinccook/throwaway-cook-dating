namespace CookDating.Bff.Dtos;

public record UpdateProfileRequest(string DisplayName, string Bio, List<string> PhotoUrls);
