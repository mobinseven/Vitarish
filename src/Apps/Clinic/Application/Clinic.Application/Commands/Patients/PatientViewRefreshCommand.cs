using MediatR;

namespace Vitarish.Clinic.Application.Commands.Patients;

public sealed record PatientViewRefreshCommand(bool Concurrently = true) : IRequest;
