using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Search.Application.Queries;
using Vitarish.Search.Contracts.Errors;
using Vitarish.Search.Contracts.Requests;
using Vitarish.Search.Contracts.Responses;
using Vitarish.Search.Contracts.Routes;
using Vitarish.Search.Domain.Models;
namespace Vitarish.Search.Api;
public static class SearchApiEndpointMapping
{
    public static IEndpointRouteBuilder MapSearchApi(this IEndpointRouteBuilder endpoints){ var group=endpoints.MapGroup(SearchApiRoutes.Base); _=group.MapPost(SearchApiRoutes.Search, SearchAsync); _=group.MapGet(SearchApiRoutes.IndexStatus, GetIndexStatusAsync); return endpoints; }
    private static async Task<IResult> SearchAsync(SearchRequest request, IMediator mediator, CancellationToken cancellationToken){ if(string.IsNullOrWhiteSpace(request.Query)) return Results.BadRequest(new SearchError(SearchErrorCode.Validation,"Search query is required.")); SearchResult result=await mediator.Send(new SearchQuery(request.IndexName, request.Query, ToDomain(request.Options)), cancellationToken); return Results.Ok(ToDto(result)); }
    private static async Task<IResult> GetIndexStatusAsync(IMediator mediator, CancellationToken cancellationToken){ IReadOnlyList<IndexStatusReport> reports=await mediator.Send(new GetIndexStatusQuery(), cancellationToken); return Results.Ok(reports.Select(ToDto).ToArray()); }
    private static SearchOptions ToDomain(SearchOptionsDto? dto)
    {
        dto ??= new SearchOptionsDto();
        return new(){ Skip=Math.Max(0,dto.Skip), Limit=SearchOptionsDto.NormalizeLimit(dto.Limit), Type=dto.Type, TenantId=dto.TenantId, CorpusId=dto.CorpusId, MetadataFilters=dto.MetadataFilters, RetrievalMode=(SearchRetrievalMode)dto.RetrievalMode, MinScore=dto.MinScore, Explain=dto.Explain };
    }
    private static SearchResultDto ToDto(SearchResult result)=>new(){ Items=result.Items.Select(ToDto).ToArray(), TotalCount=result.TotalCount, Query=result.Query, Diagnostics=result.Diagnostics is null?null:new SearchDiagnosticsDto{ RetrievalMode=(SearchRetrievalModeDto)result.Diagnostics.RetrievalMode, AppliedStrategies=result.Diagnostics.AppliedStrategies, Data=result.Diagnostics.Data } };
    private static SearchDocumentDto ToDto(SearchDocument item)=>new(){ Id=item.Id, Title=item.Title, Body=item.Body, SemanticText=item.SemanticText, TenantId=item.TenantId, Type=item.Type, NavigationUrl=item.NavigationUrl, IndexName=item.IndexName, Score=item.Score, Metadata=item.Metadata };
    private static SearchIndexStatusReportDto ToDto(IndexStatusReport report)=>new(){ IndexName=report.IndexName, DocumentCount=report.DocumentCount, StoreSizeBytes=report.StoreSizeBytes };
}
