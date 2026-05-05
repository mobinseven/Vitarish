using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record ExecuteFhirOperationCommand(FhirOperationRequest Request) : IRequest<FhirOperationResponse>;
