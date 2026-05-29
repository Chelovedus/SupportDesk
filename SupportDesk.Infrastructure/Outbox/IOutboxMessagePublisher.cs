using SupportDesk.Domain;

namespace SupportDesk.Infrastructure.Outbox;

public interface IOutboxMessagePublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}