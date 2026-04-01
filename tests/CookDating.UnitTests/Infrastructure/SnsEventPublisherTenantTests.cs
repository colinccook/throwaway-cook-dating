using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CookDating.SharedKernel.Domain;
using CookDating.SharedKernel.Infrastructure;
using Moq;

namespace CookDating.UnitTests.Infrastructure;

[TestFixture]
public class SnsEventPublisherTenantTests
{
    [Test]
    public async Task PublishAsync_IncludesTenantIdInMessageAttributes()
    {
        var snsMock = new Mock<IAmazonSimpleNotificationService>();
        PublishRequest? capturedRequest = null;
        snsMock.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PublishResponse());

        var tenantContext = new TenantContext { TenantId = "tech-dating" };
        var publisher = new SnsEventPublisher(snsMock.Object, tenantContext);

        await publisher.PublishAsync(new TestEvent(), "arn:aws:sns:us-east-1:000:test-topic");

        Assert.That(capturedRequest, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest!.MessageAttributes.ContainsKey("TenantId"), Is.True);
            Assert.That(capturedRequest.MessageAttributes["TenantId"].StringValue, Is.EqualTo("tech-dating"));
            Assert.That(capturedRequest.MessageAttributes.ContainsKey("EventType"), Is.True);
            Assert.That(capturedRequest.MessageAttributes["EventType"].StringValue, Is.EqualTo("TestEvent"));
        });
    }

    [Test]
    public async Task PublishAsync_Batch_IncludesTenantIdOnEachMessage()
    {
        var snsMock = new Mock<IAmazonSimpleNotificationService>();
        var capturedRequests = new List<PublishRequest>();
        snsMock.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishRequest, CancellationToken>((req, _) => capturedRequests.Add(req))
            .ReturnsAsync(new PublishResponse());

        var tenantContext = new TenantContext { TenantId = "cook-dating" };
        var publisher = new SnsEventPublisher(snsMock.Object, tenantContext);

        var events = new IDomainEvent[] { new TestEvent(), new TestEvent() };
        await publisher.PublishAsync(events, "arn:aws:sns:us-east-1:000:test-topic");

        Assert.That(capturedRequests, Has.Count.EqualTo(2));
        foreach (var req in capturedRequests)
        {
            Assert.That(req.MessageAttributes["TenantId"].StringValue, Is.EqualTo("cook-dating"));
        }
    }

    private sealed record TestEvent : IDomainEvent
    {
        public Guid EventId => Guid.NewGuid();
        public string EventType => "TestEvent";
        public DateTime OccurredAt => DateTime.UtcNow;
    }
}
