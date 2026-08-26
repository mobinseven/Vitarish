using System.Text.Json;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Infrastructure.BackgroundJobs;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal sealed class ProjectObjectService(
    SeaDbContext dbContext,
    IProjectObjectTypeRegistry typeRegistry,
    IRequestContext requestContext,
    ProjectObjectRefreshRequestHandler refreshRequestHandler) : IProjectObjectService
{
    public async Task<ProjectObjectReadDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        ProjectObject? value = await dbContext.ProjectObjects.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return value is null ? null : ToRead(value);
    }

    public async Task<IReadOnlyList<ProjectObjectReadDto>> GetByOwnerAsync(
        string ownerEntityType,
        Guid ownerEntityId,
        string? objectType,
        CancellationToken cancellationToken)
    {
        IQueryable<ProjectObject> query = dbContext.ProjectObjects.AsNoTracking()
            .Where(item => item.OwnerEntityType == ownerEntityType && item.OwnerEntityId == ownerEntityId);

        if (!string.IsNullOrWhiteSpace(objectType))
        {
            query = query.Where(item => item.ObjectType == objectType);
        }

        return (await query.OrderBy(item => item.CreatedAt).ToListAsync(cancellationToken))
            .Select(ToRead)
            .ToArray();
    }

    public async Task<ProjectObjectReadDto> CreateAsync(
        string ownerEntityType,
        Guid ownerEntityId,
        string objectType,
        JsonElement data,
        CancellationToken cancellationToken,
        Guid? id = null)
    {
        ProjectObject value = new()
        {
            Id = id ?? Guid.CreateVersion7(),
            ConcurrencyToken = Guid.CreateVersion7(),
            OwnerEntityType = Required(ownerEntityType, nameof(ownerEntityType)),
            OwnerEntityId = ownerEntityId,
            ObjectType = Required(objectType, nameof(objectType)),
            Data = ToDictionary(data)
        };

        if (ownerEntityId == Guid.Empty) throw new ArgumentException("An owner is required.", nameof(ownerEntityId));

        EnsureRegistered(value.OwnerEntityType, value.ObjectType);

        _ = await dbContext.ProjectObjects.AddAsync(value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        try
        {
            await refreshRequestHandler.HandleAsync(new ProjectObjectRefreshRequest(requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required."), value.ObjectType));
        }
        catch (InvalidOperationException exception)
        {
            throw new ProjectObjectRefreshUnavailableException(value.Id, value.ConcurrencyToken, exception);
        }
        return ToRead(value);
    }

    public async Task<ProjectObjectReadDto?> UpdateAsync(
        Guid id,
        string ownerEntityType,
        Guid ownerEntityId,
        string objectType,
        JsonElement data,
        Guid concurrencyToken,
        CancellationToken cancellationToken)
    {
        ProjectObject? value = await dbContext.ProjectObjects.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (value is null) return null;
        if (value.ConcurrencyToken != concurrencyToken)
        {
            throw new InvalidOperationException("The project object was modified by another process.");
        }

        if (!string.Equals(value.OwnerEntityType, ownerEntityType, StringComparison.Ordinal)
            || value.OwnerEntityId != ownerEntityId
            || !string.Equals(value.ObjectType, objectType, StringComparison.Ordinal))
        {
            return null;
        }

        value.OwnerEntityType = Required(ownerEntityType, nameof(ownerEntityType));
        value.OwnerEntityId = ownerEntityId;
        value.ObjectType = Required(objectType, nameof(objectType));
        EnsureRegistered(value.OwnerEntityType, value.ObjectType);
        value.Data = ToDictionary(data);
        value.ConcurrencyToken = Guid.CreateVersion7();
        await SaveChangesAsync(cancellationToken);
        try
        {
            await refreshRequestHandler.HandleAsync(new ProjectObjectRefreshRequest(requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required."), value.ObjectType));
        }
        catch (InvalidOperationException exception)
        {
            throw new ProjectObjectRefreshUnavailableException(value.Id, value.ConcurrencyToken, exception);
        }
        return ToRead(value);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken)
    {
        ProjectObject? value = await dbContext.ProjectObjects.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (value is null) return false;
        if (value.ConcurrencyToken != concurrencyToken)
        {
            throw new InvalidOperationException("The project object was modified by another process.");
        }
        value.DeletedAt = DateTime.UtcNow;
        value.ConcurrencyToken = Guid.CreateVersion7();
        await SaveChangesAsync(cancellationToken);
        try
        {
            await refreshRequestHandler.HandleAsync(new ProjectObjectRefreshRequest(requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required."), value.ObjectType));
        }
        catch (InvalidOperationException exception)
        {
            throw new ProjectObjectRefreshUnavailableException(value.Id, value.ConcurrencyToken, exception);
        }
        return true;
    }

    private static ProjectObjectReadDto ToRead(ProjectObject value)
    {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(value.Data));
        return new(value.Id, value.OwnerEntityType, value.OwnerEntityId, value.ObjectType, document.RootElement.Clone(), value.ConcurrencyToken, value.CreatedAt, value.CreatedBy, value.UpdatedAt, value.UpdatedBy, value.DeletedAt, value.DeletedBy);
    }

    private static Dictionary<string, object?> ToDictionary(JsonElement data)
        => data.ValueKind == JsonValueKind.Object
            ? JsonSerializer.Deserialize<Dictionary<string, object?>>(data.GetRawText()) ?? []
            : throw new ArgumentException("Project object data must be a JSON object.", nameof(data));

    private static string Required(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", parameterName) : value.Trim();

    private void EnsureRegistered(string ownerEntityType, string objectType)
    {
        if (!typeRegistry.IsRegistered(ownerEntityType, objectType))
        {
            throw new InvalidOperationException($"Project object type '{objectType}' is not registered for owner entity type '{ownerEntityType}'.");
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException("The project object was modified by another process.", exception);
        }
    }

}
