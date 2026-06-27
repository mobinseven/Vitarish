namespace Vitarish.Clinic.Interface.Components.Patients;

using MudBlazor;
using Vitarish.Files.Interface.Components;
public partial class PatientEditForm
{
    [Parameter] public ProjectWriteDto? InitialProject { get; set; }

    private PatientWriteDto patient = new();

    private StackFileImageInput? stackFileInput;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Patient is not null)
        {
            patient = Project.ToPatientWrite()!;
        }
        else if (InitialProject is PatientWriteDto initialPatient)
        {
            patient = initialPatient;
        }
        else if (InitialProject is not null)
        {
            patient = new PatientWriteDto
            {
                Id = InitialProject.Id,
                Name = InitialProject.Name,
                Description = InitialProject.Description,
                AvatarFileId = InitialProject.AvatarFileId,
                IdentificationCode = InitialProject.IdentificationCode,
                Template = InitialProject.Template,
                UserIds = InitialProject.UserIds,
                Metadata = InitialProject.Metadata,
                MetadataValues = InitialProject.MetadataValues
            };
        }

        if (Project is not null && string.IsNullOrWhiteSpace(patient.LastName))
        {
            patient.LastName = Project.Name;
        }
        StateHasChanged();
    }

    private async Task Proceed()
    {
        if (stackFileInput is not null && stackFileInput.HasPendingFiles)
        {
            await stackFileInput.Upload(); // Then wait for AvatarUploaded to be called.
        }
        else
        {
            await SaveAsync();
        }
    }

    private async Task AvatarUploaded(Guid? fileId)
    {
        patient.AvatarFileId = fileId;
        await SaveAsync();
    }

    protected override async Task SaveAsync()
    {
        patient.Name = $"{patient.FirstName} {patient.LastName}".TrimEnd(' ', ',');
        if (Project is null)
        {
            ProjectReadDto created = await SeaClient.CreateProjectAsync(patient, CancellationToken.None).EnsureValueAsync();
            await OnCreate.InvokeAsync(created.Id);
        }
        else
        {
            _ = await SeaClient.UpdateProjectAsync(patient, CancellationToken.None);
            await OnUpdate.InvokeAsync(Project.Id);
        }
    }


    private static readonly string[] ReferrerItems =
    [
        "Site",
        "Instagram"
    ];

    private static bool HasValue(string? value)
        => !string.IsNullOrWhiteSpace(value);

    private static string RequiredIcon(string? value, string validIcon = "")
        => HasValue(value) ? validIcon : "fa-solid fa-triangle-exclamation";

    private static Color RequiredColor(string? value)
        => HasValue(value) ? Color.Default : Color.Warning;

    private Task CancelAsync()
        => OnCancel.InvokeAsync();
}
