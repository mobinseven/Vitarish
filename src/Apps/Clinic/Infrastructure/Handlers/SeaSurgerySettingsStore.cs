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

internal sealed class SeaSurgerySettingsStore(
    IProjectObjectService projectObjects,
    ISurgeryPlanningAuthorization authorization,
    IRequestContext requestContext) : ISurgerySettingsStore
{
    private const string DefaultSettingsIdPrefix = SurgeryObjectTypes.SettingsObjectType + ".default:";

    public async Task<SurgerySettings?> GetAsync(CancellationToken cancellationToken)
    {
        string tenantId = TenantContext();
        Guid ownerId = Guid.Parse(tenantId);
        return await ReadAsync(DefaultSettingsId(tenantId), ownerId, cancellationToken);
    }

    public async Task<SurgerySettings> EnsureDefaultAsync(SurgerySettingsWriteModel defaults, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        SurgeryPlanRules.ValidateSettings(defaults);
        string tenantId = TenantContext();
        Guid ownerId = Guid.Parse(tenantId);
        Guid settingsId = DefaultSettingsId(tenantId);
        if (await ReadAsync(settingsId, ownerId, cancellationToken) is { } existing) return existing;
        SurgerySettingsData data = new(defaults.CancellationSubject.Trim(), defaults.CancellationTextBody.Trim());
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
        catch (DbUpdateException)
        {
            // A concurrent call created the settings first with the same deterministic id; return that stored value.
            return await ReadAsync(settingsId, ownerId, cancellationToken)
                ?? throw new InvalidOperationException("The surgery settings could not be created.");
        }
    }

    public async Task<SurgerySettings?> UpdateAsync(SurgerySettingsWriteModel model, Guid concurrencyToken, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        SurgeryPlanRules.ValidateSettings(model);
        string tenantId = TenantContext();
        Guid ownerId = Guid.Parse(tenantId);
        Guid settingsId = DefaultSettingsId(tenantId);
        ProjectObjectReadDto? current = await projectObjects.GetAsync(settingsId, cancellationToken);
        if (current is null || current.DeletedAt is not null || !IsOwned(current, ownerId)) return null;
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

    private void EnsureCanPlan()
    {
        if (!authorization.CanPlan()) throw new SurgeryPlanningForbiddenException();
    }

    private async Task<SurgerySettings?> ReadAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)
    {
        ProjectObjectReadDto? value = await projectObjects.GetAsync(id, cancellationToken);
        return value is null || value.DeletedAt is not null || !IsOwned(value, ownerId) ? null : ToSettings(value);
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
            && string.Equals(value.ObjectType, SurgeryObjectTypes.SettingsObjectType, StringComparison.Ordinal);

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

    private static Guid DefaultSettingsId(string tenantId)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(DefaultSettingsIdPrefix + tenantId));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }
}
