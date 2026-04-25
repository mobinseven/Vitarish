using MediatR;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Application.Queries;

/// <summary>
/// Query that searches <paramref name="IndexName"/> for <paramref name="Query"/>.
/// </summary>
public record SearchQuery(string IndexName, string Query, SearchOptions Options) : IRequest<SearchResult>;
