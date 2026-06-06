namespace Vitarish.Files.Interface.Models;

public sealed record FileInputOptions
{
    public required string Accept { get; init; }

    public bool Multiple { get; init; }

    public string? Capture { get; init; }

    public bool Hidden { get; init; }
}
