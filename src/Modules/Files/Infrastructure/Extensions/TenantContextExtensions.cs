using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace Vitarish.Files.Infrastructure.Extensions;

internal static class TenantContextExtensions
{
    public static IDisposable UseTenant(this IMultiTenantContextAccessor<TenantInfo> accessor, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        if (accessor is not IMultiTenantContextSetter setter)
        {
            throw new InvalidOperationException("The multi-tenant context accessor does not support mutation.");
        }

        IMultiTenantContext<TenantInfo> previous = accessor.MultiTenantContext;

        MultiTenantContext<TenantInfo> next = new()
        {
            TenantInfo = string.IsNullOrWhiteSpace(tenantId)
                ? null
                : new TenantInfo
                {
                    Id = tenantId,
                    Identifier = tenantId
                }
        };

        setter.MultiTenantContext = next;

        return new TenantScope(setter, previous);
    }

    private sealed class TenantScope(IMultiTenantContextSetter setter, IMultiTenantContext<TenantInfo> previous) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            setter.MultiTenantContext = previous;
            disposed = true;
        }
    }
}
