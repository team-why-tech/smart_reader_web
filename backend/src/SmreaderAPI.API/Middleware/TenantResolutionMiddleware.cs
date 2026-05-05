using System.Security.Claims;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.API.Middleware;

/// <summary>
/// Middleware that resolves tenant context from JWT claims after authentication
/// Runs after authentication middleware, before authorization
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    // Endpoints that should skip tenant resolution (unauthenticated endpoints)
    private static readonly HashSet<string> _skipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh-token",
        "/health",
        "/swagger",
        "/swagger/v1/swagger.json"
    };

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantConnectionResolver connectionResolver)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip tenant resolution for unauthenticated endpoints
        if (_skipPaths.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Extract tenant_id and fy from JWT claims
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        var fyClaim = context.User.FindFirst("fy")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(fyClaim))
        {
            _logger.LogWarning("JWT missing tenant_id or fy claims for path: {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid token: missing tenant information",
                data = (object?)null,
                errors = (string[]?)null
            });
            return;
        }

        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogWarning("Invalid tenant_id claim value: {TenantIdClaim}", tenantIdClaim);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid token: invalid tenant ID",
                data = (object?)null,
                errors = (string[]?)null
            });
            return;
        }

        try
        {
            // Resolve tenant connection string (cache-first, fallback to master DB)
            var connectionString = await connectionResolver.ResolveConnectionStringAsync(tenantId, fyClaim);

            // Populate tenant context for this request
            tenantContext.SetContext(tenantId, fyClaim, connectionString);

            _logger.LogDebug("Tenant context resolved: TenantId={TenantId}, FY={FY}", tenantId, fyClaim);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to resolve tenant connection: TenantId={TenantId}, FY={FY}", tenantId, fyClaim);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = $"Tenant configuration not found: {ex.Message}",
                data = (object?)null,
                errors = (string[]?)null
            });
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resolving tenant context: TenantId={TenantId}, FY={FY}", tenantId, fyClaim);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Internal server error resolving tenant context",
                data = (object?)null,
                errors = (string[]?)null
            });
            return;
        }

        await _next(context);
    }
}
