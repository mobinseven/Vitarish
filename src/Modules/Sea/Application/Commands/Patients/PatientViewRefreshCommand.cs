namespace Vitarish.Sea.Application.Commands.Patients;

public sealed record PatientViewRefreshCommand(bool Concurrently = true) : IRequest;
