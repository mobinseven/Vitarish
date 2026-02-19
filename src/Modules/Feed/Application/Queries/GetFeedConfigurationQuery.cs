using MediatR;
using Vitarish.Feed.Domain.Models;

namespace Vitarish.Feed.Application.Queries;

/// <summary>
/// Query to get the current feed configuration
/// </summary>
public record GetFeedConfigurationQuery : IRequest<FeedConfiguration>;
