using CookDating.Bff.Dtos;
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
    private readonly IProfileRepository _repository;

    public ProfileController(ProfileCommandHandlers handlers, IProfileRepository repository)
    {
        _handlers = handlers;
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
        var status = Enum.Parse<LookingStatus>(request.Status);
        await _handlers.HandleAsync(new SetLookingStatusCommand(GetUserId(), status));
        return NoContent();
    }
}
