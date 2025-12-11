namespace Vitarish.Files.Infrastructure.Extensions;

public static class FilesExtensions
{
    public static string ExtractId(string fileName, string prefix)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(prefix);

        string expectedPrefix = prefix + "-";
        if (!fileName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("The given fileName have not the given prefix.");
        }

        string withoutPrefix = fileName[expectedPrefix.Length..];

        int dotIndex = withoutPrefix.IndexOf('.');
        if (dotIndex < 0)
        {
            throw new FormatException("The given file name have not extension.");
        }

        string id = withoutPrefix[..dotIndex];
        return id;
    }
}
