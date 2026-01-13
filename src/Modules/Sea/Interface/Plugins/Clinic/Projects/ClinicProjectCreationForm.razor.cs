namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

public partial class ClinicProjectCreationForm
{
    internal const string GenderFemale = "Female";
    internal const string GenderMale = "Male";
    internal const string GenderUnspecified = "Unspecified";

    private const string FacilityIdMetadataKey = "FacilityId";
    private const string FacilityNameMetadataKey = "FacilityName";
    private const string DepartmentNameMetadataKey = "DepartmentName";

    private const string MetadataJsonSchema = """
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "anyOf": [
    { "required": ["firstName"] },
    { "required": ["lastName"] }
  ],
  "properties": {
    "firstName": { "type": "string", "maxLength": 128 },
    "lastName": { "type": "string", "maxLength": 128 },
    "dateOfBirth": { "type": ["string", "null"], "format": "date" },
    "gender": { "type": "string", "enum": ["Female", "Male", "Unspecified"] },
    "email": { "type": "string", "format": "email", "maxLength": 256 },
    "phone": { "type": ["string", "null"], "maxLength": 64 },
    "insuranceProvider": { "type": "string", "maxLength": 128 },
    "policyNumber": { "type": "string", "maxLength": 64 },
    "primaryPhysician": { "type": ["string", "null"], "maxLength": 128 },
    "visitReason": { "type": "string", "maxLength": 256 },
    "preferredPharmacy": { "type": ["string", "null"], "maxLength": 128 },
    "allergies": { "type": ["string", "null"], "maxLength": 512 },
    "currentMedications": { "type": ["string", "null"], "maxLength": 512 },
    "clinicalNotes": { "type": ["string", "null"], "maxLength": 512 },
    "emergencyContactName": { "type": ["string", "null"], "maxLength": 128 },
    "emergencyContactPhone": { "type": ["string", "null"], "maxLength": 64 },
    "consentToShareRecords": { "type": ["boolean", "null"] },
    "facilityId": { "type": ["string", "null"], "format": "uuid" },
    "departmentName": { "type": ["string", "null"], "maxLength": 256 },
    "templateId": { "type": ["string", "null"], "format": "uuid" }
  },
  "additionalProperties": false
}
""";
    private const string DefaultInstructions = "You are a clinical intake assistant. Always reply with a JSON object that matches the provided schema. Populate every required field using the supplied patient description. Use ISO-8601 format (yyyy-MM-dd) for dateOfBirth and limit gender to Female, Male (if it wasn't specified guess it from patient's first name). When information is not available, return null for optional fields and default consentToShareRecords to true. Do not include Markdown or commentary.";
    private string Instructions { get; set; } = BuildInstructions(chatInstructions: null, clinicInstructions: null);
    private AiJsonGeneratorInput? generator;
    private bool isSubmitting;
    private bool isGenerating;
    private readonly bool inputBusy;
    private bool metadataHasPendingChanges;
    private bool isManualMode;
    private bool manualIsValid;
    private Guid? facilityId;
    private string? facilityLabel;
    private string? departmentName;
    private string? departmentLabel;
    private string? errorMessage;
    private string? generationError;
    private string? metadataValidationError;
    private string? manualValidationError;
    private int activeStep;
    private string patientDescription = string.Empty;

    [Inject]
    private IUsageConfigurationService UsageConfigurationService { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    [Parameter]
    public required ProjectCreationFormContext Context { get; set; }
        = default!;

    private string finishLabel => isSubmitting
        ? Localizer["Finishing"]
        : Localizer["Finish"];

    private string generateLabel => isGenerating
        ? Localizer["Generating"]
        : Localizer["GeneratePatientData"];

    private bool DescriptionProvided => !string.IsNullOrWhiteSpace(patientDescription);

    private bool MetadataAvailable => Metadata is not null;

    private bool CanBeginGeneration => !isGenerating && DescriptionProvided && !inputBusy;

    private bool CanOpenReview => MetadataAvailable && !isGenerating;

    private bool CanApplyMetadata => metadataHasPendingChanges && !isGenerating;

    private bool CanContinueToFinish => MetadataAvailable && !metadataHasPendingChanges && string.IsNullOrWhiteSpace(metadataValidationError);

    private bool CanSubmitAi => CanContinueToFinish && !isSubmitting;

    private bool CanSubmitManual => manualIsValid && !isSubmitting;

    private bool CanSubmit => isManualMode ? CanSubmitManual : CanSubmitAi;

    private IReadOnlyList<MetadataTableRow> MetadataTableRows => Metadata is null
        ? Array.Empty<MetadataTableRow>()
        : BuildMetadataTableRows(Metadata);

    protected override void OnInitialized()
    {
        base.OnInitialized();

        InitializeEditableMetadata(new ClinicProjectMetadataModel(OnEditableMetadataChanged));
        ApplyContextMetadata();
        ValidateManual();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Instructions = await LoadInstructionsAsync();
    }

    private void ApplyContextMetadata()
    {
        if (Context.TryGetMetadata<Guid>(FacilityIdMetadataKey, out Guid facility))
        {
            facilityId = facility;
        }

        facilityLabel = Context.TryGetMetadata<string>(FacilityNameMetadataKey, out string? facilityName)
            ? (string)Localizer["FacilityWithName", facilityName!]
            : null;

        if (Context.TryGetMetadata<string>(DepartmentNameMetadataKey, out string? department))
        {
            departmentName = department;
            departmentLabel = Localizer["DepartmentWithName", department!];
        }
        else
        {
            departmentName = null;
            departmentLabel = null;
        }
    }

    private Task HandleAiBusyAsync(bool isBusy)
    {
        isGenerating = isBusy;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleAiErrorAsync(string? error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            generationError = error;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private Task HandleAiJsonAsync(AiJsonGeneratorInput.AiJsonGeneratedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Json))
        {
            if (args.IsFinal)
            {
                generationError = Localizer["AiNoOutput"];
                SetMetadataJson(string.Empty, markAsClean: true);
                ClearMetadata();
                EditableMetadata.Clear();
                StateHasChanged();
            }

            return Task.CompletedTask;
        }

        if (args.IsFinal)
        {
            if (TryDeserializeMetadata(args.Json, out ClinicProjectMetadata? metadata, out string? error))
            {
                SetMetadata(metadata, markAsClean: true);
                EditableMetadata.LoadFrom(metadata!);
                generationError = null;
                metadataValidationError = null;
                metadataHasPendingChanges = false;
                activeStep = 2;
            }
            else
            {
                generationError = error;
                SetMetadataJson(args.Json, markAsClean: false);
                ClearMetadata();
                EditableMetadata.Clear();
            }
        }
        else
        {
            SetMetadataJson(args.Json, markAsClean: false);
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task ApplyAndProceedAsync()
    {
        metadataValidationError = null;

        if (!TryCreateMetadataFromEditor(out ClinicProjectMetadata? parsed, out string? error))
        {
            metadataValidationError = error;
            ClearMetadata();
            StateHasChanged();
            return Task.CompletedTask;
        }

        SetMetadata(parsed, markAsClean: true);
        generationError = null;
        metadataHasPendingChanges = false;
        activeStep = 3;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task SubmitAsync()
    {
        if (!CanSubmitAi)
        {
            return;
        }

        await SubmitMetadataAsync();
    }

    private async Task SubmitManualAsync()
    {
        if (!CanSubmitManual)
        {
            return;
        }

        manualValidationError = null;

        if (!TryCreateMetadataFromEditor(out ClinicProjectMetadata? parsed, out string? validationError))
        {
            manualValidationError = validationError;
            StateHasChanged();
            return;
        }

        SetMetadata(parsed, markAsClean: true);
        metadataHasPendingChanges = false;
        await SubmitMetadataAsync();
    }

    private async Task SubmitMetadataAsync()
    {
        errorMessage = null;
        isSubmitting = true;
        StateHasChanged();

        try
        {
            if (!TryDeserializeMetadata(MetadataJson, out ClinicProjectMetadata? parsed, out string? validationError))
            {
                metadataValidationError = validationError;
                errorMessage = validationError;
                if (isManualMode)
                {
                    manualValidationError = validationError;
                }
                else
                {
                    activeStep = 2;
                }
                StateHasChanged();
                return;
            }

            ClinicProjectMetadata normalized = parsed!;
            SetMetadata(normalized, markAsClean: true);

            string projectName = BuildDisplayName(normalized);
            string description = BuildDescription(normalized);
            string metadata = SerializeMetadata(normalized);

            _ = await ProjectsService.AddAsync(projectName, description, template: false, metadata, CancellationToken.None);
            Dialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            StateHasChanged();
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private void GoToPreviousStep()
    {
        if (activeStep > 0 && !isSubmitting && !isGenerating)
        {
            activeStep--;
            StateHasChanged();
        }
    }

    private void ToggleManualMode()
    {
        if (isSubmitting || isGenerating)
        {
            return;
        }

        isManualMode = !isManualMode;

        if (isManualMode)
        {
            manualValidationError = null;
            ValidateManual();
        }
        else
        {
            metadataValidationError = null;
        }

        StateHasChanged();
    }

    private void Cancel() => Dialog.Cancel();

    private async Task<string> LoadInstructionsAsync()
    {
        try
        {
            UsageConfigurationDto? usageConfiguration = await UsageConfigurationService.GetUsageConfigurationAsync(CancellationToken.None);
            UsageConfigurationMetadata metadata = UsageConfigurationMetadata.FromJson(usageConfiguration?.Metadata);
            return BuildInstructions(metadata.ChatInstructions, metadata.ClinicPatientIntakeInstructions);
        }
        catch
        {
            return BuildInstructions(chatInstructions: null, clinicInstructions: null);
        }
    }

    private static string BuildInstructions(string? chatInstructions, string? clinicInstructions)
    {
        List<string> segments = new(3)
        {
            DefaultInstructions
        };

        if (!string.IsNullOrWhiteSpace(chatInstructions))
        {
            segments.Add(chatInstructions.Trim());
        }

        if (!string.IsNullOrWhiteSpace(clinicInstructions))
        {
            segments.Add(clinicInstructions.Trim());
        }

        return string.Join(Environment.NewLine + Environment.NewLine, segments);
    }

    protected override void OnMetadataJsonChanged(bool markAsClean)
    {
        metadataValidationError = null;
        metadataHasPendingChanges = !markAsClean;
    }

    protected override string BuildEmptyMetadataErrorMessage() => Localizer["MetadataEmpty"];

    protected override string BuildDeserializationErrorMessage(Exception exception) => Localizer["AiResponseParse", exception.Message];

    private bool TryCreateMetadataFromEditor(out ClinicProjectMetadata? metadata, out string? error)
    {
        metadata = null;
        error = null;

        if (string.IsNullOrWhiteSpace(EditableMetadata.FirstName) && string.IsNullOrWhiteSpace(EditableMetadata.LastName))
        {
            error = Localizer["NameRequired"];
            return false;
        }
        
        ClinicProjectMetadata normalized = new()
        {
            FirstName = NormalizeInput(EditableMetadata.FirstName),
            LastName = NormalizeInput(EditableMetadata.LastName),
            DateOfBirth = NormalizeInput(EditableMetadata.DateOfBirth),
            Gender = EditableMetadata.Gender,
            Email = NormalizeInput(EditableMetadata.Email),
            Phone = NormalizeInput(EditableMetadata.Phone),
            InsuranceProvider = NormalizeInput(EditableMetadata.InsuranceProvider),
            PolicyNumber = NormalizeInput(EditableMetadata.PolicyNumber),
            PrimaryPhysician = NormalizeInput(EditableMetadata.PrimaryPhysician),
            VisitReason = NormalizeInput(EditableMetadata.VisitReason),
            PreferredPharmacy = NormalizeInput(EditableMetadata.PreferredPharmacy),
            Allergies = NormalizeInput(EditableMetadata.Allergies),
            CurrentMedications = NormalizeInput(EditableMetadata.CurrentMedications),
            ClinicalNotes = NormalizeInput(EditableMetadata.ClinicalNotes),
            EmergencyContactName = NormalizeInput(EditableMetadata.EmergencyContactName),
            EmergencyContactPhone = NormalizeInput(EditableMetadata.EmergencyContactPhone),
            ConsentToShareRecords = EditableMetadata.ConsentToShareRecords,
            DepartmentName = NormalizeInput(EditableMetadata.DepartmentName)
        };

        metadata = NormalizeMetadata(normalized);
        return true;
    }

    private void OnEditableMetadataChanged()
    {
        metadataHasPendingChanges = true;
        metadataValidationError = null;

        if (isManualMode)
        {
            ValidateManual();
        }

        _ = InvokeAsync(StateHasChanged);
    }

    private void ValidateManual()
    {
        manualValidationError = null;
        manualIsValid = TryCreateMetadataFromEditor(out _, out string? error);

        if (!manualIsValid)
        {
            manualValidationError = error;
        }
    }

    private IReadOnlyList<MetadataTableRow> BuildMetadataTableRows(ClinicProjectMetadata metadata)
    {
        IReadOnlyList<MetadataSection> sections = BuildMetadataSections(metadata);
        List<MetadataTableRow> rows = [];

        foreach (MetadataSection section in sections)
        {
            foreach (MetadataField field in section.Fields)
            {
                rows.Add(new MetadataTableRow(section.Title, field.Label, field.Value));
            }
        }

        return rows;
    }

    protected override ClinicProjectMetadata NormalizeMetadata(ClinicProjectMetadata metadata)
    {

        string? dateOfBirth = ParseDate(metadata.DateOfBirth);

        return metadata with
        {
            Gender = metadata.Gender,
            DateOfBirth = dateOfBirth,
            FacilityId = facilityId ?? metadata.FacilityId,
            DepartmentName = departmentName ?? metadata.DepartmentName,
            TemplateId = Context.TemplateId ?? metadata.TemplateId,
            ConsentToShareRecords = metadata.ConsentToShareRecords ?? true
        };
    }

    private static string? ParseDate(string? value) => string.IsNullOrWhiteSpace(value)
            ? null
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : value;

    private string BuildDescription(ClinicProjectMetadata metadata) => string.IsNullOrWhiteSpace(metadata.ClinicalNotes)
                ? Localizer["PatientDescription"]
                : metadata.ClinicalNotes!; // TODO: build a meaningful description based on all data not just clinical notes

    private string BuildDisplayName(ClinicProjectMetadata metadata)
    {
        IEnumerable<string> parts = new[] { metadata.FirstName, metadata.LastName }
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim());

        string displayName = string.Join(" ", parts);
        return string.IsNullOrWhiteSpace(displayName) ? Localizer["Patient"] : displayName;
    }

    private IReadOnlyList<MetadataSection> BuildMetadataSections(ClinicProjectMetadata metadata)
    {
        List<MetadataSection> sections = [];

        void AddSection(string title, List<MetadataField> fields)
        {
            if (fields.Count > 0)
            {
                sections.Add(new MetadataSection(title, fields));
            }
        }

        List<MetadataField> demographics = [];
        AddField(demographics, Localizer["FirstName"], metadata.FirstName);
        AddField(demographics, Localizer["LastName"], metadata.LastName);
        AddField(demographics, Localizer["DateOfBirth"], FormatDateForDisplay(metadata.DateOfBirth));
        AddField(demographics, Localizer["Gender"], FormatGender(metadata.Gender));
        AddSection(Localizer["Demographics"], demographics);

        List<MetadataField> contact = [];
        AddField(contact, Localizer["Email"], metadata.Email);
        AddField(contact, Localizer["Phone"], metadata.Phone);
        AddSection(Localizer["Contact"], contact);

        List<MetadataField> insurance = [];
        AddField(insurance, Localizer["InsuranceProvider"], metadata.InsuranceProvider);
        AddField(insurance, Localizer["PolicyNumber"], metadata.PolicyNumber);
        AddField(insurance, Localizer["PrimaryPhysician"], metadata.PrimaryPhysician);
        AddSection(Localizer["Insurance"], insurance);

        List<MetadataField> care = [];
        AddField(care, Localizer["VisitReason"], metadata.VisitReason);
        AddField(care, Localizer["PreferredPharmacy"], metadata.PreferredPharmacy);
        AddField(care, Localizer["Allergies"], metadata.Allergies);
        AddField(care, Localizer["CurrentMedications"], metadata.CurrentMedications);
        AddSection(Localizer["CareDetails"], care);

        List<MetadataField> emergency = [];
        AddField(emergency, Localizer["EmergencyContactName"], metadata.EmergencyContactName);
        AddField(emergency, Localizer["EmergencyContactPhone"], metadata.EmergencyContactPhone);
        AddSection(Localizer["EmergencyContact"], emergency);

        List<MetadataField> administrative = [];
        AddField(administrative, Localizer["ConsentToShareRecords"], FormatBoolean(metadata.ConsentToShareRecords));
        AddField(administrative, Localizer["Department"], metadata.DepartmentName);
        AddSection(Localizer["Administrative"], administrative);

        return sections;
    }

    private static void AddField(List<MetadataField> fields, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields.Add(new MetadataField(label, value));
        }
    }

    private string? FormatBoolean(bool? value) => value is null ? null : (string)(value.Value ? Localizer["Yes"] : Localizer["No"]);

    private static string? FormatDateForDisplay(string? value) => string.IsNullOrWhiteSpace(value)
            ? null
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed)
            ? parsed.ToString("y", CultureInfo.CurrentCulture)
            : value;
    private sealed record MetadataSection(string Title, IReadOnlyList<MetadataField> Fields);
    private sealed record MetadataField(string Label, string Value);
    private sealed record MetadataTableRow(string Section, string Label, string Value);
}
