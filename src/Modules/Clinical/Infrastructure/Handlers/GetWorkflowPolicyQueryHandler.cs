using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetWorkflowPolicyQueryHandler(ClinicalDbContext dbContext)
    : IRequestHandler<GetWorkflowPolicyQuery, WorkflowPolicyReadDto?>
{
    public async Task<WorkflowPolicyReadDto?> Handle(GetWorkflowPolicyQuery request, CancellationToken cancellationToken)
    {
        Domain.Entities.WorkflowPolicy? policy = await dbContext.WorkflowPolicies
            .AsNoTracking()
            .Where(p => p.VisitType == request.VisitType && p.IsActive)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            return null;
        }

        WorkflowPolicyDefinition? definition = null;
        if (!string.IsNullOrWhiteSpace(policy.PolicyJson))
        {
            try
            {
                definition = JsonSerializer.Deserialize<WorkflowPolicyDefinition>(policy.PolicyJson);
            }
            catch
            {
                // fall through with null
            }
        }

        return new WorkflowPolicyReadDto
        {
            Id = policy.Id,
            VisitType = policy.VisitType,
            Definition = definition,
            IsActive = policy.IsActive,
            EffectiveFrom = policy.EffectiveFrom,
            EffectiveTo = policy.EffectiveTo,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt
        };
    }
}
