using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Vitarish.Contents.Contracts.Models;
using Vitarish.Contents.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Contents.Api;

internal static partial class StructuredContentsApi
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(ContentsApiRoutes.Base);
        group.RequireAuthorization(IdentityDefinitions.TenantOwner);
        group.MapModuleGet(ContentsApiRoutes.Sites, ListSitesAsync, ContentsApiOperations.ListSites, typeof(SiteResponse[]), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePost(ContentsApiRoutes.Sites, CreateSiteAsync, ContentsApiOperations.CreateSite, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePut(ContentsApiRoutes.Site, UpdateSiteAsync, ContentsApiOperations.UpdateSite, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModuleGet(ContentsApiRoutes.Types, ListTypesAsync, ContentsApiOperations.ListTypes, typeof(ContentTypeResponse[]), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModuleGet(ContentsApiRoutes.Type, GetTypeAsync, ContentsApiOperations.GetContentType, typeof(ContentTypeResponse), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePost(ContentsApiRoutes.Types, CreateTypeAsync, ContentsApiOperations.CreateType, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePut(ContentsApiRoutes.TypeSchema, UpdateSchemaAsync, ContentsApiOperations.UpdateTypeSchema, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModuleGet(ContentsApiRoutes.Entries, ListEntriesAsync, ContentsApiOperations.ListEntries, typeof(EntryResponse[]), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePost(ContentsApiRoutes.Entries, CreateEntryAsync, ContentsApiOperations.CreateEntry, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModuleGet(ContentsApiRoutes.Entry, GetEntryAsync, ContentsApiOperations.GetEntry, typeof(EntryResponse), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePut(ContentsApiRoutes.EntryDraft, SaveDraftAsync, ContentsApiOperations.SaveDraft, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePost(ContentsApiRoutes.EntryPublish, PublishAsync, ContentsApiOperations.Publish, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePost(ContentsApiRoutes.EntryUnpublish, UnpublishAsync, ContentsApiOperations.Unpublish, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePost(ContentsApiRoutes.EntryArchive, ArchiveAsync, ContentsApiOperations.Archive, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModulePost(ContentsApiRoutes.EntryRestore, RestoreAsync, ContentsApiOperations.Restore, typeof(EntryOutcome), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModuleGet(ContentsApiRoutes.EntryRevisions, ListRevisionsAsync, ContentsApiOperations.ListRevisions, typeof(RevisionResponse[]), profile: ModuleEndpointProfile.ProtectedJson);
        group.MapModuleGet(ContentsApiRoutes.EntryRevision, GetRevisionAsync, ContentsApiOperations.GetRevision, typeof(RevisionResponse), profile: ModuleEndpointProfile.ProtectedJson);

        RouteGroupBuilder publicGroup = endpoints.MapAnonymousModuleGroup(ContentsApiRoutes.Public, ContentsPublicDelivery.AnonymousJustification);
        publicGroup.MapModuleGet(ContentsApiRoutes.PublicSite, GetPublicSiteAsync, ContentsApiOperations.GetPublicSite, typeof(SiteResponse), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: ContentsPublicDelivery.AnonymousJustification).RequireRateLimiting(ContentsPublicDelivery.RateLimitPolicy);
        publicGroup.MapModuleGet(ContentsApiRoutes.PublicEntry, GetPublicEntryAsync, ContentsApiOperations.GetPublicEntry, typeof(PublishedEntryResponse), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: ContentsPublicDelivery.AnonymousJustification).RequireRateLimiting(ContentsPublicDelivery.RateLimitPolicy);
        publicGroup.MapModuleGet(ContentsApiRoutes.PublicTypeEntries, ListPublicEntriesAsync, ContentsApiOperations.ListPublicEntries, typeof(PublishedEntryResponse[]), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: ContentsPublicDelivery.AnonymousJustification).RequireRateLimiting(ContentsPublicDelivery.RateLimitPolicy);
    }
}
