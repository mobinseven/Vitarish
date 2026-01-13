namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

public partial class ClinicProjectGridCellDisplay
{
    private ClinicTemplateProjectMetadata? templateMetadata;

    [Parameter, EditorRequired]
    public ProjectGridCellDisplayContext Context { get; set; } = default!;

    protected StatsDto DisplayStats => Context.Stats ?? new();

    private string? visitChipLabel => ChipLabel;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        UpdateDisplayState(Context.Project.Metadata, Context.Project.Name);
    }

    private Task HandleClickedAsync() => InvokeClickAsync(Context.OnClicked, Context.Project.Id);

    protected override ClinicProjectMetadata? ParseMetadata(string? metadataJson) => ClinicProjectMetadataParser.TryParsePatient(metadataJson);

    protected override void OnMetadataAssigned(string? metadataJson) => templateMetadata = Metadata is null
            ? ClinicProjectMetadataParser.TryParseTemplate(metadataJson)
            : null;

    protected override string GetDefaultDisplayName() => Localizer["Clinic patient"];

    protected override string? GetChipLabel(ClinicProjectMetadata metadata) => metadata.VisitReason;
}
