using MediatR;

namespace Vitarish.Clinic.Application.ClinicWorkspace;

public sealed record RunClinicWorkspace(ClinicWorkspaceContext Context, ClinicWorkspaceInput Input)
    : IRequest<ClinicWorkspaceResult>;
