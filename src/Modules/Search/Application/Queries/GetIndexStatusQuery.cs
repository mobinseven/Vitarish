using MediatR;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Application.Queries;

/// <summary>
/// Query that retrieves the indexing status report for all registered search indexes.
/// </summary>
public record GetIndexStatusQuery : IRequest<IReadOnlyList<IndexStatusReport>>;
