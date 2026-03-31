using CookDating.Conversation.Domain;
using CookDating.Conversation.Domain.Events;
using Domain = CookDating.Conversation.Domain;

namespace CookDating.UnitTests.Conversation;

[TestFixture]
public class ConversationTests
{
    [Test]
    public void StartForMatch_ShouldCreateConversation()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        Assert.That(conv.MatchId, Is.EqualTo("match-1"));
        Assert.That(conv.Participant1Id, Is.EqualTo("alice"));
        Assert.That(conv.Participant2Id, Is.EqualTo("bob"));
        Assert.That(conv.Messages, Is.Empty);
    }

    [Test]
    public void StartForMatch_ShouldRaiseConversationStartedEvent()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        Assert.That(conv.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(conv.DomainEvents.First(), Is.TypeOf<ConversationStarted>());
    }

    [Test]
    public void StartForMatch_WithSameUser_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Domain.Conversation.StartForMatch("match-1", "alice", "alice"));
    }

    [Test]
    public void SendMessage_ByParticipant_ShouldSucceed()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        conv.ClearDomainEvents();

        var message = conv.SendMessage("alice", "Hello Bob!");

        Assert.That(conv.Messages, Has.Count.EqualTo(1));
        Assert.That(message.Content, Is.EqualTo("Hello Bob!"));
        Assert.That(message.SenderId, Is.EqualTo("alice"));
    }

    [Test]
    public void SendMessage_ShouldRaiseMessageSentEvent()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        conv.ClearDomainEvents();

        conv.SendMessage("alice", "Hello!");

        Assert.That(conv.DomainEvents, Has.Count.EqualTo(1));
        var evt = (MessageSent)conv.DomainEvents.First();
        Assert.That(evt.SenderId, Is.EqualTo("alice"));
        Assert.That(evt.RecipientId, Is.EqualTo("bob"));
    }

    [Test]
    public void SendMessage_ByNonParticipant_ShouldThrow()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            conv.SendMessage("charlie", "Hey!"));
        Assert.That(ex.Message, Does.Contain("not a participant"));
    }

    [Test]
    public void SendMessage_WithEmptyContent_ShouldThrow()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        Assert.Throws<ArgumentException>(() => conv.SendMessage("alice", ""));
    }

    [Test]
    public void SendMessage_ExceedingMaxLength_ShouldThrow()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        var longMessage = new string('a', 2001);
        Assert.Throws<ArgumentException>(() => conv.SendMessage("alice", longMessage));
    }

    [Test]
    public void MarkMessagesAsRead_ShouldMarkOtherUsersMessages()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        conv.SendMessage("alice", "Hello!");
        conv.SendMessage("alice", "How are you?");

        conv.MarkMessagesAsRead("bob");

        Assert.That(conv.Messages.All(m => m.IsRead), Is.True);
    }

    [Test]
    public void MarkMessagesAsRead_ShouldNotMarkOwnMessages()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        conv.SendMessage("alice", "Hello!");

        conv.MarkMessagesAsRead("alice");

        Assert.That(conv.Messages.All(m => !m.IsRead), Is.True);
    }

    [Test]
    public void UnreadCountFor_ShouldCountCorrectly()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        conv.SendMessage("alice", "Hi 1");
        conv.SendMessage("alice", "Hi 2");
        conv.SendMessage("bob", "Hey");

        Assert.That(conv.UnreadCountFor("bob"), Is.EqualTo(2));
        Assert.That(conv.UnreadCountFor("alice"), Is.EqualTo(1));
    }

    [Test]
    public void IsParticipant_ShouldReturnCorrectly()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        Assert.That(conv.IsParticipant("alice"), Is.True);
        Assert.That(conv.IsParticipant("charlie"), Is.False);
    }

    [Test]
    public void GetOtherParticipant_ShouldReturnPartner()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        Assert.That(conv.GetOtherParticipant("alice"), Is.EqualTo("bob"));
        Assert.That(conv.GetOtherParticipant("bob"), Is.EqualTo("alice"));
    }

    [Test]
    public void LastMessageAt_ShouldUpdateOnNewMessage()
    {
        var conv = Domain.Conversation.StartForMatch("match-1", "alice", "bob");
        Assert.That(conv.LastMessageAt, Is.Null);

        conv.SendMessage("alice", "Hello!");
        Assert.That(conv.LastMessageAt, Is.Not.Null);
    }
}
