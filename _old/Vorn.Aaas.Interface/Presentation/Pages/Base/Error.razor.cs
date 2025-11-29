using System.Diagnostics;
namespace Vorn.Aaas.Interface.Presentation.Pages.Base;

public partial class Error
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized()
    {
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;

        logger.LogError(RequestId);
    }

}
