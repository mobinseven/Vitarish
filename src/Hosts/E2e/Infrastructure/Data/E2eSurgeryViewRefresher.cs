using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.E2e.Infrastructure.Data;

internal sealed class E2eSurgeryViewRefresher(
    ClinicDbContext clinicDbContext,
    SeaDbContext seaDbContext) : IProjectObjectMaterializedViewRefresher
{
    public string ObjectType => SurgeryObjectTypes.ObjectType;

    public async Task RefreshAsync(string tenantId, CancellationToken cancellationToken)
    {
        List<ProjectObject> projectObjects = await seaDbContext.ProjectObjects.AsNoTracking()
            .Where(item => item.OwnerEntityType == SurgeryObjectTypes.OwnerEntityType
                && item.ObjectType == SurgeryObjectTypes.ObjectType
                && item.DeletedAt == null
                && EF.Property<string>(item, "TenantId") == tenantId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        SurgeryView[] views = projectObjects.Select(item => ToView(item, tenantId)).ToArray();
        SurgerySuggestionView[] suggestions = BuildSuggestions(tenantId, views);

        List<SurgeryView> staleViews = await clinicDbContext.SurgeryViews
            .Where(item => item.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        List<SurgerySuggestionView> staleSuggestions = await clinicDbContext.SurgerySuggestionViews
            .Where(item => item.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        clinicDbContext.SurgeryViews.RemoveRange(staleViews);
        clinicDbContext.SurgerySuggestionViews.RemoveRange(staleSuggestions);
        await clinicDbContext.SaveChangesAsync(cancellationToken);

        clinicDbContext.SurgeryViews.AddRange(views);
        clinicDbContext.SurgerySuggestionViews.AddRange(suggestions);
        await clinicDbContext.SaveChangesAsync(cancellationToken);
    }

    private static SurgeryView ToView(ProjectObject projectObject, string tenantId)
    {
        using JsonDocument data = JsonDocument.Parse(JsonSerializer.Serialize(projectObject.Data));
        JsonElement root = data.RootElement;
        return new SurgeryView
        {
            Id = projectObject.Id,
            TenantId = tenantId,
            PatientId = projectObject.OwnerEntityId,
            ObjectType = projectObject.ObjectType,
            ConcurrencyToken = projectObject.ConcurrencyToken,
            Data = JsonDocument.Parse(root.GetRawText()),
            Status = ReadString(root, nameof(SurgeryWriteModel.Status)) ?? string.Empty,
            ScheduledAt = ReadScheduledAt(root),
            DurationMinutes = ReadDurationMinutes(root),
            Location = ReadLocation(root),
            Team = ReadTeam(root),
            CreatedAt = projectObject.CreatedAt,
            UpdatedAt = projectObject.UpdatedAt
        };
    }

    private static SurgerySuggestionView[] BuildSuggestions(string tenantId, IReadOnlyList<SurgeryView> views)
    {
        Dictionary<(string Kind, string Value), (long Count, DateTime LastUsedAt)> groups = [];
        foreach (SurgeryView view in views)
        {
            DateTime lastUsedAt = view.UpdatedAt is { } updatedAt && updatedAt > view.CreatedAt ? updatedAt : view.CreatedAt;
            if (!string.IsNullOrWhiteSpace(view.Location))
            {
                Accumulate(groups, SurgerySuggestionKinds.Location, view.Location.Trim(), lastUsedAt);
            }

            foreach (JsonElement member in view.Team.RootElement.EnumerateArray())
            {
                if (member.ValueKind == JsonValueKind.Object
                    && member.TryGetProperty(nameof(SurgeryTeamMember.Role), out JsonElement role)
                    && role.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(role.GetString()))
                {
                    Accumulate(groups, SurgerySuggestionKinds.Role, role.GetString()!.Trim(), lastUsedAt);
                }
            }
        }

        return groups.Select(group => new SurgerySuggestionView
        {
            TenantId = tenantId,
            Kind = group.Key.Kind,
            Value = group.Key.Value,
            UseCount = group.Value.Count,
            LastUsedAt = group.Value.LastUsedAt
        }).ToArray();
    }

    private static void Accumulate(
        Dictionary<(string Kind, string Value), (long Count, DateTime LastUsedAt)> groups,
        string kind,
        string value,
        DateTime lastUsedAt)
    {
        if (groups.TryGetValue((kind, value), out (long Count, DateTime LastUsedAt) current))
        {
            groups[(kind, value)] = (current.Count + 1, current.LastUsedAt > lastUsedAt ? current.LastUsedAt : lastUsedAt);
        }
        else
        {
            groups[(kind, value)] = (1, lastUsedAt);
        }
    }

    private static string? ReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTimeOffset ReadScheduledAt(JsonElement root)
    {
        string? value = ReadString(root, nameof(SurgeryWriteModel.ScheduledAt));
        return value is not null && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed)
            ? parsed
            : default;
    }

    private static int? ReadDurationMinutes(JsonElement root)
        => root.TryGetProperty(nameof(SurgeryWriteModel.DurationMinutes), out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out int parsed)
                ? parsed
                : null;

    private static string? ReadLocation(JsonElement root)
    {
        string? value = ReadString(root, nameof(SurgeryWriteModel.Location));
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static JsonDocument ReadTeam(JsonElement root)
        => root.TryGetProperty(nameof(SurgeryWriteModel.Team), out JsonElement value) && value.ValueKind == JsonValueKind.Array
            ? JsonDocument.Parse(value.GetRawText())
            : JsonDocument.Parse("[]");
}
