using MediatR;
using Vitarish.Modules.Feed.Domain.Models;

namespace Vitarish.Modules.Feed.Application.Queries;

/// <summary>
/// Query to get the current feed configuration
/// </summary>
public record GetFeedConfigurationQuery : IRequest<FeedConfiguration>;
