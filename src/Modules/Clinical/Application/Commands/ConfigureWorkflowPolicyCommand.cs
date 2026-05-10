using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record ConfigureWorkflowPolicyCommand(
    string TenantId,
    WorkflowPolicyWriteDto WriteDto) : IRequest<WorkflowPolicyReadDto>;
