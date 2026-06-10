using MediatR;
using Vitarish.Clinic.Domain.Models;

namespace Vitarish.Clinic.Application.Queries.Patients;

public sealed record GetPatientsQuery(string? Search = null, int? Skip = null, int? Take = null) : IRequest<IReadOnlyList<PatientListItemDto>>;
