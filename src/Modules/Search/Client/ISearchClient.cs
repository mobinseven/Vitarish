using Vitarish.Search.Contracts.Requests;
using Vitarish.Search.Contracts.Responses;
using Vitarish.Search.Contracts.Results;
namespace Vitarish.Search.Client;
public interface ISearchClient
{
    Task<SearchClientResult<SearchResultDto>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<SearchClientResult<IReadOnlyList<SearchIndexStatusReportDto>>> GetIndexStatusAsync(GetSearchIndexStatusRequest request, CancellationToken cancellationToken = default);
}
