using CookDating.Matching.Domain;
using CookDating.Matching.Domain.Events;

namespace CookDating.UnitTests.Matching;

[TestFixture]
public class MatchCandidateTests
{
    private static MatchCandidate CreateActiveCandidate(string id = "user-1") 
    {
        var candidate = MatchCandidate.Create(id, "Alice", "Female", "Male", 18, 35, 50);
        candidate.Activate();
        return candidate;
    }

    [Test]
    public void Create_ShouldInitializeInactive()
    {
        var candidate = MatchCandidate.Create("user-1", "Alice", "Female", null, 18, 35, 50);
        Assert.That(candidate.IsActive, Is.False);
    }

    [Test]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var candidate = MatchCandidate.Create("user-1", "Alice", "Female", null, 18, 35, 50);
        candidate.Activate();
        Assert.That(candidate.IsActive, Is.True);
    }

    [Test]
    public void RecordSwipe_WhenNotActive_ShouldThrow()
    {
        var candidate = MatchCandidate.Create("user-1", "Alice", "Female", null, 18, 35, 50);
        Assert.Throws<InvalidOperationException>(() => candidate.RecordSwipe("user-2", SwipeDirection.Right, false));
    }

    [Test]
    public void RecordSwipe_Right_WithoutMutualLike_ShouldReturnNull()
    {
        var candidate = CreateActiveCandidate();
        var match = candidate.RecordSwipe("user-2", SwipeDirection.Right, targetHasLikedMe: false);
        Assert.That(match, Is.Null);
    }

    [Test]
    public void RecordSwipe_Right_WithMutualLike_ShouldReturnMatch()
    {
        var candidate = CreateActiveCandidate();
        var match = candidate.RecordSwipe("user-2", SwipeDirection.Right, targetHasLikedMe: true);
        Assert.That(match, Is.Not.Null);
        Assert.That(match!.InvolvesUser("user-1"), Is.True);
        Assert.That(match.InvolvesUser("user-2"), Is.True);
    }

    [Test]
    public void RecordSwipe_Right_WithMutualLike_ShouldRaiseMatchCreatedEvent()
    {
        var candidate = CreateActiveCandidate();
        candidate.ClearDomainEvents();
        candidate.RecordSwipe("user-2", SwipeDirection.Right, targetHasLikedMe: true);
        
        Assert.That(candidate.DomainEvents, Has.Count.EqualTo(2)); // SwipeRecorded + MatchCreated
        Assert.That(candidate.DomainEvents.Any(e => e is MatchCreated), Is.True);
    }

    [Test]
    public void RecordSwipe_Left_ShouldNeverCreateMatch()
    {
        var candidate = CreateActiveCandidate();
        var match = candidate.RecordSwipe("user-2", SwipeDirection.Left, targetHasLikedMe: true);
        Assert.That(match, Is.Null);
    }

    [Test]
    public void RecordSwipe_ShouldRaiseSwipeRecordedEvent()
    {
        var candidate = CreateActiveCandidate();
        candidate.ClearDomainEvents();
        candidate.RecordSwipe("user-2", SwipeDirection.Right, false);
        
        Assert.That(candidate.DomainEvents.Any(e => e is SwipeRecorded), Is.True);
        var evt = (SwipeRecorded)candidate.DomainEvents.First(e => e is SwipeRecorded);
        Assert.That(evt.UserId, Is.EqualTo("user-1"));
        Assert.That(evt.TargetUserId, Is.EqualTo("user-2"));
    }

    [Test]
    public void RecordSwipe_OnSelf_ShouldThrow()
    {
        var candidate = CreateActiveCandidate();
        Assert.Throws<InvalidOperationException>(() => candidate.RecordSwipe("user-1", SwipeDirection.Right, false));
    }

    [Test]
    public void RecordSwipe_DuplicateTarget_ShouldThrow()
    {
        var candidate = CreateActiveCandidate();
        candidate.RecordSwipe("user-2", SwipeDirection.Left, false);
        Assert.Throws<InvalidOperationException>(() => candidate.RecordSwipe("user-2", SwipeDirection.Right, false));
    }

    [Test]
    public void HasLiked_ShouldReturnTrueForRightSwipe()
    {
        var candidate = CreateActiveCandidate();
        candidate.RecordSwipe("user-2", SwipeDirection.Right, false);
        Assert.That(candidate.HasLiked("user-2"), Is.True);
    }

    [Test]
    public void HasLiked_ShouldReturnFalseForLeftSwipe()
    {
        var candidate = CreateActiveCandidate();
        candidate.RecordSwipe("user-2", SwipeDirection.Left, false);
        Assert.That(candidate.HasLiked("user-2"), Is.False);
    }
}
