using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Queries;

public sealed record ListProjectionsQuery(ProjectionFilter Filter) : IRequest<IReadOnlyList<ProjectionReadDto>>;
