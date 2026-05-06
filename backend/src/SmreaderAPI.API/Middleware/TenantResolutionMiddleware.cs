using System.Text.Json;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Caching;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.API.Middleware;

/// <summary>
/// Extracts tenant ID from JWT claims (authenticated) or request body/headers (login),
/// resolves the tenant connection string, caches it, and populates ITenantContext.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        TenantConnectionStringCache cache,
        ITenantRepository tenantRepo,
        TenantConnectionStringBuilder connBuilder)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

        // Skip tenant resolution for endpoints that don't need it
        if (ShouldSkipTenantResolution(path))
        {
            await _next(context);
            return;
        }

        // For login endpoint: tenant ID comes from request body — handled by controller
        // For authenticated endpoints: tenant ID comes from JWT claim
        if (path.Contains("/auth/login"))
        {
            // Login flow: the controller will resolve tenant manually
            // Pass through without tenant context (controller handles it)
            await _next(context);
            return;
        }

        // For all other endpoints: extract tenant_id from JWT claims
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        if (tenantIdClaim is null || !int.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogWarning("Missing or invalid tenant_id claim in JWT");
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { success = false, message = "Tenant identification required." }));
            return;
        }

        // Resolve connection string (cached or from Master DB)
        var connStr = cache.Get(tenantId);
        if (connStr is null)
        {
            var tenant = await tenantRepo.GetLatestByIdAsync(tenantId);
            if (tenant is null)
            {
                _logger.LogWarning("Tenant {TenantId} not found in Master DB", tenantId);
                context.Response.StatusCode = 404;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { success = false, message = "Tenant not found." }));
                return;
            }

            connStr = connBuilder.Build(tenant);
            cache.Set(tenantId, connStr);
        }

        tenantContext.Set(tenantId, connStr);
        _logger.LogDebug("Tenant resolved: {TenantId}", tenantId);

        await _next(context);
    }

    private static bool ShouldSkipTenantResolution(string path)
    {
        return path.Contains("/health") ||
               path.Contains("/swagger") ||
               path == "/";
    }
}
