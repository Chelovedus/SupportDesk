using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SupportDesk.Domain;
using SupportDesk.Infrastructure.Outbox;

namespace SupportDesk.Infrastructure.RabbitMq;

public sealed class RabbitMqOutboxPublisher : IOutboxMessagePublisher
{
    private readonly RabbitMqOptions _options;
    public RabbitMqOutboxPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    // WIP need refactor. Reusable connections
    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
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

        var body = Encoding.UTF8.GetBytes(message.PayloadJson);

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = message.Id.ToString(),
            Type = message.Type,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: _options.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

    }
}