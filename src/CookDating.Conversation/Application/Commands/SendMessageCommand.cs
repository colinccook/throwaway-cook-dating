namespace CookDating.Conversation.Application.Commands;

public record SendMessageCommand(
    string ConversationId,
    string SenderId,
    string Content
);
