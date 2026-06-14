namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Controls how many retrieval strategies a search query may use.
/// </summary>
public enum SearchRetrievalMode
{
    Strict = 0,
    Balanced = 1,
    Broad = 2
}
