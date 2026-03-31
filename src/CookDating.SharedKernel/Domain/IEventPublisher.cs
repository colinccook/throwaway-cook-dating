namespace CookDating.SharedKernel.Domain;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, string topicArn, CancellationToken cancellationToken = default);
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, string topicArn, CancellationToken cancellationToken = default);
}
