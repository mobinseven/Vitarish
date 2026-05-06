using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IClinicalActionService
{
    Task<ClinicalActionResult> DispatchAsync(ClinicalActionRequest request, Guid? workflowId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicalActionRecordReadDto>> ListRecordsAsync(ClinicalActionRecordFilter filter, CancellationToken cancellationToken = default);
}
