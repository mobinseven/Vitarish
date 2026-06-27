using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Entities;

namespace Vitarish.Main.Pages.Platform;

public partial class PlatformAdminConnectionMemoryPage : PlatformAdminPageBase
{
    [Inject] private IPlatformConnectionMemoryQuery MemoryQuery { get; set; } = default!;

    private IReadOnlyList<ConnectionMemorySnapshot> Samples { get; set; } = [];
    private const int DefaultLimit = 50;
    private string SummaryText => $"{Localizer["Showing recent"]} {Samples.Count} {Localizer["samples"]}";

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        Samples = await MemoryQuery.GetRecentSamplesAsync(DefaultLimit, CancellationToken.None);
        StateHasChanged();
    }

    private string KindLabel(ConnectionMemorySampleKind kind) => kind switch
    {
        ConnectionMemorySampleKind.Opened => Localizer["Circuit Opened"],
        ConnectionMemorySampleKind.Sampled => Localizer["Periodic Sample"],
        ConnectionMemorySampleKind.Closed => Localizer["Circuit Closed"],
        ConnectionMemorySampleKind.Failed => Localizer["Sampling Failed"],
        _ => kind.ToString()
    };

    private string ObservedAtLabel(DateTimeOffset observedAt) 
        => $"{Localizer["Observed at"]}: {observedAt:yyyy-MM-dd HH:mm:ss} UTC";

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
