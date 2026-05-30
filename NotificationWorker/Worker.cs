using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SupportDesk.Infrastructure.RabbitMq;

namespace NotificationWorker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqOptions _options;

    public Worker(ILogger<Worker> logger, IOptions<RabbitMqOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification worker running at: {Timestamp}. RabbitMq queue: {QueueName}",
            DateTimeOffset.UtcNow,
            _options.QueueName);

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };
        
        await using var connection = await factory.CreateConnectionAsync(cancellationToken: cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        
        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        
        await channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.RoutingKey,
            cancellationToken: cancellationToken);
        
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var localConsumer = (AsyncEventingBasicConsumer)sender;
            var consumerChannel = localConsumer.Channel;
            
            var body = eventArgs.Body.ToArray();
            var payloadJson = Encoding.UTF8.GetString(body);

            var messageId = eventArgs.BasicProperties.MessageId;
            var eventType = eventArgs.BasicProperties.Type;

            try
            {
                _logger.LogInformation(
                    "Notification sent for event {MessageType}. MessageId: {MessageId}. Payload {PayloadJson}",
                    eventType,
                    messageId,
                    payloadJson);

                await consumerChannel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception: exception,
                    message: "Failed to process message {MessageId}, type: {MessageType}",
                    messageId,
                    eventType);
                
                await consumerChannel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: cancellationToken);
            }
        };
        
        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
        
        _logger.LogInformation(
            "Notification worker started. Queue: {QueueName}. RoutingKey: {RoutingKey}",
            _options.QueueName,
            _options.RoutingKey);

        try
        {
            await Task.Delay(
                delay: Timeout.InfiniteTimeSpan,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Notification worker is stopping.");
        }
    }
}