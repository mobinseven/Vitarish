namespace Vitarish.Files.Infrastructure.Extensions;

public static class FilesExtensions
{
    public static string ExtractId(string path, string prefix)
    {
        string fn = Path.GetFileName(path);
        string expectedPrefix = prefix + "-";
        if (!fn.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("The given path have not the given prefix.");
        }

        string withoutPrefix = fn[expectedPrefix.Length..];

        int dotIndex = withoutPrefix.IndexOf('.');
        if (dotIndex < 0)
        {
            throw new FormatException("The given file name have not extension.");
        }

        string id = withoutPrefix[..dotIndex];
        return id;
    }
}
