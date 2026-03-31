using System.Security.Claims;
using CookDating.Bff.Dtos;
using CookDating.Conversation.Application.Commands;
using CookDating.Conversation.Application.Handlers;
using CookDating.Matching.Application.Commands;
using CookDating.Matching.Application.Handlers;
using CookDating.Matching.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CookDating.Bff.Hubs;

[Authorize]
public class MatchingHub : Hub
{
    private readonly MatchingCommandHandlers _matchingHandlers;
    private readonly ConversationCommandHandlers _conversationHandlers;

    public MatchingHub(
        MatchingCommandHandlers matchingHandlers,
        ConversationCommandHandlers conversationHandlers)
    {
        _matchingHandlers = matchingHandlers;
        _conversationHandlers = conversationHandlers;
    }

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

    public async Task GetCandidates()
    {
        var userId = GetUserId();
        var candidates = await _matchingHandlers.HandleAsync(new GetCandidatesCommand(userId));

        var dtos = candidates.Select(c => new CandidateDto(
            UserId: c.Id,
            DisplayName: c.DisplayName,
            Gender: c.Gender
        )).ToList();

        await Clients.Caller.SendAsync("ReceiveCandidates", dtos);
    }

    public async Task Swipe(SwipeDto swipe)
    {
        var userId = GetUserId();

        if (!Enum.TryParse<SwipeDirection>(swipe.Direction, ignoreCase: true, out var direction))
            throw new HubException($"Invalid swipe direction: {swipe.Direction}");

        var (match, isMatch) = await _matchingHandlers.HandleAsync(
            new SwipeCommand(userId, swipe.TargetUserId, direction));

        if (isMatch && match is not null)
        {
            var otherUserId = match.GetOtherUserId(userId);

            var matchDtoForCaller = new MatchDto(
                match.Id,
                otherUserId,
                swipe.TargetUserId,
                match.MatchedAt.ToString("o"));

            var matchDtoForOther = new MatchDto(
                match.Id,
                userId,
                userId,
                match.MatchedAt.ToString("o"));

            await Clients.Caller.SendAsync("MatchFound", matchDtoForCaller);
            await Clients.Group(otherUserId).SendAsync("MatchFound", matchDtoForOther);

            // Create conversation immediately (workers also process via events)
            await _conversationHandlers.HandleAsync(
                new StartConversationCommand(match.Id, userId, otherUserId));
        }
    }

    private string GetUserId()
    {
        var userId = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return userId ?? throw new HubException("User ID not found in token claims");
    }
}
