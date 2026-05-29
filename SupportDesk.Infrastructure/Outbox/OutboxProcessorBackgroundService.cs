using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using SupportDesk.Domain;

namespace SupportDesk.Infrastructure.Outbox;

public sealed class OutboxProcessorBackgroundService : BackgroundService
{
    public OutboxProcessorBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IOutboxMessagePublisher messagePublisher,
        ILogger<OutboxProcessorBackgroundService> logger,
        IOptions<OutboxProcessorOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _publisher = messagePublisher;
        _logger = logger;
        _options = options.Value;
    }

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOutboxMessagePublisher _publisher;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;
    private readonly OutboxProcessorOptions _options;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_options.Enabled is false)
        {
            _logger.LogInformation("Outbox processor is disabled.");
            return;
        }

        try
        {
            while (cancellationToken.IsCancellationRequested is false)
            {
                await ProcessPendingMessagesAsync(cancellationToken);
            
                await Task.Delay(
                    delay: TimeSpan.FromSeconds(_options.IntervalSeconds),
                    cancellationToken: cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Outbox processor is stopping.");
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.Status == OutboxMessageStatus.Pending)
            .OrderBy(message => message.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken: cancellationToken);

        await ProcessMessagesAsync(messages, dbContext, cancellationToken);
    }

    private async Task ProcessMessagesAsync(List<OutboxMessage> messages, SupportDeskDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            try
            {
                message.MarkAsProcessing();

                await _publisher.PublishAsync(message: message, cancellationToken: cancellationToken);
                
                _logger.LogInformation(
                    "Published outbox message {MessageId} of type {MessageType}.",
                    message.Id,
                    message.Type);
                
                message.MarkAsProcessed(DateTimeOffset.UtcNow);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.MarkAsFailed(error: exception.Message, maxRetryCount: _options.MaxRetryCount);
                
                _logger.LogError(
                    exception: exception,
                    message: "Failed to process outbox message {MessageId} of type {MessageType}",
                    message.Id,
                    message.Type);
            }
            
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}