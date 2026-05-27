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
    public int RetryCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? LastError { get; private set; }

    public void MarkAsProcessing()
    {
        if (Status != OutboxMessageStatus.Pending)
            throw new DomainException("Only pending outbox messages can be marked as processing");
        
        Status = OutboxMessageStatus.Processing;
    }

    public void MarkAsProcessed(DateTimeOffset processedAt)
    {
        if (Status != OutboxMessageStatus.Processing)
            throw new DomainException("Only processing outbox messages can be processed");
        
        Status = OutboxMessageStatus.Processed;
        ProcessedAt = processedAt;
        LastError = null;
    }

    public void MarkAsFailed(string error, int maxRetryCount)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new DomainException("Error can not be empty.");
        if (maxRetryCount < 1)
            throw new DomainException("Max retry count must be greater than zero.");
        
        RetryCount++;
        Status = RetryCount < maxRetryCount ? OutboxMessageStatus.Pending : OutboxMessageStatus.Failed;
        LastError = error;
    }
}