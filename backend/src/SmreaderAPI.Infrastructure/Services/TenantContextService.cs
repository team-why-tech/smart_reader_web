using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Infrastructure.Services;

public class TenantContextService : ITenantContext
{
    private int? _tenantId;

    public int? TenantId => _tenantId;

    public void SetTenantId(int tenantId)
    {
        _tenantId = tenantId;
    }
}
