using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Handlers;

/// <summary>Tenant identity, ownership, and per-tenant singleton creation shared by the surgery object stores.</summary>
internal static class SurgeryTenantObjects
{
    public static (string TenantId, Guid OwnerId) RequireTenant(IRequestContext requestContext)
    {
        string? tenantId = requestContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId) || !Guid.TryParse(tenantId, out Guid ownerId))
        {
            throw new InvalidOperationException("Tenant context is required.");
        }

        return (tenantId, ownerId);
    }

    public static void EnsureCanPlan(ISurgeryPlanningAuthorization authorization)
    {
        if (!authorization.CanPlan()) throw new SurgeryPlanningForbiddenException();
    }

    /// <summary>
    /// The deterministic ID of a tenant's default object of a type. These IDs are already stored,
    /// so the derivation must never change.
    /// </summary>
    public static Guid DefaultObjectId(string objectType, string tenantId)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(objectType + ".default:" + tenantId));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }

    public static bool IsOwned(ProjectObjectReadDto value, Guid ownerId, string objectType)
        => string.Equals(value.OwnerEntityType, SurgeryObjectTypes.CatalogOwnerEntityType, StringComparison.Ordinal)
            && value.OwnerEntityId == ownerId
            && string.Equals(value.ObjectType, objectType, StringComparison.Ordinal);

    /// <summary>Creates a tenant's singleton object, or returns the stored one when a concurrent call created it first.</summary>
    public static async Task<T> CreateSingletonAsync<T>(Func<Task<T>> create, Func<Task<T?>> readExisting, string failureMessage)
        where T : class
    {
        try
        {
            return await create();
        }
        catch (DbUpdateException)
        {
            // A concurrent call created it first with the same deterministic id; return that stored value.
            return await readExisting() ?? throw new InvalidOperationException(failureMessage);
        }
    }
}
