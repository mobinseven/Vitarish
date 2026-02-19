using MediatR;
using Vitarish.Modules.Feed.Domain.Abstractions;

namespace Vitarish.Modules.Feed.Application.Queries;

/// <summary>
/// Query to get all available feed data sources
/// </summary>
public record GetFeedSourcesQuery : IRequest<IReadOnlyList<IFeedDataSource>>;
