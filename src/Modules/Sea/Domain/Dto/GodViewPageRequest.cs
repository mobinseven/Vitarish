namespace Vitarish.Sea.Domain.Dto;

using System;
using System.Collections.Generic;

public sealed record GodViewPageRequest(int Page, int PageSize, IReadOnlyList<string> SearchTerms)
{
    public static GodViewPageRequest Create(int page, int pageSize, IReadOnlyList<string> searchTerms)
    {
        int normalizedPage = page < 0 ? 0 : page;
        int normalizedPageSize = pageSize <= 0 ? 25 : pageSize;
        IReadOnlyList<string> normalizedTerms = searchTerms.Count == 0 ? Array.Empty<string>() : searchTerms;
        return new GodViewPageRequest(normalizedPage, normalizedPageSize, normalizedTerms);
    }
}
