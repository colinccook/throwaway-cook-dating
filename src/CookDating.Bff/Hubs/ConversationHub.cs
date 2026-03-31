using System.Security.Claims;
using CookDating.Bff.Dtos;
using CookDating.Conversation.Application.Commands;
using CookDating.Conversation.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CookDating.Bff.Hubs;

[Authorize]
public class ConversationHub(ConversationCommandHandlers conversationCommandHandlers) : Hub
{
    private string GetUserId() =>
        Context.User?.FindFirst("sub")?.Value
        ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new HubException("User not authenticated");

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task GetConversations()
    {
        var userId = GetUserId();
        var conversations = await conversationCommandHandlers.HandleAsync(new GetConversationsCommand(userId));

        var conversationDtos = conversations.Select(c =>
        {
            var lastMessage = c.Messages.MaxBy(m => m.SentAt);
            return new ConversationDto(
                c.Id,
                c.MatchId,
                c.GetOtherParticipant(userId),
                lastMessage?.Content,
                lastMessage?.SentAt.ToString("o")
            );
        }).ToList();

        await Clients.Caller.SendAsync("ReceiveConversations", conversationDtos);
    }

    public async Task JoinConversation(string conversationId)
    {
        var userId = GetUserId();
        var conversation = await conversationCommandHandlers.HandleAsync(new GetConversationCommand(conversationId, userId));

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");

        var messageDtos = conversation.Messages.Select(m => new MessageDto(
            m.Id,
            m.SenderId,
            m.Content,
            m.SentAt.ToString("o"),
            m.IsRead
        )).ToList();

        await Clients.Caller.SendAsync("ReceiveMessages", messageDtos);
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
    }

    public async Task SendMessage(string conversationId, string content)
    {
        var userId = GetUserId();
        var message = await conversationCommandHandlers.HandleAsync(new SendMessageCommand(conversationId, userId, content));

        var messageDto = new MessageDto(
            message.Id,
            message.SenderId,
            message.Content,
            message.SentAt.ToString("o"),
            message.IsRead
        );

        await Clients.Group($"conversation:{conversationId}").SendAsync("ReceiveMessage", messageDto);
    }

    public async Task MarkRead(string conversationId)
    {
        var userId = GetUserId();
        await conversationCommandHandlers.HandleAsync(new MarkMessagesReadCommand(conversationId, userId));
    }
}
