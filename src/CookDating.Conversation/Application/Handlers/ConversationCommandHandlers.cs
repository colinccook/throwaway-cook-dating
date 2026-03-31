using CookDating.Conversation.Application.Commands;
using CookDating.SharedKernel.Domain;
using ConversationAggregate = CookDating.Conversation.Domain.Conversation;

namespace CookDating.Conversation.Application.Handlers;

public class ConversationCommandHandlers
{
    private readonly Domain.IConversationRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public ConversationCommandHandlers(Domain.IConversationRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ConversationAggregate> HandleAsync(StartConversationCommand command, CancellationToken ct = default)
    {
        var existing = await _repository.GetByMatchIdAsync(command.MatchId, ct);
        if (existing != null) return existing;

        var conversation = ConversationAggregate.StartForMatch(
            command.MatchId,
            command.Participant1Id,
            command.Participant2Id
        );

        await _repository.SaveAsync(conversation, ct);
        conversation.ClearDomainEvents();

        return conversation;
    }

    public async Task<Domain.Message> HandleAsync(SendMessageCommand command, CancellationToken ct = default)
    {
        var conversation = await _repository.GetByIdAsync(command.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation not found: {command.ConversationId}");

        // This call enforces the match constraint - throws if sender is not a participant
        var message = conversation.SendMessage(command.SenderId, command.Content);

        await _repository.SaveAsync(conversation, ct);
        conversation.ClearDomainEvents();

        return message;
    }

    public async Task<List<ConversationAggregate>> HandleAsync(GetConversationsCommand command, CancellationToken ct = default)
    {
        return await _repository.GetConversationsForUserAsync(command.UserId, ct);
    }

    public async Task<ConversationAggregate> HandleAsync(GetConversationCommand command, CancellationToken ct = default)
    {
        var conversation = await _repository.GetByIdAsync(command.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation not found: {command.ConversationId}");

        if (!conversation.IsParticipant(command.UserId))
            throw new InvalidOperationException($"User {command.UserId} is not a participant in this conversation");

        return conversation;
    }

    public async Task HandleAsync(MarkMessagesReadCommand command, CancellationToken ct = default)
    {
        var conversation = await _repository.GetByIdAsync(command.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation not found: {command.ConversationId}");

        conversation.MarkMessagesAsRead(command.UserId);
        await _repository.SaveAsync(conversation, ct);
    }
}
