using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record RegisterPatientCommand(
    PatientRegistrationPayload Payload,
    string ActorId,
    string TenantId,
    string? AccessReason = null) : IRequest<ClinicalActionResult>;
