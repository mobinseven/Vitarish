using MediatR;
using Vitarish.Clinic.Domain.Models;

namespace Vitarish.Clinic.Application.Queries.Patients;

public sealed record GetPatientByIdQuery(Guid PatientId) : IRequest<PatientView?>;
