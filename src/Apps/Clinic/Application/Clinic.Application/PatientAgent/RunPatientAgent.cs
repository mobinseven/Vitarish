using MediatR;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record RunPatientAgent(PatientAgentContext Context, PatientAgentInput Input)
    : IRequest<PatientAgentResult>;
