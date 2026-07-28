using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.ClinicalEvents;

public sealed class ClinicClinicalEventProposalStore(ClinicDbContext dbContext) : IClinicalEventProposalStore
{
    public async Task SaveAsync(ClinicalEventProposal proposal, CancellationToken cancellationToken)
    {
        dbContext.ClinicalEventProposals.Add(ClinicalEventProposalEntity.From(proposal));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ClinicalEventProposal?> GetAsync(Guid actionId, CancellationToken cancellationToken)
    {
        ClinicalEventProposalEntity? entity = await dbContext.ClinicalEventProposals
            .AsNoTracking().SingleOrDefaultAsync(item => item.ActionId == actionId, cancellationToken);
        return entity?.ToProposal();
    }

    public async Task<bool> CompleteAsync(Guid actionId, Guid eventId, CancellationToken cancellationToken)
    {
        int updated = await dbContext.ClinicalEventProposals
            .Where(item => item.ActionId == actionId && item.Status == ClinicalEventProposalStatus.Prepared)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, ClinicalEventProposalStatus.Completed)
                .SetProperty(item => item.CreatedEventId, eventId), cancellationToken);
        return updated == 1;
    }
}

internal sealed class ClinicalEventProposalEntity
{
    public Guid ActionId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string AgentRunId { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string ClinicianId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ProposalJson { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid ProcedureId { get; set; }
    public Guid? PreviousEventId { get; set; }
    public string StateFingerprint { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public ClinicalEventProposalStatus Status { get; set; }
    public Guid? CreatedEventId { get; set; }

    public static ClinicalEventProposalEntity From(ClinicalEventProposal proposal) => new()
    {
        ActionId = proposal.ActionId,
        TenantId = proposal.Scope.PracticeId,
        AgentRunId = proposal.Scope.AgentRunId,
        PatientId = proposal.Scope.PatientId,
        ClinicianId = proposal.Scope.AuthenticatedClinicianId,
        ActionType = proposal.ActionType,
        ProposalJson = JsonSerializer.Serialize(proposal),
        ProjectId = proposal.ProjectId,
        ProcedureId = proposal.ProcedureId,
        PreviousEventId = proposal.PreviousEventId,
        StateFingerprint = proposal.StateFingerprint,
        ExpiresAt = proposal.ExpiresAt,
        Status = proposal.Status,
        CreatedEventId = proposal.CreatedEventId
    };

    public ClinicalEventProposal ToProposal()
    {
        ClinicalEventProposal proposal = JsonSerializer.Deserialize<ClinicalEventProposal>(ProposalJson)
            ?? throw new InvalidOperationException("The stored clinical event proposal is invalid.");
        return proposal with { Status = Status, CreatedEventId = CreatedEventId };
    }
}
