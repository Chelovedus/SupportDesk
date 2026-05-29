namespace SupportDesk.Infrastructure.RabbitMq;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "supportdesk.events";
    public string QueueName { get; set; } = "supportdesk.notifications";
    public string RoutingKey { get; set; } = "supportdesk.ticket-events";
}