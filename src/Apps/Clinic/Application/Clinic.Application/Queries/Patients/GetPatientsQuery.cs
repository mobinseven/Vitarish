using MediatR;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clinic.Application.Queries.Patients;

public sealed record GetPatientsQuery(
    string? Search = null,
    int? Skip = null,
    int? Take = null,
    bool? Template = false,
    ProjectSortBy? SortBy = ProjectSortBy.Name,
    bool Descending = false) : IRequest<IReadOnlyList<PatientView>>;
