using CookDating.SharedKernel.Domain;
using CookDating.Profile.Domain.Events;

namespace CookDating.Profile.Domain;

public class UserProfile : AggregateRoot<string>
{
    public const int MinimumAllowedAge = 18;
    public const int MaximumAllowedAge = 120;

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

        ValidateDateOfBirth(dateOfBirth);

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

    public void UpdateDateOfBirth(DateOnly dateOfBirth)
    {
        ValidateDateOfBirth(dateOfBirth);

        DateOfBirth = dateOfBirth;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateGender(Gender gender)
    {
        Gender = gender;
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

    public static void ValidateDateOfBirth(DateOnly dateOfBirth, DateOnly? today = null)
    {
        var referenceDate = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (dateOfBirth > referenceDate)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(dateOfBirth));

        var age = CalculateAge(dateOfBirth, referenceDate);
        if (age < MinimumAllowedAge)
            throw new ArgumentException($"Must be at least {MinimumAllowedAge} years old", nameof(dateOfBirth));
        if (age > MaximumAllowedAge)
            throw new ArgumentException($"Age cannot be greater than {MaximumAllowedAge} years", nameof(dateOfBirth));
    }

    private static int CalculateAge(DateOnly dateOfBirth, DateOnly? today = null)
    {
        var referenceDate = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var age = referenceDate.Year - dateOfBirth.Year;
        if (dateOfBirth > referenceDate.AddYears(-age)) age--;
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
