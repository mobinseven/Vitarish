namespace Vitarish.Files.Contracts.Routes;

public static class FilesApiRoutes
{
    public const string Base = "/api/modules/files";
    public const string ById = "/{fileId:guid}";
    public const string ContentById = "/{fileId:guid}/content";

    /// <summary>The <see cref="ContentById"/> address of one file.</summary>
    public static string ContentPath(Guid fileId) => $"{Base}/{fileId}/content";
    public const string Upload = "/upload";
    public const string UploadPolicy = "/upload-policy";
}
