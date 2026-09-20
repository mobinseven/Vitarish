using MediatR;

namespace Vitarish.Clinic.Application.Queries.Patients;

public sealed record CountPatientsQuery(
    string? Search = null,
    bool? Template = false,
    IReadOnlyList<string>? Tags = null) : IRequest<int>;
