namespace Vitarish.Users.Infrastructure.Data;

internal static class UserNameFollowsContactIdentifierSql
{
    internal const string ResourceName = "Vitarish.Users.Infrastructure.Data.UserNameFollowsContactIdentifier.sql";

    internal static string Script { get; } = Load();

    private static string Load()
    {
        using Stream stream = typeof(UserNameFollowsContactIdentifierSql).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{ResourceName}'.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
