namespace Vitarish.Contents.Contracts.Routes;

public static class ContentsApiRoutes
{
    public const string Base = "/api/contents";
    public const string Items = "/items";
    public const string ItemById = "/items/{contentId:guid}";
    public const string ArchiveItem = "/items/{contentId:guid}/archive";
    public const string Revisions = "/items/{contentId:guid}/revisions";
}
