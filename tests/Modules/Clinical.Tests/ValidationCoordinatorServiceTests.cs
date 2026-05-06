namespace Clinical.Tests;

public class ValidationCoordinatorServiceTests
{
    private readonly ValidationCoordinatorService _coordinator;

    public ValidationCoordinatorServiceTests()
    {
        Mock<IConflictHandler> conflictHandler = new();
        conflictHandler
            .Setup(h => h.IsDuplicateActionAsync(It.IsAny<Guid>(), It.IsAny<ClinicalActionType>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _coordinator = new ValidationCoordinatorService(conflictHandler.Object);
    }

    [Fact]
    public async Task Validate_RegisterPatient_RequiresFamilyName()
    {
        WorkflowPolicyDefinition policy = WorkflowPolicyDefinition.CreateDefault();
        PatientRegistrationPayload payload = new()
        {
            FamilyName = string.Empty,
            GivenName = "John",
            BirthDate = new DateOnly(1990, 1, 1)
        };

        IReadOnlyList<WorkflowValidationResult> errors = await _coordinator.ValidateAsync(
            ClinicalActionType.RegisterPatient,
            payload,
            policy,
            string.Empty,
            null);

        errors.Should().ContainSingle(e => e.Code == "REQUIRED_FIELD" && e.Field == "FamilyName");
    }

    [Fact]
    public async Task Validate_RecordVitals_RequiresAtLeastOneVital()
    {
        WorkflowPolicyDefinition policy = WorkflowPolicyDefinition.CreateDefault();
        VitalsPayload payload = new();

        IReadOnlyList<WorkflowValidationResult> errors = await _coordinator.ValidateAsync(
            ClinicalActionType.RecordVitals,
            payload,
            policy,
            "Patient/p1",
            null);

        errors.Should().ContainSingle(e => e.Code == "NO_VITALS");
    }

    [Fact]
    public async Task Validate_RecordVitals_RequiresBothBloodPressureComponents()
    {
        WorkflowPolicyDefinition policy = WorkflowPolicyDefinition.CreateDefault();
        VitalsPayload payload = new() { SystolicBp = 120 }; // Missing diastolic

        IReadOnlyList<WorkflowValidationResult> errors = await _coordinator.ValidateAsync(
            ClinicalActionType.RecordVitals,
            payload,
            policy,
            "Patient/p1",
            null);

        errors.Should().ContainSingle(e => e.Code == "BP_INCOMPLETE");
    }

    [Fact]
    public async Task Validate_AddDiagnosis_RequiresCode()
    {
        WorkflowPolicyDefinition policy = WorkflowPolicyDefinition.CreateDefault();
        DiagnosisPayload payload = new()
        {
            Code = string.Empty,
            CodeSystem = "http://hl7.org/fhir/sid/icd-10"
        };

        IReadOnlyList<WorkflowValidationResult> errors = await _coordinator.ValidateAsync(
            ClinicalActionType.AddDiagnosis,
            payload,
            policy,
            "Patient/p1",
            "Encounter/e1");

        errors.Should().ContainSingle(e => e.Code == "REQUIRED_FIELD" && e.Field == "Code");
    }

    [Fact]
    public async Task Validate_AddNote_RequiresContent()
    {
        WorkflowPolicyDefinition policy = WorkflowPolicyDefinition.CreateDefault();
        ClinicalNotePayload payload = new()
        {
            NoteType = "11488-4",
            Content = string.Empty
        };

        IReadOnlyList<WorkflowValidationResult> errors = await _coordinator.ValidateAsync(
            ClinicalActionType.AddClinicalNote,
            payload,
            policy,
            "Patient/p1",
            null);

        errors.Should().ContainSingle(e => e.Code == "REQUIRED_FIELD" && e.Field == "Content");
    }
}
