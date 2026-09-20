using MediatR;
using Vitarish.Clinic.Domain.Enums;
using Vitarish.Clinic.Domain.Models;

namespace Vitarish.Clinic.Application.Queries.Patients;

public sealed record GetPatientsQuery(
    string? Search = null,
    int? Skip = null,
    int? Take = null,
    bool? Template = false,
    PatientSortBy? SortBy = PatientSortBy.Name,
    bool Descending = false,
    IReadOnlyList<string>? Tags = null) : IRequest<IReadOnlyList<PatientView>>;
