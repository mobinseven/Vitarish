using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class ConfigureWorkflowPolicyCommandHandler(ClinicalDbContext dbContext)
    : IRequestHandler<ConfigureWorkflowPolicyCommand, WorkflowPolicyReadDto>
{
    public async Task<WorkflowPolicyReadDto> Handle(ConfigureWorkflowPolicyCommand request, CancellationToken cancellationToken)
    {
        Domain.Entities.WorkflowPolicy? existing = await dbContext.WorkflowPolicies
            .FirstOrDefaultAsync(p => p.VisitType == request.WriteDto.VisitType && p.IsActive, cancellationToken);

        if (existing is not null)
        {
            existing.IsActive = false;
            existing.EffectiveTo = DateTimeOffset.UtcNow;
        }

        Domain.Entities.WorkflowPolicy policy = new()
        {
            VisitType = request.WriteDto.VisitType,
            PolicyJson = JsonSerializer.Serialize(request.WriteDto.Definition),
            IsActive = request.WriteDto.IsActive,
            EffectiveFrom = request.WriteDto.EffectiveFrom,
            EffectiveTo = request.WriteDto.EffectiveTo
        };

        dbContext.WorkflowPolicies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WorkflowPolicyReadDto
        {
            Id = policy.Id,
            VisitType = policy.VisitType,
            Definition = request.WriteDto.Definition,
            IsActive = policy.IsActive,
            EffectiveFrom = policy.EffectiveFrom,
            EffectiveTo = policy.EffectiveTo,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt
        };
    }
}
