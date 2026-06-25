using Vitarish.Search.Client;
using Vitarish.Search.Contracts.Requests;
using Vitarish.Search.Contracts.Responses;

namespace Vitarish.Search.Interface.Components;

public partial class SearchIndexStatusPanel : CommonBase
{
    [Inject] private ISearchClient SearchClient { get; set; } = default!;

    private IReadOnlyList<SearchIndexStatusReportDto>? _reports;
    private bool _loading;
    private string? _errorMessage;

    protected override async Task ReloadRequired()
    {
        _loading = true;
        _errorMessage = null;
        _reports = null;

        try
        {
            var result = await SearchClient.GetIndexStatusAsync(new GetSearchIndexStatusRequest(), CancellationToken.None);
            if (!result.IsSuccess || result.Value is null)
            {
                _errorMessage = result.Error?.Message ?? Localizer["IndexStatusLoadFailed"];
                return;
            }

            _reports = result.Value;
        }
        catch
        {
            _errorMessage = Localizer["IndexStatusLoadFailed"];
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private static string FormatSize(long bytes) => bytes >= 1_073_741_824
            ? $"{bytes / 1_073_741_824.0:F1} GB"
            : bytes >= 1_048_576 ? $"{bytes / 1_048_576.0:F1} MB" : bytes >= 1_024 ? $"{bytes / 1_024.0:F1} KB" : $"{bytes} B";
}
