using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class CreatePatientVisitsProcedureHandler(IProceduresService procedures)
{
    internal const string ReservedProcedureName = "Visits";

    public async Task<bool> CreateAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await procedures.AddAsync(new ProcedureWriteDto
        {
            ProjectId = patientId,
            Name = ReservedProcedureName,
            Description = string.Empty
        }, cancellationToken);

        return result.IsSuccess && result.Value is not null;
    }
}
