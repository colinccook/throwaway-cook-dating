namespace CookDating.Bff.Dtos;

public record MessageDto(string Id, string SenderId, string Content, string SentAt, bool IsRead);
