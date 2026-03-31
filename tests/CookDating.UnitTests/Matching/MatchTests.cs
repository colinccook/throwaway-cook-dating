using CookDating.Matching.Domain;

namespace CookDating.UnitTests.Matching;

[TestFixture]
public class MatchTests
{
    [Test]
    public void Create_ShouldOrderUserIdsConsistently()
    {
        var match1 = Match.Create("bob", "alice");
        var match2 = Match.Create("alice", "bob");
        Assert.That(match1.Id, Is.EqualTo(match2.Id));
    }

    [Test]
    public void Create_WithSameUser_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => Match.Create("user-1", "user-1"));
    }

    [Test]
    public void InvolvesUser_ShouldReturnCorrectly()
    {
        var match = Match.Create("user-1", "user-2");
        Assert.That(match.InvolvesUser("user-1"), Is.True);
        Assert.That(match.InvolvesUser("user-3"), Is.False);
    }

    [Test]
    public void GetOtherUserId_ShouldReturnPartner()
    {
        var match = Match.Create("user-1", "user-2");
        Assert.That(match.GetOtherUserId("user-1"), Is.EqualTo("user-2"));
    }

    [Test]
    public void GetOtherUserId_WithNonParticipant_ShouldThrow()
    {
        var match = Match.Create("user-1", "user-2");
        Assert.Throws<InvalidOperationException>(() => match.GetOtherUserId("user-3"));
    }
}
