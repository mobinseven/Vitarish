namespace Vitarish.Files.Contracts.Routes;

public static class FilesApiRoutes
{
    public const string Base = "/api/modules/files";
    public const string ById = "/{fileId:guid}";
    public const string ContentById = "/{fileId:guid}/content";
    public const string Upload = "/upload";
    public const string UploadContentField = "content";
}
