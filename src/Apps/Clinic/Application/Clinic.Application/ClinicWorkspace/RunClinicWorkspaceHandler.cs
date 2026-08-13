using MediatR;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;

namespace Vitarish.Clinic.Application.ClinicWorkspace;

public sealed class RunClinicWorkspaceHandler(
    IClinicWorkspaceRuntime runtime) : IRequestHandler<RunClinicWorkspace, ClinicWorkspaceResult>
{
    public async Task<ClinicWorkspaceResult> Handle(
        RunClinicWorkspace request,
        CancellationToken cancellationToken)
    {
        return await runtime.RunAsync(request.Context, request.Input, cancellationToken);
    }
}
