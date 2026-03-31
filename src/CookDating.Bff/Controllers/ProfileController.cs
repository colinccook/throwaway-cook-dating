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
public class ProfileController : ControllerBase
{
    private readonly ProfileCommandHandlers _handlers;
    private readonly MatchingCommandHandlers _matchingHandlers;
    private readonly IProfileRepository _repository;

    public ProfileController(
        ProfileCommandHandlers handlers,
        MatchingCommandHandlers matchingHandlers,
        IProfileRepository repository)
    {
        _handlers = handlers;
        _matchingHandlers = matchingHandlers;
        _repository = repository;
    }

    private string GetUserId() => User.FindFirst("sub")?.Value
        ?? throw new InvalidOperationException("User ID not found in token");

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var profile = await _repository.GetByIdAsync(GetUserId());
        if (profile == null) return NotFound();

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
        await _handlers.HandleAsync(new UpdateProfileCommand(
            GetUserId(), request.DisplayName, request.Bio, request.PhotoUrls));
        return NoContent();
    }

    [HttpPut("status")]
    public async Task<IActionResult> SetStatus([FromBody] SetStatusRequest request)
    {
        var userId = GetUserId();
        var status = Enum.Parse<LookingStatus>(request.Status);
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
}
