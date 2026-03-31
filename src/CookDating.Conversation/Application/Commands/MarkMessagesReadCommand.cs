namespace CookDating.Conversation.Application.Commands;

public record MarkMessagesReadCommand(string ConversationId, string UserId);
