using Vitarish.Sea.Interface.Plugins.Base.Enums;

namespace Vitarish.Sea.Interface.Plugins.Gym.Projects;

public partial class GymProjectCreationForm : ProjectsBase
{
    private const string FacilityIdMetadataKey = "FacilityId";
    private const string FacilityNameMetadataKey = "FacilityName";
    private const string GenderFemale = "Female";
    private const string GenderMale = "Male";
    private const string GenderUnassigned = "Unassigned";

    private const string MetadataJsonSchema = """
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": [
    "firstName",
    "lastName",
    "gender",
    "emergencyContactName",
    "emergencyContactPhone"
  ],
  "properties": {
    "firstName": { "type": "string", "maxLength": 128 },
    "lastName": { "type": "string", "maxLength": 128 },
    "dateOfBirth": { "type": ["string", "null"], "format": "date" },
    "gender": { "type": "string", "enum": ["Female", "Male", "Unassigned"] },
    "nationality": { "type": ["string", "null"], "maxLength": 128 },
    "email": { "type": ["string", "null"], "format": "email", "maxLength": 256 },
    "phone": { "type": ["string", "null"], "maxLength": 64 },
    "membershipType": { "type": ["string", "null"], "maxLength": 64 },
    "preferredProgram": { "type": ["string", "null"], "maxLength": 128 },
    "goals": { "type": ["string", "null"], "maxLength": 256 },
    "emergencyContactName": { "type": "string", "maxLength": 128 },
    "emergencyContactPhone": { "type": "string", "maxLength": 64 },
    "medicalNotes": { "type": ["string", "null"], "maxLength": 512 },
    "facilityId": { "type": ["string", "null"], "format": "uuid" },
    "templateId": { "type": ["string", "null"], "format": "uuid" }
  },
  "additionalProperties": false
}
""";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ResponderOptions responderOptions;

    private AiStreamResponder? responder;
    private Guid? facilityId;
    private string? facilityName;
    private string? facilityLabel;
    private bool isGenerating;
    private bool isSubmitting;
    private bool inputBusy;
    private int activeStep;
    private string traineeDescription = string.Empty;
    private string metadataJson = string.Empty;
    private GymProjectMetadata? traineeMetadata;
    private readonly GymTraineeMetadataModel editableMetadata;
    private string? generationError;
    private string? metadataValidationError;
    private string? errorMessage;
    private bool suppressMetadataChangeNotification;
    private bool metadataHasPendingChanges;

    public GymProjectCreationForm()
    {
        responderOptions = new ResponderOptions
        {
            TextFormat = ResponseTextFormat.JsonObject,
            Instructions = $"""
You are a fitness intake assistant. Always reply with a JSON object matching the provided schema. Populate every required field using the trainee description. Use ISO-8601 format (yyyy-MM-dd) for dateOfBirth and limit gender to Female, Male, or Unassigned. When information is unavailable, return null for optional fields. Do not include Markdown or commentary.

Schema:
{MetadataJsonSchema}
"""
        };

        editableMetadata = new GymTraineeMetadataModel(OnEditableMetadataChanged);
    }

    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;

    [Parameter]
    public required ProjectCreationFormContext Context { get; set; }

    private string generateLabel => isGenerating
        ? Localizer["Generating..."]
        : Localizer["Generate trainee profile"];

    private string finishLabel => isSubmitting
        ? Localizer["Finishing..."]
        : Localizer["Create trainee project"];

    private bool DescriptionProvided => !string.IsNullOrWhiteSpace(traineeDescription);

    private bool MetadataAvailable => traineeMetadata is not null;

    private bool HasRequiredFields => !string.IsNullOrWhiteSpace(editableMetadata.FirstName)
        && !string.IsNullOrWhiteSpace(editableMetadata.LastName)
        && !string.IsNullOrWhiteSpace(editableMetadata.EmergencyContactName)
        && !string.IsNullOrWhiteSpace(editableMetadata.EmergencyContactPhone);

    private bool CanBeginGeneration => !isGenerating && DescriptionProvided && !inputBusy;

    private bool CanOpenReview => MetadataAvailable && !isGenerating;

    private bool CanSubmit => MetadataAvailable && HasRequiredFields && string.IsNullOrWhiteSpace(metadataValidationError) && !isSubmitting;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Context.TryGetMetadata<Guid>(FacilityIdMetadataKey, out Guid facility))
        {
            facilityId = facility;
        }

        if (Context.TryGetMetadata<string>(FacilityNameMetadataKey, out string? name))
        {
            facilityName = name;
            facilityLabel = Localizer["Facility: {0}", name!];
        }
        else
        {
            facilityName = null;
            facilityLabel = null;
        }
    }

    private async Task BeginGenerationAsync()
    {
        if (responder is null)
        {
            return;
        }

        if (!DescriptionProvided)
        {
            generationError = Localizer["Provide a trainee description before requesting the AI assistant."];
            StateHasChanged();
            return;
        }

        generationError = null;
        metadataValidationError = null;
        SetMetadataJson(string.Empty);
        traineeMetadata = null;
        ClearEditableMetadata();

        isGenerating = true;
        activeStep = 1;
        StateHasChanged();

        try
        {
            await responder.Submit([ResponderInput.TextInput(BuildPrompt(traineeDescription))]);
        }
        catch (Exception ex)
        {
            generationError = ex.Message;
        }
        finally
        {
            isGenerating = false;
            StateHasChanged();
        }
    }

    private Task HandleResponderOutputAsync(ResponderOutput output)
    {
        if (output.Type == ResponderOutputType.Text)
        {
            SetMetadataJson(output.Text);
        }

        return Task.CompletedTask;
    }

    private Task HandleResponderCompletedAsync(IEnumerable<ResponderOutput> outputs)
    {
        string text = outputs.LastOrDefault(o => o.Type == ResponderOutputType.Text)?.Text ?? metadataJson;

        if (string.IsNullOrWhiteSpace(text))
        {
            generationError = Localizer["The AI assistant did not return any JSON output."];
            SetMetadataJson(string.Empty);
            traineeMetadata = null;
            ClearEditableMetadata();
            StateHasChanged();
            return Task.CompletedTask;
        }

        if (TryDeserializeMetadata(text, out GymProjectMetadata? metadata, out string? error))
        {
            traineeMetadata = metadata;
            SetMetadataJson(JsonSerializer.Serialize(metadata, SerializerOptions));
            generationError = null;
            metadataValidationError = null;
            LoadEditableMetadata(metadata!);
            metadataHasPendingChanges = false; // JSON matches editor values
            activeStep = 2;
        }
        else
        {
            generationError = error;
            traineeMetadata = null;
            ClearEditableMetadata();
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    private bool TryDeserializeMetadata(string json, out GymProjectMetadata? metadata, out string? error)
    {
        metadata = null;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = Localizer["The metadata payload is empty."];
            return false;
        }

        try
        {
            GymProjectMetadata? parsed = JsonSerializer.Deserialize<GymProjectMetadata>(json, SerializerOptions);
            if (parsed is null)
            {
                error = Localizer["The metadata payload is empty."];
                return false;
            }

            metadata = NormalizeMetadata(parsed);
            return true;
        }
        catch (Exception ex)
        {
            error = Localizer["Unable to parse the AI response: {0}", ex.Message];
            return false;
        }
    }

    private GymProjectMetadata NormalizeMetadata(GymProjectMetadata metadata) => metadata with
    {
        FirstName = NormalizeInput(metadata.FirstName),
        LastName = NormalizeInput(metadata.LastName),
        DateOfBirth = NormalizeDate(metadata.DateOfBirth),
        Gender = metadata.Gender,
        Nationality = NormalizeInput(metadata.Nationality),
        Email = NormalizeInput(metadata.Email),
        Phone = NormalizeInput(metadata.Phone),
        MembershipType = NormalizeInput(metadata.MembershipType),
        PreferredProgram = NormalizeInput(metadata.PreferredProgram),
        Goals = NormalizeInput(metadata.Goals),
        EmergencyContactName = NormalizeInput(metadata.EmergencyContactName),
        EmergencyContactPhone = NormalizeInput(metadata.EmergencyContactPhone),
        MedicalNotes = NormalizeInput(metadata.MedicalNotes),
        FacilityId = metadata.FacilityId ?? facilityId,
        TemplateId = metadata.TemplateId ?? Context.TemplateId
    };

    private static string? NormalizeInput(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeDate(string? value) => string.IsNullOrWhiteSpace(value)
            ? null
            : DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exact)
            ? exact.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : value.Trim();

    private static string NormalizeGender(string? value) => value?.Trim() switch
    {
        GenderFemale => GenderFemale,
        GenderMale => GenderMale,
        _ => GenderUnassigned
    };

    private void SetMetadataJson(string? value) => metadataJson = value ?? string.Empty;

    private void LoadEditableMetadata(GymProjectMetadata metadata)
    {
        suppressMetadataChangeNotification = true;
        editableMetadata.LoadFrom(metadata);
        suppressMetadataChangeNotification = false;
        UpdateMetadataPreview();
        metadataHasPendingChanges = false; // JSON matches editor values
    }

    private void ClearEditableMetadata()
    {
        suppressMetadataChangeNotification = true;
        editableMetadata.Clear();
        suppressMetadataChangeNotification = false;
        UpdateMetadataPreview();
        metadataHasPendingChanges = false; // cleared state is clean
    }

    private void OnEditableMetadataChanged()
    {
        if (suppressMetadataChangeNotification)
        {
            return;
        }

        metadataValidationError = null;
        errorMessage = null;
        UpdateMetadataPreview();
        metadataHasPendingChanges = true; // any manual edit marks as dirty
        StateHasChanged();
    }

    private void UpdateMetadataPreview()
    {
        if (!MetadataAvailable)
        {
            return;
        }

        GymProjectMetadata preview = editableMetadata.ToMetadata(facilityId, Context.TemplateId);
        metadataJson = JsonSerializer.Serialize(preview, SerializerOptions);
    }

    // NEW: parity method to apply edits and go to Finish step (3)
    private async Task ApplyAndProceedAsync()
    {
        metadataValidationError = null;

        if (!TryCreateMetadataFromEditor(out GymProjectMetadata? parsed, out string? error))
        {
            metadataValidationError = error;
            traineeMetadata = null;
            StateHasChanged();
            return;
        }

        traineeMetadata = parsed;
        SetMetadataJson(JsonSerializer.Serialize(parsed, SerializerOptions));
        generationError = null;
        metadataHasPendingChanges = false;
        activeStep = 3; // Move to Finish step
        StateHasChanged();
    }

    private bool TryCreateMetadataFromEditor(out GymProjectMetadata metadata, out string? error)
    {
        metadata = editableMetadata.ToMetadata(facilityId, Context.TemplateId);
        error = null;

        (string Label, string? Value)[] required = new (string Label, string? Value)[]
        {
            (Localizer["FirstName"], metadata.FirstName),
            (Localizer["LastName"], metadata.LastName),
            (Localizer["EmergencyContactName"], metadata.EmergencyContactName),
            (Localizer["EmergencyContactPhone"], metadata.EmergencyContactPhone)
        };

        foreach ((string label, string value) in required)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = Localizer["{0} is required.", label];
                return false;
            }
        }

        metadata = NormalizeMetadata(metadata);
        return true;
    }

    private async Task SubmitAsync()
    {
        if (!CanSubmit)
        {
            return;
        }

        metadataValidationError = null;
        errorMessage = null;

        if (!TryCreateMetadataFromEditor(out GymProjectMetadata? metadata, out string? validationError))
        {
            metadataValidationError = validationError;
            StateHasChanged();
            return;
        }

        isSubmitting = true;
        StateHasChanged();

        try
        {
            GymProjectMetadata normalized = metadata!;
            traineeMetadata = normalized;
            string projectName = BuildProjectName(normalized);
            string description = BuildDescription(normalized);
            string payload = JsonSerializer.Serialize(normalized, SerializerOptions);
            metadataJson = payload;

            _ = await ProjectsService.AddAsync(projectName, description, template: false, payload, CancellationToken.None);
            Dialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private string BuildProjectName(GymProjectMetadata metadata)
    {
        string?[] parts = new[] { metadata.FirstName, metadata.LastName }
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();

        return parts.Length == 0
            ? Localizer["Gym trainee"]
            : string.Join(" ", parts);
    }

    private string BuildDescription(GymProjectMetadata metadata) => string.IsNullOrWhiteSpace(metadata.Goals)
                ? Localizer["Personal training intake"]
                : metadata.Goals!;

    private string BuildPrompt(string description)
    {
        string facilitySegment = string.IsNullOrWhiteSpace(facilityName)
            ? string.Empty
            : $"The trainee will attend the facility named \"{facilityName}\".";

        return $"""
Extract the trainee's information from the following description and produce JSON that matches the schema.
{facilitySegment}

Description:
{description}
""";
    }

    private void GoToPreviousStep()
    {
        if (activeStep > 0 && !isGenerating && !isSubmitting)
        {
            activeStep--;
            StateHasChanged();
        }
    }

    private void Cancel() => Dialog.Cancel();

    private sealed class GymTraineeMetadataModel
    {
        private readonly Action onChanged;

        public GymTraineeMetadataModel(Action onChanged)
        {
            this.onChanged = onChanged;
        }

        public string FirstName
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string LastName
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public DateTime? DateOfBirth
        {
            get;
            set => Set(ref field, value);
        } = DateTime.Today.AddYears(-18);

        public Gender? Gender
        {
            get;
            set => Set(ref field, value);
        }

        public string Nationality
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string Email
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string Phone
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string MembershipType
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string PreferredProgram
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string Goals
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string EmergencyContactName
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string EmergencyContactPhone
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public string MedicalNotes
        {
            get;
            set => Set(ref field, value ?? string.Empty);
        } = string.Empty;

        public void LoadFrom(GymProjectMetadata metadata)
        {
            FirstName = metadata.FirstName ?? string.Empty;
            LastName = metadata.LastName ?? string.Empty;
            Gender = metadata.Gender;
            Nationality = metadata.Nationality ?? string.Empty;
            Email = metadata.Email ?? string.Empty;
            Phone = metadata.Phone ?? string.Empty;
            MembershipType = metadata.MembershipType ?? string.Empty;
            PreferredProgram = metadata.PreferredProgram ?? string.Empty;
            Goals = metadata.Goals ?? string.Empty;
            EmergencyContactName = metadata.EmergencyContactName ?? string.Empty;
            EmergencyContactPhone = metadata.EmergencyContactPhone ?? string.Empty;
            MedicalNotes = metadata.MedicalNotes ?? string.Empty;
            DateOfBirth = TryParseDate(metadata.DateOfBirth);
        }

        public void Clear()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Gender = default;
            Nationality = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            MembershipType = string.Empty;
            PreferredProgram = string.Empty;
            Goals = string.Empty;
            EmergencyContactName = string.Empty;
            EmergencyContactPhone = string.Empty;
            MedicalNotes = string.Empty;
            DateOfBirth = DateTime.Today.AddYears(-18);
        }

        public GymProjectMetadata ToMetadata(Guid? facilityId, Guid? templateId) => new()
        {
            FirstName = NormalizeInput(FirstName),
            LastName = NormalizeInput(LastName),
            DateOfBirth = DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Gender = Gender,
            Nationality = NormalizeInput(Nationality),
            Email = NormalizeInput(Email),
            Phone = NormalizeInput(Phone),
            MembershipType = NormalizeInput(MembershipType),
            PreferredProgram = NormalizeInput(PreferredProgram),
            Goals = NormalizeInput(Goals),
            EmergencyContactName = NormalizeInput(EmergencyContactName),
            EmergencyContactPhone = NormalizeInput(EmergencyContactPhone),
            MedicalNotes = NormalizeInput(MedicalNotes),
            FacilityId = facilityId,
            TemplateId = templateId
        };

        private void Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            onChanged();
        }

        private static DateTime? TryParseDate(string? value) => string.IsNullOrWhiteSpace(value)
                ? null
                : DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exact)
                ? exact
                : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed) ? parsed : null;
    }
}
