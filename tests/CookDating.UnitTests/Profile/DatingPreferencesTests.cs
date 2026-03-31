using CookDating.Profile.Domain;

namespace CookDating.UnitTests.Profile;

[TestFixture]
public class DatingPreferencesTests
{
    [Test]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var prefs = new DatingPreferences(Gender.Female, 18, 30, 50);
        Assert.That(prefs.MinAge, Is.EqualTo(18));
    }

    [Test]
    public void Create_WithAgeBelowMinimum_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new DatingPreferences(null, 16, 30, 50));
    }

    [Test]
    public void Create_WithMaxAgeLessThanMin_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new DatingPreferences(null, 25, 20, 50));
    }

    [Test]
    public void Create_WithZeroDistance_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new DatingPreferences(null, 18, 30, 0));
    }

    [Test]
    public void TwoPreferencesWithSameValues_ShouldBeEqual()
    {
        var a = new DatingPreferences(Gender.Male, 18, 30, 50);
        var b = new DatingPreferences(Gender.Male, 18, 30, 50);
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void TwoPreferencesWithDifferentValues_ShouldNotBeEqual()
    {
        var a = new DatingPreferences(Gender.Male, 18, 30, 50);
        var b = new DatingPreferences(Gender.Female, 18, 30, 50);
        Assert.That(a, Is.Not.EqualTo(b));
    }
}
