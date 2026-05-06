using MediatR;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Application.Queries;

/// <summary>
/// Query that searches an optional <paramref name="IndexName"/> for <paramref name="Query"/>.
/// When <paramref name="IndexName"/> is <see langword="null"/>, all registered indexes are searched.
/// </summary>
public record SearchQuery(string? IndexName, string Query, SearchOptions Options) : IRequest<SearchResult>;
