using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Endpoints;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Application.Queries;
using Vitarish.Search.Contracts.Errors;
using Vitarish.Search.Contracts.Requests;
using Vitarish.Search.Contracts.Responses;
using Vitarish.Search.Contracts.Routes;
using Vitarish.Search.Domain.Models;
namespace Vitarish.Search.Api;
public static class SearchApiEndpointMapping
{
    public static IEndpointRouteBuilder MapSearchApi(this IEndpointRouteBuilder endpoints){ var group=endpoints.MapProtectedModuleGroup(SearchApiRoutes.Base); _=group.MapModulePost(SearchApiRoutes.Search, SearchAsync, "SearchSearch"); _=group.MapModuleGet(SearchApiRoutes.IndexStatus, GetIndexStatusAsync, "SearchGetIndexStatus"); _=group.MapModuleGet(SearchApiRoutes.IndexPolicyReports, GetIndexPolicyReports, "SearchGetIndexPolicyReports"); _=group.MapModulePost(SearchApiRoutes.RebuildIndex, RebuildIndexAsync, "SearchRebuildIndex"); _=group.MapModulePost(SearchApiRoutes.IndexDocument, IndexDocumentAsync, "SearchIndexDocument"); _=group.MapModulePost(SearchApiRoutes.DeleteDocument, DeleteDocumentAsync, "SearchDeleteDocument"); return endpoints; }
    private static async Task<IResult> SearchAsync(SearchRequest request, IMediator mediator, CancellationToken cancellationToken){ if(string.IsNullOrWhiteSpace(request.Query)) return Results.BadRequest(new SearchError(SearchErrorCode.Validation,"Search query is required.")); SearchResult result=await mediator.Send(new SearchQuery(request.IndexName, request.Query, ToDomain(request.Options)), cancellationToken); return Results.Ok(ToDto(result)); }
    private static async Task<IResult> GetIndexStatusAsync(IMediator mediator, CancellationToken cancellationToken){ IReadOnlyList<IndexStatusReport> reports=await mediator.Send(new GetIndexStatusQuery(), cancellationToken); return Results.Ok(reports.Select(ToDto).ToArray()); }

    private static IResult GetIndexPolicyReports(ISearchService service) => Results.Ok(service.GetIndexPolicyReports().Select(ToDto).ToArray());
    private static async Task<IResult> RebuildIndexAsync(RebuildSearchIndexRequest request, ISearchService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IndexName)) return Results.BadRequest(new SearchError(SearchErrorCode.Validation, "Index name is required."));
        await service.RebuildIndexAsync(request.IndexName, cancellationToken);
        return Results.Ok(new SearchCommandResult(true));
    }
    private static async Task<IResult> IndexDocumentAsync(IndexSearchDocumentRequest request, ISearchService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IndexName)) return Results.BadRequest(new SearchError(SearchErrorCode.Validation, "Index name is required."));
        if (request.Documents is null || request.Documents.Count == 0) return Results.BadRequest(new SearchError(SearchErrorCode.Validation, "Documents list cannot be null or empty."));
        await service.IndexAsync(request.IndexName, request.Documents.Select(ToDomain), cancellationToken);
        return Results.Ok(new SearchCommandResult(true));
    }
    private static async Task<IResult> DeleteDocumentAsync(DeleteSearchDocumentRequest request, ISearchService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IndexName) || string.IsNullOrWhiteSpace(request.TenantId)) return Results.BadRequest(new SearchError(SearchErrorCode.Validation, "Index name and tenant id are required."));
        await service.DeleteAsync(request.IndexName, new SearchDeleteOptions { TenantId = request.TenantId, Type = request.Type, CorpusId = request.CorpusId, MetadataFilters = request.MetadataFilters ?? new Dictionary<string, string>() }, cancellationToken);
        return Results.Ok(new SearchCommandResult(true));
    }
    private static SearchOptions ToDomain(SearchOptionsDto? dto)
    {
        dto ??= new SearchOptionsDto();
        return new(){ Skip=Math.Max(0,dto.Skip), Limit=SearchOptionsDto.NormalizeLimit(dto.Limit), Type=dto.Type, TenantId=dto.TenantId, CorpusId=dto.CorpusId, MetadataFilters=dto.MetadataFilters, RetrievalMode=(SearchRetrievalMode)dto.RetrievalMode, MinScore=dto.MinScore, Explain=dto.Explain };
    }
    private static SearchResultDto ToDto(SearchResult result)=>new(){ Items=result.Items.Select(ToDto).ToArray(), TotalCount=result.TotalCount, Query=result.Query, Diagnostics=result.Diagnostics is null?null:new SearchDiagnosticsDto{ RetrievalMode=(SearchRetrievalModeDto)result.Diagnostics.RetrievalMode, AppliedStrategies=result.Diagnostics.AppliedStrategies, Data=result.Diagnostics.Data } };
    private static SearchDocument ToDomain(SearchDocumentDto item)=>new(){ Id=item.Id, Title=item.Title, Body=item.Body, SemanticText=item.SemanticText, TenantId=item.TenantId, Type=item.Type, NavigationUrl=item.NavigationUrl, IndexName=item.IndexName, Score=item.Score, Metadata=item.Metadata };
    private static SearchDocumentDto ToDto(SearchDocument item)=>new(){ Id=item.Id, Title=item.Title, Body=item.Body, SemanticText=item.SemanticText, TenantId=item.TenantId, Type=item.Type, NavigationUrl=item.NavigationUrl, IndexName=item.IndexName, Score=item.Score, Metadata=item.Metadata };
    private static SearchIndexStatusReportDto ToDto(IndexStatusReport report)=>new(){ IndexName=report.IndexName, DocumentCount=report.DocumentCount, StoreSizeBytes=report.StoreSizeBytes };
    private static SearchIndexPolicyReportDto ToDto(SearchIndexPolicyReport report)=>new(){ IndexName=report.IndexName, ExactMatchPolicy=new SearchExactMatchPolicyDto{ IsExactMatchFirst=report.ExactMatchPolicy.IsExactMatchFirst, FieldNames=report.ExactMatchPolicy.FieldNames, PriorityTier=(SearchPriorityTierDto)report.ExactMatchPolicy.PriorityTier } };
}
