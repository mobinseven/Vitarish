namespace Vitarish.Clinic.Interface.Components.Patients;

public partial class PatientEditForm
{
    [Parameter] public ProjectWriteDto? InitialProject { get; set; }

    private PatientWriteDto projectWriteDto = new();

    private Vitarish.Interfaces.Modules.Components.StackFileInput? stackFileInput;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Project is not null)
        {
            projectWriteDto = Project.ToPatientWrite()!;
        }
        else if (InitialProject is PatientWriteDto initialPatient)
        {
            projectWriteDto = initialPatient;
        }
        else if (InitialProject is not null)
        {
            projectWriteDto = new PatientWriteDto
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

        if (Project is not null && string.IsNullOrWhiteSpace(projectWriteDto.LastName))
        {
            projectWriteDto.LastName = Project.Name;
        }
        StateHasChanged();
    }

    private async Task Proceed()
    {
        //if(stackFileInput is not null && stackFileInput.HasPendingFiles)
        //{
        //   await stackFileInput.Upload(); // Then wait for AvatarUploaded to be called.
        //}
        //else
        //{
        //    await SaveAsync();
        //}
    }

    private async Task AvatarUploaded(Guid? fileId)
    {
        projectWriteDto.AvatarFileId = fileId;
        await SaveAsync();
    }

    protected override async Task SaveAsync()
    {
        projectWriteDto.Name = $"{projectWriteDto.FirstName} {projectWriteDto.LastName}".TrimEnd(' ', ',');
        if (string.IsNullOrWhiteSpace(projectWriteDto.Name))
        {
            _ = await DialogService.ShowMessageBoxAsync(null, Localizer["RequiredItem", Localizer["LastName"]]);
            return;
        }
        if (Project is null)
        {
            ProjectReadDto created = await ProjectsService.AddAsync(projectWriteDto, CancellationToken.None).EnsureValueAsync();
            await OnCreate.InvokeAsync(created.Id);
        }
        else
        {
            _ = await ProjectsService.UpdateAsync(projectWriteDto, CancellationToken.None);
            await OnUpdate.InvokeAsync(Project.Id);
        }
    }
}
