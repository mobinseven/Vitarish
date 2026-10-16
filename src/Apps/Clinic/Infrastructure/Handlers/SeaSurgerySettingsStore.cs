using System.Text.Json;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Serializers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class SeaSurgerySettingsStore(
    IProjectObjectService projectObjects,
    ISurgeryPlanningAuthorization authorization,
    IRequestContext requestContext) : ISurgerySettingsStore
{
    public async Task<SurgerySettings?> GetAsync(CancellationToken cancellationToken)
    {
        (string tenantId, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
        return await ReadAsync(SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.SettingsObjectType, tenantId), ownerId, cancellationToken);
    }

    public async Task<SurgerySettings> EnsureDefaultAsync(SurgerySettingsWriteModel defaults, CancellationToken cancellationToken)
    {
        SurgeryTenantObjects.EnsureCanPlan(authorization);
        SurgeryPlanRules.ValidateSettings(defaults);
        (string tenantId, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
        Guid settingsId = SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.SettingsObjectType, tenantId);
        if (await ReadAsync(settingsId, ownerId, cancellationToken) is { } existing) return existing;
        SurgerySettingsData data = new(defaults.CancellationSubject.Trim(), defaults.CancellationTextBody.Trim());
        return await SurgeryTenantObjects.CreateSingletonAsync(
            async () =>
            {
                try
                {
                    ProjectObjectReadDto value = await projectObjects.CreateAsync(
                        SurgeryObjectTypes.CatalogOwnerEntityType,
                        ownerId,
                        SurgeryObjectTypes.SettingsObjectType,
                        Serialize(data),
                        cancellationToken,
                        settingsId);
                    return ToSettings(value);
                }
                catch (ProjectObjectRefreshUnavailableException exception)
                {
                    // Settings have no materialized view, so this only means the refresh could not be queued after the write succeeded.
                    return await ReadAsync(exception.ObjectId, ownerId, cancellationToken)
                        ?? throw new InvalidOperationException("The stored surgery settings could not be read.");
                }
            },
            () => ReadAsync(settingsId, ownerId, cancellationToken),
            "The surgery settings could not be created.");
    }

    public async Task<SurgerySettings?> UpdateAsync(SurgerySettingsWriteModel model, Guid concurrencyToken, CancellationToken cancellationToken)
    {
        SurgeryTenantObjects.EnsureCanPlan(authorization);
        SurgeryPlanRules.ValidateSettings(model);
        (string tenantId, Guid ownerId) = SurgeryTenantObjects.RequireTenant(requestContext);
        Guid settingsId = SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.SettingsObjectType, tenantId);
        ProjectObjectReadDto? current = await projectObjects.GetAsync(settingsId, cancellationToken);
        if (current is null || current.DeletedAt is not null || !SurgeryTenantObjects.IsOwned(current, ownerId, SurgeryObjectTypes.SettingsObjectType)) return null;
        SurgerySettingsData data = new(model.CancellationSubject.Trim(), model.CancellationTextBody.Trim());
        try
        {
            ProjectObjectReadDto? value = await projectObjects.UpdateAsync(
                settingsId,
                SurgeryObjectTypes.CatalogOwnerEntityType,
                ownerId,
                SurgeryObjectTypes.SettingsObjectType,
                Serialize(data),
                concurrencyToken,
                cancellationToken);
            return value is null ? null : ToSettings(value);
        }
        catch (ProjectObjectRefreshUnavailableException exception)
        {
            // Settings have no materialized view, so this only means the refresh could not be queued after the write succeeded.
            return await ReadAsync(exception.ObjectId, ownerId, cancellationToken);
        }
    }


    private async Task<SurgerySettings?> ReadAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
    {
        ProjectObjectReadDto? value = await projectObjects.GetAsync(id, cancellationToken);
        return value is null || value.DeletedAt is not null || !SurgeryTenantObjects.IsOwned(value, ownerId, SurgeryObjectTypes.SettingsObjectType) ? null : ToSettings(value);
    }



    private static JsonElement Serialize(SurgerySettingsData data)
        => JsonSerializer.SerializeToElement(data, SurgerySettingsJsonSerializerContext.Default.SurgerySettingsData);

    private static SurgerySettingsData Deserialize(JsonElement data)
        => JsonSerializer.Deserialize(data, SurgerySettingsJsonSerializerContext.Default.SurgerySettingsData)
            ?? throw new InvalidOperationException("The stored surgery settings object is invalid.");

    private static SurgerySettings ToSettings(ProjectObjectReadDto value)
    {
        SurgerySettingsData data = Deserialize(value.Data);
        return new SurgerySettings(data.CancellationSubject, data.CancellationTextBody, value.ConcurrencyToken);
    }

}
