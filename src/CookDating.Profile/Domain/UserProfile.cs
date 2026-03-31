using CookDating.SharedKernel.Domain;
using CookDating.Profile.Domain.Events;

namespace CookDating.Profile.Domain;

public class UserProfile : AggregateRoot<string>
{
    public string DisplayName { get; private set; } = default!;
    public string Bio { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public DatingPreferences Preferences { get; private set; } = default!;
    public List<string> PhotoUrls { get; private set; } = [];
    public LookingStatus LookingStatus { get; private set; } = LookingStatus.NotLooking;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserProfile() { }

    public static UserProfile Create(
        string userId,
        string displayName,
        DateOnly dateOfBirth,
        Gender gender,
        DatingPreferences preferences)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required", nameof(displayName));

        var age = CalculateAge(dateOfBirth);
        if (age < 18)
            throw new ArgumentException("Must be at least 18 years old", nameof(dateOfBirth));

        var profile = new UserProfile
        {
            Id = userId,
            DisplayName = displayName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            Preferences = preferences,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        profile.RaiseDomainEvent(new ProfileCreated
        {
            UserId = userId,
            DisplayName = displayName,
            Gender = gender,
            Preferences = preferences
        });

        return profile;
    }

    public void UpdateProfile(string displayName, string bio, List<string> photoUrls)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required", nameof(displayName));

        DisplayName = displayName;
        Bio = bio;
        PhotoUrls = photoUrls ?? [];
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePreferences(DatingPreferences preferences)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLookingStatus(LookingStatus newStatus)
    {
        if (LookingStatus == newStatus) return;

        var previousStatus = LookingStatus;
        LookingStatus = newStatus;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new LookingStatusChanged
        {
            UserId = Id,
            NewStatus = newStatus,
            PreviousStatus = previousStatus
        });
    }

    public int GetAge() => CalculateAge(DateOfBirth);

    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age;
    }

    public static UserProfile Rehydrate(
        string userId, string displayName, string bio, DateOnly dateOfBirth,
        Gender gender, DatingPreferences preferences, List<string> photoUrls,
        LookingStatus lookingStatus, DateTime createdAt, DateTime updatedAt)
    {
        return new UserProfile
        {
            Id = userId,
            DisplayName = displayName,
            Bio = bio,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            Preferences = preferences,
            PhotoUrls = photoUrls,
            LookingStatus = lookingStatus,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
