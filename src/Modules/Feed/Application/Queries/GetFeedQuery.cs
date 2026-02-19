using MediatR;
using Vitarish.Feed.Domain.Models;

namespace Vitarish.Feed.Application.Queries;

/// <summary>
/// Query to fetch aggregated feed items from all enabled sources
/// </summary>
public record GetFeedQuery(FeedQueryOptions Options) : IRequest<IReadOnlyList<FeedItem>>;
