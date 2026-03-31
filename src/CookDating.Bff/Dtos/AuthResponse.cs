namespace CookDating.Bff.Dtos;

public record AuthResponse(string AccessToken, string UserId, string Email);
