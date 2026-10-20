using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Domain.Enums;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.E2e.Infrastructure.Data;

/// <summary>
/// Rebuilds the current clinic's patient view from its procedure projects, as the
/// <c>mv_Patients</c> materialized view does on PostgreSQL, which the in-memory store cannot refresh.
/// </summary>
internal sealed class E2ePatientViewRefresher(
    ClinicDbContext clinicDbContext,
    SeaDbContext seaDbContext,
    IRequestContext requestContext) : IRequestHandler<PatientViewRefreshCommand>
{
    public async Task Handle(PatientViewRefreshCommand request, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required to refresh the patient view.");

        List<Project> projects = await seaDbContext.Projects.AsNoTracking()
            .OrderBy(project => project.CreatedAt)
            .ToListAsync(cancellationToken);
        Dictionary<Guid, string[]> userIds = (await seaDbContext.ProjectUsers.AsNoTracking()
                .Select(projectUser => new { projectUser.ProjectId, projectUser.UserId })
                .ToListAsync(cancellationToken))
            .GroupBy(projectUser => projectUser.ProjectId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.UserId).Order(StringComparer.Ordinal).ToArray());

        List<PatientView> staleViews = await clinicDbContext.Set<PatientView>()
            .Where(view => view.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        clinicDbContext.Set<PatientView>().RemoveRange(staleViews);
        _ = await clinicDbContext.SaveChangesAsync(cancellationToken);

        clinicDbContext.Set<PatientView>().AddRange(projects.Select(project =>
            ToView(project, tenantId, userIds.GetValueOrDefault(project.Id, []))));
        _ = await clinicDbContext.SaveChangesAsync(cancellationToken);
    }

    private static PatientView ToView(Project project, string tenantId, string[] userIds)
    {
        using JsonDocument metadata = JsonDocument.Parse(JsonSerializer.Serialize(project.Metadata));
        JsonElement root = metadata.RootElement;
        return new PatientView
        {
            Id = project.Id,
            TenantId = tenantId,
            Name = project.Name,
            Description = project.Description,
            AvatarFileId = Guid.TryParse(ReadString(root, nameof(PatientWriteDto.AvatarFileId)), out Guid avatarFileId) ? avatarFileId : null,
            IdentificationCode = project.IdentificationCode,
            Template = project.Template,
            CreatedAt = project.CreatedAt,
            CreatedBy = project.CreatedBy,
            UpdatedAt = project.UpdatedAt,
            UpdatedBy = project.UpdatedBy,
            UserIds = userIds,
            Tags = ReadTags(root),
            FirstName = ReadString(root, nameof(PatientWriteDto.FirstName)),
            LastName = ReadString(root, nameof(PatientWriteDto.LastName)),
            DateOfBirth = DateTime.TryParse(ReadString(root, nameof(PatientWriteDto.DateOfBirth)), out DateTime dateOfBirth) ? dateOfBirth : null,
            Gender = (Gender)ReadInt(root, nameof(PatientWriteDto.Gender)),
            PlaceOfBirth = ReadString(root, nameof(PatientWriteDto.PlaceOfBirth)),
            Address = ReadString(root, nameof(PatientWriteDto.Address)),
            Education = (Education)ReadInt(root, nameof(PatientWriteDto.Education)),
            Occupation = ReadString(root, nameof(PatientWriteDto.Occupation)),
            FatherName = ReadString(root, nameof(PatientWriteDto.FatherName)),
            Height = ReadInt(root, nameof(PatientWriteDto.Height)),
            Weight = ReadInt(root, nameof(PatientWriteDto.Weight)),
            Email = ReadString(root, nameof(PatientWriteDto.Email)),
            Phone = ReadString(root, nameof(PatientWriteDto.Phone)),
            Referrer = ReadString(root, nameof(PatientWriteDto.Referrer)),
            InsuranceProvider = ReadString(root, nameof(PatientWriteDto.InsuranceProvider)),
            VisitReason = ReadString(root, nameof(PatientWriteDto.VisitReason)),
            Allergies = ReadString(root, nameof(PatientWriteDto.Allergies)),
            CurrentMedications = ReadString(root, nameof(PatientWriteDto.CurrentMedications)),
            ClinicalNotes = ReadString(root, nameof(PatientWriteDto.ClinicalNotes)),
            EmergencyContactName = ReadString(root, nameof(PatientWriteDto.EmergencyContactName)),
            EmergencyContactPhone = ReadString(root, nameof(PatientWriteDto.EmergencyContactPhone))
        };
    }

    private static string? ReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int ReadInt(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out int parsed)
                ? parsed
                : 0;

    private static string[] ReadTags(JsonElement root)
        => root.TryGetProperty(nameof(PatientWriteDto.Tags), out JsonElement value) && value.ValueKind == JsonValueKind.Array
            ? PatientTags.Normalize(value.EnumerateArray()
                .Where(tag => tag.ValueKind == JsonValueKind.String)
                .Select(tag => tag.GetString()!)
                .ToArray())
            : [];
}
