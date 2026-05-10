using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class ListWorkflowPoliciesQueryHandler(ClinicalDbContext dbContext)
    : IRequestHandler<ListWorkflowPoliciesQuery, IReadOnlyList<WorkflowPolicyReadDto>>
{
    public async Task<IReadOnlyList<WorkflowPolicyReadDto>> Handle(ListWorkflowPoliciesQuery request, CancellationToken cancellationToken)
    {
        List<Domain.Entities.WorkflowPolicy> policies = await dbContext.WorkflowPolicies
            .AsNoTracking()
            .OrderByDescending(p => p.EffectiveFrom)
            .Take(100)
            .ToListAsync(cancellationToken);

        return policies.Select(policy =>
        {
            WorkflowPolicyDefinition? definition = null;
            if (!string.IsNullOrWhiteSpace(policy.PolicyJson))
            {
                try
                {
                    definition = JsonSerializer.Deserialize<WorkflowPolicyDefinition>(policy.PolicyJson);
                }
                catch { }
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
        }).ToArray();
    }
}
