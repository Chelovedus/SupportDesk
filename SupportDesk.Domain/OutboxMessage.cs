namespace SupportDesk.Domain;

public class OutboxMessage
{
    public OutboxMessage(DateTimeOffset createdAt, Guid id, string type, string payloadJson)
    {
        if (id == Guid.Empty)
            throw new DomainException("Id can not be empty.");
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("Type can not be empty.");
        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new DomainException("Payload can not be empty.");
        
        CreatedAt = createdAt;
        Id = id;
        Type = type;
        PayloadJson = payloadJson;
        RetryCount = 0;
        Status = OutboxMessageStatus.Pending;
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string PayloadJson { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public int RetryCount;
    public DateTimeOffset CreatedAt;
    public DateTimeOffset? ProcessedAt;
    public string? LastError;
}