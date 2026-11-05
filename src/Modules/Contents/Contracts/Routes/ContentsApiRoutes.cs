namespace Vitarish.Contents.Contracts.Routes;

public static class ContentsApiRoutes
{
    public const string Base = "/api/contents";
    public const string Items = "/items";
    public const string ItemById = "/items/{contentId:guid}";
    public const string ArchiveItem = "/items/{contentId:guid}/archive";
    public const string Revisions = "/items/{contentId:guid}/revisions";
    public const string Sites = "/sites";
    public const string Site = "/sites/{siteId:guid}";
    public const string Types = "/types";
    public const string Type = "/types/{typeId:guid}";
    public const string TypeSchema = "/types/{typeId:guid}/schema";
    public const string Entries = "/entries";
    public const string Entry = "/entries/{id:guid}";
    public const string EntryDraft = "/entries/{id:guid}/draft";
    public const string EntryPublish = "/entries/{id:guid}/publish";
    public const string EntryUnpublish = "/entries/{id:guid}/unpublish";
    public const string EntryArchive = "/entries/{id:guid}/archive";
    public const string EntryRestore = "/entries/{id:guid}/restore";
    public const string EntryRevisions = "/entries/{id:guid}/revisions";
    public const string EntryRevision = "/entries/{id:guid}/revisions/{revisionId:guid}";
    public const string Public = Base + "/public";
    public const string PublicSite = "/sites/{siteKey}";
    public const string PublicEntry = "/sites/{siteKey}/entries";
    public const string PublicTypeEntries = "/sites/{siteKey}/types/{typeKey}/entries";
}
