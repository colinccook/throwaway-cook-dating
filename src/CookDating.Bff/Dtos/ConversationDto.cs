namespace CookDating.Bff.Dtos;

public record ConversationDto(string ConversationId, string MatchId, string OtherUserId, string? LastMessage, string? LastMessageAt);
