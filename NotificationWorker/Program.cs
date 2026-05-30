using NotificationWorker;
using SupportDesk.Infrastructure.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();