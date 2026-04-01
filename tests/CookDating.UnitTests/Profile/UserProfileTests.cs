using CookDating.Profile.Domain;
using CookDating.Profile.Domain.Events;

namespace CookDating.UnitTests.Profile;

[TestFixture]
public class UserProfileTests
{
    private static DatingPreferences DefaultPreferences => new(Gender.Female, 18, 35, 50);
    private static DateOnly AdultDob => DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25));
    private static DateOnly MinorDob => DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16));
    private static readonly DateOnly ReferenceDate = new(2025, 1, 15);

    [Test]
    public void Create_WithValidDetails_ShouldCreateProfile()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);

        Assert.That(profile.Id, Is.EqualTo("user-1"));
        Assert.That(profile.DisplayName, Is.EqualTo("Alice"));
        Assert.That(profile.LookingStatus, Is.EqualTo(LookingStatus.NotLooking));
    }

    [Test]
    public void Create_ShouldRaiseProfileCreatedEvent()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);

        Assert.That(profile.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(profile.DomainEvents.First(), Is.TypeOf<ProfileCreated>());
    }

    [Test]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            UserProfile.Create("", "Alice", AdultDob, Gender.Female, DefaultPreferences));
    }

    [Test]
    public void Create_WithMinorDob_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            UserProfile.Create("user-1", "Alice", MinorDob, Gender.Female, DefaultPreferences));
    }

    [Test]
    public void Create_WithFutureDob_ShouldThrow()
    {
        var futureDob = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        Assert.Throws<ArgumentException>(() =>
            UserProfile.Create("user-1", "Alice", futureDob, Gender.Female, DefaultPreferences));
    }

    [Test]
    public void Create_WithAgeAboveMaximum_ShouldThrow()
    {
        var tooOldDob = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-(UserProfile.MaximumAllowedAge + 1)));
        Assert.Throws<ArgumentException>(() =>
            UserProfile.Create("user-1", "Alice", tooOldDob, Gender.Female, DefaultPreferences));
    }

    [Test]
    public void SetLookingStatus_ToActivelyLooking_ShouldRaiseEvent()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);
        profile.ClearDomainEvents();

        profile.SetLookingStatus(LookingStatus.ActivelyLooking);

        Assert.That(profile.LookingStatus, Is.EqualTo(LookingStatus.ActivelyLooking));
        Assert.That(profile.DomainEvents, Has.Count.EqualTo(1));
        var evt = (LookingStatusChanged)profile.DomainEvents.First();
        Assert.That(evt.NewStatus, Is.EqualTo(LookingStatus.ActivelyLooking));
        Assert.That(evt.PreviousStatus, Is.EqualTo(LookingStatus.NotLooking));
    }

    [Test]
    public void SetLookingStatus_ToSameStatus_ShouldNotRaiseEvent()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);
        profile.ClearDomainEvents();

        profile.SetLookingStatus(LookingStatus.NotLooking);

        Assert.That(profile.DomainEvents, Is.Empty);
    }

    [Test]
    public void UpdateProfile_ShouldUpdateFields()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);

        profile.UpdateProfile("Alice Updated", "New bio", ["photo1.jpg", "photo2.jpg"]);

        Assert.That(profile.DisplayName, Is.EqualTo("Alice Updated"));
        Assert.That(profile.Bio, Is.EqualTo("New bio"));
        Assert.That(profile.PhotoUrls, Has.Count.EqualTo(2));
    }

    [Test]
    public void UpdateProfile_WithEmptyName_ShouldThrow()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);

        Assert.Throws<ArgumentException>(() => profile.UpdateProfile("", "bio", []));
    }

    [Test]
    public void UpdateDateOfBirth_WithValidAge_ShouldUpdate()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);
        var newDob = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30));

        profile.UpdateDateOfBirth(newDob);

        Assert.That(profile.DateOfBirth, Is.EqualTo(newDob));
    }

    [Test]
    public void UpdateDateOfBirth_WithMinorAge_ShouldThrow()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);

        Assert.Throws<ArgumentException>(() => profile.UpdateDateOfBirth(MinorDob));
    }

    [Test]
    public void UpdateDateOfBirth_WithFutureDate_ShouldThrow()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);
        var futureDob = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        Assert.Throws<ArgumentException>(() => profile.UpdateDateOfBirth(futureDob));
    }

    [Test]
    public void UpdateGender_ShouldUpdateGender()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);

        profile.UpdateGender(Gender.NonBinary);

        Assert.That(profile.Gender, Is.EqualTo(Gender.NonBinary));
    }

    [Test]
    public void UpdatePreferences_ShouldReplacePreferences()
    {
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, DefaultPreferences);
        var newPrefs = new DatingPreferences(null, 21, 40, 100);

        profile.UpdatePreferences(newPrefs);

        Assert.That(profile.Preferences, Is.EqualTo(newPrefs));
    }

    [Test]
    public void Create_ProfileCreatedEvent_ContainsCorrectDetails()
    {
        var prefs = new DatingPreferences(Gender.Male, 20, 30, 50);
        var profile = UserProfile.Create("user-1", "Alice", AdultDob, Gender.Female, prefs);

        var evt = (ProfileCreated)profile.DomainEvents.First();
        Assert.That(evt.UserId, Is.EqualTo("user-1"));
        Assert.That(evt.DisplayName, Is.EqualTo("Alice"));
        Assert.That(evt.Gender, Is.EqualTo(Gender.Female));
        Assert.That(evt.Preferences, Is.EqualTo(prefs));
    }

    [Test]
    public void ValidateDateOfBirth_WithReferenceDate_EnforcesBounds()
    {
        var minBoundaryDob = new DateOnly(2007, 1, 15);
        var maxBoundaryDob = new DateOnly(1905, 1, 15);
        var tooOldDob = new DateOnly(1904, 1, 14);
        var futureDob = new DateOnly(2025, 1, 16);

        Assert.DoesNotThrow(() => UserProfile.ValidateDateOfBirth(minBoundaryDob, ReferenceDate));
        Assert.DoesNotThrow(() => UserProfile.ValidateDateOfBirth(maxBoundaryDob, ReferenceDate));
        Assert.Throws<ArgumentException>(() => UserProfile.ValidateDateOfBirth(tooOldDob, ReferenceDate));
        Assert.Throws<ArgumentException>(() => UserProfile.ValidateDateOfBirth(futureDob, ReferenceDate));
    }
}
