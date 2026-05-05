using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Tenancy;

namespace SmreaderAPI.API.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, ITenantConnectionResolver resolver)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdValue = context.User.FindFirstValue(TenantClaimTypes.TenantId);
            var financialYear = context.User.FindFirstValue(TenantClaimTypes.FinancialYear);

            if (int.TryParse(tenantIdValue, out var tenantId) && !string.IsNullOrWhiteSpace(financialYear))
            {
                var connectionString = await resolver.ResolveConnectionStringAsync(tenantId, financialYear);
                tenantContext.Set(tenantId, financialYear, connectionString);
            }
        }

        await _next(context);
    }
}
