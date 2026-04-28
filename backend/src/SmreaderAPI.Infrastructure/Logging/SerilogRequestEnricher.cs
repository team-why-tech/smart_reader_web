using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace SmreaderAPI.Infrastructure.Logging;

public class SerilogRequestEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SerilogRequestEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientIp", clientIp));
    }
}
