using CookDating.SharedKernel.Domain;

namespace CookDating.Conversation.Domain;

public interface IConversationRepository : IRepository<Conversation, string>
{
    Task<Conversation?> GetByMatchIdAsync(string matchId, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetConversationsForUserAsync(string userId, CancellationToken cancellationToken = default);
}
