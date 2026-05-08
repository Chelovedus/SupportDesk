using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using SupportDesk.Application.Tickets;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Auth;
using SupportDesk.Application.Users;
using SupportDesk.Infrastructure;
using SupportDesk.Infrastructure.Auth;
using SupportDesk.Infrastructure.Seed;
using SupportDesk.Infrastructure.Tickets;
using SupportDesk.Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<ITicketService, EfTicketService>();
builder.Services.AddScoped<IUserReadRepository, EfUserReadRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHashService, AspNetCorePasswordHashService>();


builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title =  "SupportDesk API",
        Version = "v1",
        Description = "Backend API for internal support ticket management"
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<SupportDeskDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.AddSeedUsersAsync();
}

app.MapControllers();

app.Run();