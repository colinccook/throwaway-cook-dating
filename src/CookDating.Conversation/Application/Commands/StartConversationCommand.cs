namespace CookDating.Conversation.Application.Commands;

public record StartConversationCommand(
    string MatchId,
    string Participant1Id,
    string Participant2Id
);
