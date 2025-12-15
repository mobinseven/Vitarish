using System.Text;

namespace Vitarish.Clients.Interface.Sea.Plugins.Gym.Projects;

public partial class GymProjectGridCellDisplay : ProjectsBase
{
    private GymTraineeMetadata? metadata;
    private string? traineeInitials;
    private string traineeDisplayName = string.Empty;
    private string? membershipChipLabel;
    private string? demographicsLine;
    private string? contactLine;

    [Parameter, EditorRequired]
    public ProjectGridCellDisplayContext Context { get; set; } = default!;

    private StatsDto DisplayStats => Context.Stats ?? new();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        metadata = GymProjectMetadataParser.TryParse(Context.Project.Metadata);
        traineeDisplayName = BuildDisplayName();
        traineeInitials = BuildInitials();
        membershipChipLabel = metadata?.MembershipType;
        demographicsLine = BuildDemographicsLine();
        contactLine = BuildContactLine();
    }

    private async Task HandleClickedAsync()
    {
        if (!Context.OnClicked.HasDelegate)
        {
            return;
        }

        await Context.OnClicked.InvokeAsync(Context.Project.Id);
    }

    private string BuildDisplayName()
    {
        if (metadata is null)
        {
            return Context.Project.Name;
        }

        string[] nameParts =
        {
            metadata.FirstName?.Trim() ?? string.Empty,
            metadata.LastName?.Trim() ?? string.Empty
        };

        string value = string.Join(" ", nameParts.Where(static part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(value) ? Localizer["Gym trainee"] : value;
    }

    private string? BuildInitials()
    {
        if (metadata is null)
        {
            return null;
        }

        StringBuilder builder = new();
        if (!string.IsNullOrWhiteSpace(metadata.FirstName))
        {
            _ = builder.Append(char.ToUpperInvariant(metadata.FirstName![0]));
        }

        if (!string.IsNullOrWhiteSpace(metadata.LastName))
        {
            _ = builder.Append(char.ToUpperInvariant(metadata.LastName![0]));
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private string? BuildDemographicsLine()
    {
        if (metadata is null)
        {
            return null;
        }

        string? dateOfBirthText = FormatDate(metadata.DateOfBirth);
        string? genderText = FormatGender(metadata.Gender);

        return string.IsNullOrWhiteSpace(dateOfBirthText) && string.IsNullOrWhiteSpace(genderText)
            ? null
            : string.IsNullOrWhiteSpace(dateOfBirthText)
            ? (string)Localizer["GenderOnlyLabel", genderText!]
            : string.IsNullOrWhiteSpace(genderText)
            ? (string)Localizer["BirthDateLabel", dateOfBirthText!]
            : (string)Localizer["BirthAndGenderLabel", dateOfBirthText!, genderText!];
    }

    private string? BuildContactLine() => metadata is null
            ? null
            : string.IsNullOrWhiteSpace(metadata.Email) && string.IsNullOrWhiteSpace(metadata.Phone)
            ? null
            : (string)Localizer["ContactSummaryLabel", FormatValue(metadata.Email), FormatValue(metadata.Phone)];

    private string FormatValue(string? value) => string.IsNullOrWhiteSpace(value) ? Localizer["NotProvided"] : value!;

    private string? FormatDate(string? value) => string.IsNullOrWhiteSpace(value)
            ? null
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed)
            ? parsed.ToString("d", CultureInfo.CurrentCulture)
            : value;

    private string? FormatGender(string? gender) => string.IsNullOrWhiteSpace(gender) ? null : (string)Localizer[gender!];
}
