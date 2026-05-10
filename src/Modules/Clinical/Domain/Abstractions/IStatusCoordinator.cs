using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IStatusCoordinator
{
    Task<VisitWorkflowStatus?> ReconcileAsync(
        ClinicalWorkflow workflow,
        CancellationToken cancellationToken = default);
}
