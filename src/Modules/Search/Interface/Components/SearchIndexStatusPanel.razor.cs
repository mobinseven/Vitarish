using MediatR;
using Vitarish.Search.Application.Queries;

namespace Vitarish.Search.Interface.Components;

public partial class SearchIndexStatusPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private IReadOnlyList<IndexStatusReport>? _reports;
    private bool _loading;
    private string? _errorMessage;

    protected override async Task ReloadRequired()
    {
        _loading = true;
        _errorMessage = null;
        _reports = null;

        try
        {
            _reports = await Mediator.Send(new GetIndexStatusQuery());
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

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824)
            return $"{bytes / 1_073_741_824.0:F1} GB";

        if (bytes >= 1_048_576)
            return $"{bytes / 1_048_576.0:F1} MB";

        if (bytes >= 1_024)
            return $"{bytes / 1_024.0:F1} KB";

        return $"{bytes} B";
    }
}
