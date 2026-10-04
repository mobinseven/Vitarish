namespace Vitarish.Users.Infrastructure.Data;

internal static class PhoneNumberNormalizationSql
{
    internal const string ResourceName = "Vitarish.Users.Infrastructure.Data.PhoneNumberNormalization.sql";

    internal static string FunctionDefinition { get; } = Load();

    private static string Load()
    {
        using Stream stream = typeof(PhoneNumberNormalizationSql).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{ResourceName}'.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
