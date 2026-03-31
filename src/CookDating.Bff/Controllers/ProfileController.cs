using CookDating.Bff.Dtos;
using CookDating.Matching.Application.Commands;
using CookDating.Matching.Application.Handlers;
using CookDating.Profile.Application.Commands;
using CookDating.Profile.Application.Handlers;
using CookDating.Profile.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookDating.Bff.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public partial class ProfileController : ControllerBase
{
    private readonly ProfileCommandHandlers _handlers;
    private readonly MatchingCommandHandlers _matchingHandlers;
    private readonly IProfileRepository _repository;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        ProfileCommandHandlers handlers,
        MatchingCommandHandlers matchingHandlers,
        IProfileRepository repository,
        ILogger<ProfileController> logger)
    {
        _handlers = handlers;
        _matchingHandlers = matchingHandlers;
        _repository = repository;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst("sub")?.Value
        ?? throw new InvalidOperationException("User ID not found in token");

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var userId = GetUserId();
        var profile = await _repository.GetByIdAsync(userId);
        if (profile == null)
        {
            LogProfileNotFound(userId);
            return NotFound();
        }

        return Ok(new ProfileDto(
            profile.Id, profile.DisplayName, profile.Bio,
            profile.DateOfBirth.ToString("yyyy-MM-dd"),
            profile.Gender.ToString(),
            profile.Preferences.PreferredGender?.ToString(),
            profile.Preferences.MinAge, profile.Preferences.MaxAge,
            profile.Preferences.MaxDistanceKm,
            profile.PhotoUrls, profile.LookingStatus.ToString()));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();

        Gender? preferredGender = request.PreferredGender is not null
            ? Enum.Parse<Gender>(request.PreferredGender) : null;

        await _handlers.HandleAsync(new UpdateProfileCommand(
            userId, request.DisplayName, request.Bio, request.PhotoUrls ?? [],
            DateOfBirth: request.DateOfBirth is not null ? DateOnly.Parse(request.DateOfBirth) : null,
            Gender: request.Gender is not null ? Enum.Parse<Gender>(request.Gender) : null,
            PreferredGender: preferredGender,
            MinAge: request.MinAge,
            MaxAge: request.MaxAge,
            MaxDistanceKm: request.MaxDistanceKm));

        LogProfileUpdated(userId);
        return NoContent();
    }

    [HttpPut("status")]
    public async Task<IActionResult> SetStatus([FromBody] SetStatusRequest request)
    {
        var userId = GetUserId();
        var status = Enum.Parse<LookingStatus>(request.Status);
        LogStatusChanging(userId, request.Status);
        await _handlers.HandleAsync(new SetLookingStatusCommand(userId, status));

        // Sync candidate state directly (avoids waiting for async worker processing)
        var profile = await _repository.GetByIdAsync(userId);
        if (profile != null)
        {
            await _matchingHandlers.HandleAsync(new ProcessLookingStatusCommand(
                userId, profile.DisplayName, profile.Gender.ToString(),
                profile.Preferences.PreferredGender?.ToString(),
                profile.Preferences.MinAge, profile.Preferences.MaxAge,
                profile.Preferences.MaxDistanceKm,
                request.Status));
        }

        return NoContent();
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Profile not found for user {UserId}")]
    private partial void LogProfileNotFound(string userId);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Information, Message = "Profile updated for user {UserId}")]
    private partial void LogProfileUpdated(string userId);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Changing looking status for user {UserId} to {Status}")]
    private partial void LogStatusChanging(string userId, string status);
}
