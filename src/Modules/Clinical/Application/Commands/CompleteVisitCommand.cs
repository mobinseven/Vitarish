using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record CompleteVisitCommand(
    Guid WorkflowId,
    string ActorId) : IRequest<ClinicalActionResult>;
