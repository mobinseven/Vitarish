using MediatR;
using Vitarish.Clinical.Application.Queries;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalConnectorHealthPanel : CommonBase
{
    private const int MaxDisplayedResourceTypes = 8;

    [Inject] private IMediator Mediator { get; set; } = default!;

    private ConnectorHealthReport? _report;
    private bool _loading;

    protected override async Task ReloadRequired()
    {
        _loading = true;

        try
        {
            _report = await Mediator.Send(new GetConnectorHealthQuery());
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private static Color GetStateColor(ConnectorHealthState state) => state switch
    {
        ConnectorHealthState.Healthy => Color.Success,
        ConnectorHealthState.Degraded => Color.Warning,
        ConnectorHealthState.Unreachable => Color.Error,
        _ => Color.Default
    };

    private static Severity GetSeverity(ConnectorHealthState state) => state switch
    {
        ConnectorHealthState.Degraded => Severity.Warning,
        ConnectorHealthState.Unreachable => Severity.Error,
        _ => Severity.Info
    };

    private string FormatResources(IReadOnlyList<string> resources) =>
        resources.Count == 0
            ? Localizer["Unavailable"]
            : string.Join(", ", resources.Take(MaxDisplayedResourceTypes))
                + (resources.Count > MaxDisplayedResourceTypes ? "…" : string.Empty);

    private string FormatSoftware(ConnectorHealthReport report)
    {
        if (string.IsNullOrWhiteSpace(report.SoftwareName) && string.IsNullOrWhiteSpace(report.SoftwareVersion))
        {
            return Localizer["Unavailable"];
        }

        return string.Join(' ', new[] { report.SoftwareName, report.SoftwareVersion }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
