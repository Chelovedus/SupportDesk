namespace SupportDesk.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    
    private const string LoggingScopeKey = "CorrelationId";
    private const int MaxCorrelationIdLength = 128;
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context.Request.Headers);
        
        context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
           [LoggingScopeKey] = correlationId,
        }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(IHeaderDictionary headers)
    {
        var correlationId = headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > MaxCorrelationIdLength)
        {
            correlationId = CreateCorrelationId();
        }
        
        return correlationId;
    }

    private static string CreateCorrelationId()
    {
        return Guid.CreateVersion7().ToString("N");
    }
}