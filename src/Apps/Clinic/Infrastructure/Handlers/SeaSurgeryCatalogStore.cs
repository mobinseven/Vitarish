using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Serializers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class SeaSurgeryCatalogStore(
    IProjectObjectService projectObjects,
    ISurgeryPlanningAuthorization authorization,
    IRequestContext requestContext) : ISurgeryCatalogStore
{
    private const string DefaultCatalogIdPrefix = SurgeryObjectTypes.CatalogObjectType + ".default:";

    public async Task<IReadOnlyList<SurgeryCatalog>> ListAsync(CancellationToken cancellationToken)
    {
        string tenantId = TenantContext();
        Guid ownerId = Guid.Parse(tenantId);
        return (await projectObjects.GetByOwnerAsync(
                SurgeryObjectTypes.CatalogOwnerEntityType,
                ownerId,
                SurgeryObjectTypes.CatalogObjectType,
                cancellationToken))
            .Where(value => value.DeletedAt is null)
            .Select(value => (Value: value, Data: Deserialize(value.Data)))
            .OrderByDescending(item => item.Data.IsDefault)
            .ThenBy(item => item.Data.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => ToCatalog(item.Value, item.Data))
            .ToArray();
    }

    public async Task<SurgeryCatalog?> FindAsync(string catalogId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(catalogId)) return null;
        string tenantId = TenantContext();
        Guid ownerId = Guid.Parse(tenantId);
        if (string.Equals(catalogId, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, StringComparison.Ordinal))
        {
            return await ReadAsync(DefaultCatalogId(tenantId), ownerId, cancellationToken);
        }

        return Guid.TryParse(catalogId, out Guid id)
            ? await ReadAsync(id, ownerId, cancellationToken)
            : null;
    }

    public async Task<SurgeryCatalog> CreateAsync(SurgeryCatalogWriteModel catalog, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        SurgeryPlanRules.ValidateCatalog(catalog);
        Guid ownerId = Guid.Parse(TenantContext());
        SurgeryCatalogData data = new(
            catalog.Name.Trim(),
            Normalize(catalog.Tools),
            Normalize(catalog.DefaultTeamRoles),
            catalog.ReminderPolicies,
            false);
        try
        {
            ProjectObjectReadDto value = await projectObjects.CreateAsync(
                SurgeryObjectTypes.CatalogOwnerEntityType,
                ownerId,
                SurgeryObjectTypes.CatalogObjectType,
                Serialize(data),
                cancellationToken);
            return ToCatalog(value);
        }
        catch (ProjectObjectRefreshUnavailableException exception)
        {
            // A surgery type has no materialized view, so this only means the refresh could not be queued after the write succeeded.
            return await ReadAsync(exception.ObjectId, ownerId, cancellationToken)
                ?? throw new InvalidOperationException("The stored surgery type object could not be read.");
        }
    }

    public async Task<SurgeryCatalog?> UpdateAsync(
        Guid id,
        SurgeryCatalogWriteModel catalog,
        Guid concurrencyToken,
        CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        SurgeryPlanRules.ValidateCatalog(catalog);
        string tenantId = TenantContext();
        Guid ownerId = Guid.Parse(tenantId);
        ProjectObjectReadDto? current = await projectObjects.GetAsync(id, cancellationToken);
        if (current is null || current.DeletedAt is not null || !IsOwned(current, ownerId)) return null;
        SurgeryCatalogData data = new(
            catalog.Name.Trim(),
            Normalize(catalog.Tools),
            Normalize(catalog.DefaultTeamRoles),
            catalog.ReminderPolicies,
            Deserialize(current.Data).IsDefault);
        try
        {
            ProjectObjectReadDto? value = await projectObjects.UpdateAsync(
                id,
                SurgeryObjectTypes.CatalogOwnerEntityType,
                ownerId,
                SurgeryObjectTypes.CatalogObjectType,
                Serialize(data),
                concurrencyToken,
                cancellationToken);
            return value is null ? null : ToCatalog(value);
        }
        catch (ProjectObjectRefreshUnavailableException exception)
        {
            // A surgery type has no materialized view, so this only means the refresh could not be queued after the write succeeded.
            return await ReadAsync(exception.ObjectId, ownerId, cancellationToken);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        string tenantId = TenantContext();
        Guid ownerId = Guid.Parse(tenantId);
        if (id == DefaultCatalogId(tenantId)) throw new InvalidOperationException("The default surgery type cannot be deleted.");
        ProjectObjectReadDto? current = await projectObjects.GetAsync(id, cancellationToken);
        if (current is null || current.DeletedAt is not null || !IsOwned(current, ownerId)) return false;
        try
        {
            return await projectObjects.DeleteAsync(id, concurrencyToken, cancellationToken);
        }
        catch (ProjectObjectRefreshUnavailableException)
        {
            // A surgery type has no materialized view, so this only means the refresh could not be queued after the delete succeeded.
            return true;
        }
    }

    public async Task<SurgeryCatalog> EnsureDefaultAsync(SurgeryCatalogWriteModel defaults, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        SurgeryPlanRules.ValidateCatalog(defaults);
        string tenantId = TenantContext();
        Guid ownerId = Guid.Parse(tenantId);
        Guid defaultId = DefaultCatalogId(tenantId);
        if (await ReadAsync(defaultId, ownerId, cancellationToken) is { } existing) return existing;
        SurgeryCatalogData data = new(
            defaults.Name.Trim(),
            Normalize(defaults.Tools),
            Normalize(defaults.DefaultTeamRoles),
            defaults.ReminderPolicies,
            true);
        try
        {
            ProjectObjectReadDto value = await projectObjects.CreateAsync(
                SurgeryObjectTypes.CatalogOwnerEntityType,
                ownerId,
                SurgeryObjectTypes.CatalogObjectType,
                Serialize(data),
                cancellationToken,
                defaultId);
            return ToCatalog(value);
        }
        catch (ProjectObjectRefreshUnavailableException exception)
        {
            // A surgery type has no materialized view, so this only means the refresh could not be queued after the write succeeded.
            return await ReadAsync(exception.ObjectId, ownerId, cancellationToken)
                ?? throw new InvalidOperationException("The stored default surgery type could not be read.");
        }
        catch (DbUpdateException)
        {
            // A concurrent call created the default first with the same deterministic id; return that stored value.
            return await ReadAsync(defaultId, ownerId, cancellationToken)
                ?? throw new InvalidOperationException("The default surgery type could not be created.");
        }
    }

    private async Task<SurgeryCatalog?> ReadAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
    {
        ProjectObjectReadDto? value = await projectObjects.GetAsync(id, cancellationToken);
        return value is null || value.DeletedAt is not null || !IsOwned(value, ownerId) ? null : ToCatalog(value);
    }

    private void EnsureCanPlan()
    {
        if (!authorization.CanPlan()) throw new SurgeryPlanningForbiddenException();
    }

    private string TenantContext()
    {
        string? tenantId = requestContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId) || !Guid.TryParse(tenantId, out _))
        {
            throw new InvalidOperationException("Tenant context is required.");
        }

        return tenantId;
    }

    private static bool IsOwned(ProjectObjectReadDto value, Guid ownerId)
        => string.Equals(value.OwnerEntityType, SurgeryObjectTypes.CatalogOwnerEntityType, StringComparison.Ordinal)
            && value.OwnerEntityId == ownerId
            && string.Equals(value.ObjectType, SurgeryObjectTypes.CatalogObjectType, StringComparison.Ordinal);

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string> values)
        => values.Select(value => value.Trim()).Where(value => value.Length > 0).ToArray();

    private static JsonElement Serialize(SurgeryCatalogData data)
        => JsonSerializer.SerializeToElement(data, SurgeryCatalogJsonSerializerContext.Default.SurgeryCatalogData);

    private static SurgeryCatalogData Deserialize(JsonElement data)
        => JsonSerializer.Deserialize(data, SurgeryCatalogJsonSerializerContext.Default.SurgeryCatalogData)
            ?? throw new InvalidOperationException("The stored surgery type object is invalid.");

    private static SurgeryCatalog ToCatalog(ProjectObjectReadDto value)
        => ToCatalog(value, Deserialize(value.Data));

    private static SurgeryCatalog ToCatalog(ProjectObjectReadDto value, SurgeryCatalogData data)
        => new(value.Id.ToString("D"), data.Name, data.Tools, data.DefaultTeamRoles, data.ReminderPolicies, value.ConcurrencyToken);

    private static Guid DefaultCatalogId(string tenantId)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(DefaultCatalogIdPrefix + tenantId));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }
}
