using System.Text.Json;
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
    public async Task<IReadOnlyList<SurgeryCatalog>> ListAsync(CancellationToken cancellationToken)
    {
        (string tenantId, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
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
        (string tenantId, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
        if (string.Equals(catalogId, SurgeryCatalogDefaults.LegacyGeneralSurgeryId, StringComparison.Ordinal))
        {
            return await ReadAsync(SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.CatalogObjectType, tenantId), ownerId, cancellationToken);
        }

        return Guid.TryParse(catalogId, out Guid id)
            ? await ReadAsync(id, ownerId, cancellationToken)
            : null;
    }

    public async Task<SurgeryCatalog> CreateAsync(SurgeryCatalogWriteModel catalog, CancellationToken cancellationToken)
    {
        SurgeryTenantObjects.EnsureCanPlan(authorization);
        SurgeryPlanRules.ValidateCatalog(catalog);
        (_, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
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
        SurgeryTenantObjects.EnsureCanPlan(authorization);
        SurgeryPlanRules.ValidateCatalog(catalog);
        (string tenantId, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
        ProjectObjectReadDto? current = await projectObjects.GetAsync(id, cancellationToken);
        if (current is null || current.DeletedAt is not null || !SurgeryTenantObjects.IsOwned(current, ownerId, SurgeryObjectTypes.CatalogObjectType)) return null;
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
        SurgeryTenantObjects.EnsureCanPlan(authorization);
        (string tenantId, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
        if (id == SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.CatalogObjectType, tenantId)) throw new InvalidOperationException("The default surgery type cannot be deleted.");
        ProjectObjectReadDto? current = await projectObjects.GetAsync(id, cancellationToken);
        if (current is null || current.DeletedAt is not null || !SurgeryTenantObjects.IsOwned(current, ownerId, SurgeryObjectTypes.CatalogObjectType)) return false;
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
        SurgeryTenantObjects.EnsureCanPlan(authorization);
        SurgeryPlanRules.ValidateCatalog(defaults);
        (string tenantId, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
        Guid defaultId = SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.CatalogObjectType, tenantId);
        if (await ReadAsync(defaultId, ownerId, cancellationToken) is { } existing) return existing;
        SurgeryCatalogData data = new(
            defaults.Name.Trim(),
            Normalize(defaults.Tools),
            Normalize(defaults.DefaultTeamRoles),
            defaults.ReminderPolicies,
            true);
        return await SurgeryTenantObjects.CreateSingletonAsync(
            async () =>
            {
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
            },
            () => ReadAsync(defaultId, ownerId, cancellationToken),
            "The default surgery type could not be created.");
    }

    private async Task<SurgeryCatalog?> ReadAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
    {
        ProjectObjectReadDto? value = await projectObjects.GetAsync(id, cancellationToken);
        return value is null || value.DeletedAt is not null || !SurgeryTenantObjects.IsOwned(value, ownerId, SurgeryObjectTypes.CatalogObjectType) ? null : ToCatalog(value);
    }




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

}
