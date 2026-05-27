using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using SupportDesk.Application.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Auth;
using SupportDesk.Application.Users;
using SupportDesk.Infrastructure;
using SupportDesk.Infrastructure.Auth;
using SupportDesk.Infrastructure.Outbox;
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SupportDeskDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ITicketService, EfTicketService>();
builder.Services.AddScoped<IUserReadRepository, EfUserReadRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHashService, AspNetCorePasswordHashService>();

builder.Services.Configure<OutboxProcessorOptions>(
    builder.Configuration.GetSection(OutboxProcessorOptions.SectionName));
builder.Services.AddHostedService<OutboxProcessorBackgroundService>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtOptions = builder.Configuration
                     .GetSection("Jwt")
                     .Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Jwt options are not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SupportDesk API",
        Version = "v1",
        Description = "Backend API for internal support ticket management"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste JWT token here. Example: eyJ..."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
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

var migrateOnStartup = app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
var seedOnStartup = app.Configuration.GetValue<bool>("Database:SeedOnStartup");

if (migrateOnStartup || seedOnStartup)
{
    using var scope = app.Services.CreateScope();

    if (migrateOnStartup)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    if (seedOnStartup)
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.AddSeedUsersAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();