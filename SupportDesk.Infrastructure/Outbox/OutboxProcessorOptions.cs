namespace SupportDesk.Infrastructure.Outbox;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";
    public bool Enabled { get; set; } = false;
    public int IntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 20;
    public int MaxRetryCount { get; set; } = 3;
}