using CookDating.Profile.Application.Commands;
using CookDating.Profile.Domain;
using CookDating.SharedKernel.Domain;

namespace CookDating.Profile.Application.Handlers;

public class ProfileCommandHandlers
{
    private readonly IProfileRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private const string ProfileEventsTopicArn = "arn:aws:sns:us-east-1:000000000000:profile-events";

    public ProfileCommandHandlers(IProfileRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<UserProfile> HandleAsync(CreateProfileCommand command, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(command.UserId, ct);
        if (existing != null)
            throw new InvalidOperationException($"Profile already exists for user {command.UserId}");

        var preferences = new DatingPreferences(
            command.PreferredGender,
            command.MinAge,
            command.MaxAge,
            command.MaxDistanceKm
        );

        var profile = UserProfile.Create(
            command.UserId,
            command.DisplayName,
            command.DateOfBirth,
            command.Gender,
            preferences
        );

        await _repository.SaveAsync(profile, ct);
        await _eventPublisher.PublishAsync(profile.DomainEvents, ProfileEventsTopicArn, ct);
        profile.ClearDomainEvents();

        return profile;
    }

    public async Task HandleAsync(UpdateProfileCommand command, CancellationToken ct = default)
    {
        var profile = await _repository.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Profile not found for user {command.UserId}");

        profile.UpdateProfile(command.DisplayName, command.Bio, command.PhotoUrls);

        if (command.DateOfBirth.HasValue)
            profile.UpdateDateOfBirth(command.DateOfBirth.Value);

        if (command.Gender.HasValue)
            profile.UpdateGender(command.Gender.Value);

        var prefsChanged = command.PreferredGender is not null
            || command.MinAge.HasValue || command.MaxAge.HasValue || command.MaxDistanceKm.HasValue;

        if (prefsChanged)
        {
            var current = profile.Preferences;
            var newPrefs = new DatingPreferences(
                command.PreferredGender ?? current.PreferredGender,
                command.MinAge ?? current.MinAge,
                command.MaxAge ?? current.MaxAge,
                command.MaxDistanceKm ?? current.MaxDistanceKm
            );
            profile.UpdatePreferences(newPrefs);
        }

        await _repository.SaveAsync(profile, ct);
    }

    public async Task HandleAsync(SetLookingStatusCommand command, CancellationToken ct = default)
    {
        var profile = await _repository.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Profile not found for user {command.UserId}");

        profile.SetLookingStatus(command.Status);

        await _repository.SaveAsync(profile, ct);
        await _eventPublisher.PublishAsync(profile.DomainEvents, ProfileEventsTopicArn, ct);
        profile.ClearDomainEvents();
    }
}
