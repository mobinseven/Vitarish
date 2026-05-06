using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Queries;

public sealed record GetVisitQueueQuery(
    string Role,
    string? LocationId = null) : IRequest<IReadOnlyList<OperationalProjectionSummary>>;
