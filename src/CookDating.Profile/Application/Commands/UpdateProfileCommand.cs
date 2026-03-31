namespace CookDating.Profile.Application.Commands;

public record UpdateProfileCommand(
    string UserId,
    string DisplayName,
    string Bio,
    List<string> PhotoUrls
);
