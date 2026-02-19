using MediatR;
using Vitarish.Feed.Domain.Abstractions;

namespace Vitarish.Feed.Application.Queries;

/// <summary>
/// Query to get all available feed data sources
/// </summary>
public record GetFeedSourcesQuery : IRequest<IReadOnlyList<IFeedDataSource>>;
