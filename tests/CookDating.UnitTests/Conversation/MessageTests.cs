using CookDating.Conversation.Domain;

namespace CookDating.UnitTests.Conversation;

[TestFixture]
public class MessageTests
{
    [Test]
    public void Create_ShouldSetProperties()
    {
        var msg = Message.Create("alice", "Hello");
        Assert.That(msg.SenderId, Is.EqualTo("alice"));
        Assert.That(msg.Content, Is.EqualTo("Hello"));
        Assert.That(msg.IsRead, Is.False);
    }

    [Test]
    public void MarkAsRead_ShouldSetIsReadTrue()
    {
        var msg = Message.Create("alice", "Hello");
        msg.MarkAsRead();
        Assert.That(msg.IsRead, Is.True);
    }

    [Test]
    public void Create_WithEmptySender_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Message.Create("", "Hello"));
    }

    [Test]
    public void Create_WithEmptyContent_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Message.Create("alice", ""));
    }
}
