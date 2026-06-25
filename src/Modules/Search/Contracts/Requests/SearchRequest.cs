using Vitarish.Search.Contracts.Responses;
namespace Vitarish.Search.Contracts.Requests;
public sealed record SearchRequest(string? IndexName, string Query, SearchOptionsDto Options);
