using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Queries;

public sealed record ListVisitWorkflowsQuery(VisitWorkflowFilter Filter) : IRequest<IReadOnlyList<VisitWorkflowReadDto>>;
