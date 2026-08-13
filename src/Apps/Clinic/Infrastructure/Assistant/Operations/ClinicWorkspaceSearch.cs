using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.Assistant.Operations;

internal sealed class ClinicWorkspaceSearch(
    ClinicDbContext dbContext,
    PatientViewQueryShape queryShape,
    IDataProtectionProvider protectionProvider,
    TimeProvider timeProvider) : IClinicWorkspacePatientSearch
{
    private const string Purpose = "Vitarish.Clinic.ClinicWorkspaceCandidate.v1";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly IDataProtector _protector = protectionProvider.CreateProtector(Purpose);

    public async Task<ClinicWorkspaceSearchResult> SearchAsync(string practiceId, string userId, string agentRunId, string search, DateOnly? birthDate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(practiceId) || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(agentRunId) || string.IsNullOrWhiteSpace(search) || search.Length > 100)
            return new([], false);
        string term = search.Trim();
        IQueryable<Clinic.Domain.Models.PatientView> query = queryShape.ApplyFilters(dbContext.PatientViews.AsNoTracking(), practiceId, term, false);
        if (birthDate is { } date)
        {
            DateTime start = date.ToDateTime(TimeOnly.MinValue);
            query = query.Where(patient => patient.DateOfBirth >= start && patient.DateOfBirth < start.AddDays(1));
        }
        var patients = await query.OrderByDescending(patient => patient.Name == term || patient.IdentificationCode == term || patient.Phone == term || patient.Email == term).ThenBy(patient => patient.Name).Take(6).ToListAsync(cancellationToken);
        bool hasMore = patients.Count > 5;
        return new(patients.Take(5).Select(patient =>
        {
            string token = _protector.Protect(JsonSerializer.Serialize(
                new ClinicWorkspaceCandidatePayload(patient.Id, practiceId, userId, agentRunId, timeProvider.GetUtcNow().Add(Lifetime)),
                ClinicWorkspaceJsonSerializerContext.Default.ClinicWorkspaceCandidatePayload));
            return new ClinicWorkspaceSearchCandidate(token, patient.Name, patient.DateOfBirth is { } dob ? DateOnly.FromDateTime(dob) : null, Mask(patient.IdentificationCode), Mask(patient.Phone));
        }).ToArray(), hasMore);
    }

    public async Task<ClinicWorkspaceCandidateReference?> ResolveAsync(string practiceId, string userId, string agentRunId, string candidateToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (candidateToken.Length > 4_096) return null;
        try
        {
            ClinicWorkspaceCandidatePayload? payload = JsonSerializer.Deserialize(
                _protector.Unprotect(candidateToken),
                ClinicWorkspaceJsonSerializerContext.Default.ClinicWorkspaceCandidatePayload);
            if (payload is not { } candidate || candidate.ExpiresAt <= timeProvider.GetUtcNow() || candidate.PatientId == Guid.Empty ||
                candidate.PracticeId != practiceId || candidate.UserId != userId || candidate.AgentRunId != agentRunId)
                return null;
            Clinic.Domain.Models.PatientView? patient = await dbContext.PatientViews.AsNoTracking().FirstOrDefaultAsync(patient =>
                patient.Id == candidate.PatientId && patient.TenantId == practiceId && patient.DeletedAt == null && !patient.Template, cancellationToken);
            return patient is null ? null : new ClinicWorkspaceCandidateReference(candidate.PatientId, candidate.PracticeId, candidate.UserId, candidate.AgentRunId, patient.Name, Mask(patient.IdentificationCode), Mask(patient.Phone));
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or FormatException)
        {
            return null;
        }
    }

    private static string Mask(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Length <= 2 ? "**" : $"{value[0]}{new string('*', Math.Min(value.Length - 2, 8))}{value[^1]}";
}

internal sealed record ClinicWorkspaceCandidatePayload(Guid PatientId, string PracticeId, string UserId, string AgentRunId, DateTimeOffset ExpiresAt);
