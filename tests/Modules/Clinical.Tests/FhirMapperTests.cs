using Hl7.Fhir.Serialization;
using Vitarish.Clinical.Infrastructure.Mapping;

namespace Clinical.Tests;

public class FhirMapperTests
{
    [Fact]
    public void FhirPatientMapper_ProducesValidJson_WithRequiredFields()
    {
        PatientRegistrationPayload payload = new()
        {
            FamilyName = "Smith",
            GivenName = "John",
            BirthDate = new DateOnly(1980, 5, 15),
            Gender = "male",
            Phone = "555-0100"
        };

        FhirMappedResource result = FhirPatientMapper.Map(payload);

        result.ResourceType.Should().Be("Patient");
        result.ResourceJson.Should().NotBeNullOrWhiteSpace();
        result.ResourceJson.Should().Contain("Smith");
        result.ResourceJson.Should().Contain("1980-05-15");
        result.ResourceJson.Should().Contain("male");
    }

    [Fact]
    public void FhirEncounterMapper_ProducesValidJson_WithSubjectAndClass()
    {
        StartEncounterPayload payload = new()
        {
            EncounterClass = "AMB",
            ReasonCode = "Routine checkup"
        };

        FhirMappedResource result = FhirEncounterMapper.Map(payload, "Patient/p-001");

        result.ResourceType.Should().Be("Encounter");
        result.ResourceJson.Should().Contain("Patient/p-001");
        result.ResourceJson.Should().Contain("AMB");
    }

    [Fact]
    public void FhirObservationMapper_ProducesBloodPressureObservation_WithLoincCodes()
    {
        VitalsPayload payload = new()
        {
            SystolicBp = 120,
            DiastolicBp = 80
        };

        IReadOnlyList<FhirMappedResource> results = FhirObservationMapper.Map(payload, "Patient/p-001", "Encounter/e-001");

        results.Should().ContainSingle();
        results[0].ResourceType.Should().Be("Observation");
        results[0].ResourceJson.Should().Contain("85354-9"); // BP panel LOINC
        results[0].ResourceJson.Should().Contain("8480-6"); // Systolic
        results[0].ResourceJson.Should().Contain("8462-4"); // Diastolic
    }

    [Fact]
    public void FhirObservationMapper_ProducesPulseObservation_WithLoincCode()
    {
        VitalsPayload payload = new()
        {
            PulseRate = 72
        };

        IReadOnlyList<FhirMappedResource> results = FhirObservationMapper.Map(payload, "Patient/p-001", null);

        results.Should().ContainSingle();
        results[0].ResourceJson.Should().Contain("8867-4"); // Heart rate LOINC
    }

    [Fact]
    public void FhirObservationMapper_ProducesMultipleObservations_WhenMultipleVitalsProvided()
    {
        VitalsPayload payload = new()
        {
            PulseRate = 72,
            Temperature = 37.0m,
            SpO2Percent = 98,
            WeightKg = 75,
            HeightCm = 180
        };

        IReadOnlyList<FhirMappedResource> results = FhirObservationMapper.Map(payload, "Patient/p-001", null);

        results.Should().HaveCount(5);
        results.All(r => r.ResourceType == "Observation").Should().BeTrue();
    }

    [Fact]
    public void FhirConditionMapper_ProducesValidJson_WithCodeAndSubject()
    {
        DiagnosisPayload payload = new()
        {
            Code = "J06.9",
            CodeSystem = "http://hl7.org/fhir/sid/icd-10",
            DisplayText = "Acute upper respiratory infection",
            ClinicalStatus = "active"
        };

        FhirMappedResource result = FhirConditionMapper.Map(payload, "Patient/p-001", "Encounter/e-001");

        result.ResourceType.Should().Be("Condition");
        result.ResourceJson.Should().Contain("J06.9");
        result.ResourceJson.Should().Contain("Patient/p-001");
    }

    [Fact]
    public void FhirMedicationRequestMapper_ProducesValidJson_WithRequiredFields()
    {
        MedicationRequestPayload payload = new()
        {
            MedicationCode = "1049502",
            MedicationCodeSystem = "http://www.nlm.nih.gov/research/umls/rxnorm",
            MedicationDisplay = "Amoxicillin 500 MG",
            DosageInstruction = "Take 1 tablet 3 times daily"
        };

        FhirMappedResource result = FhirMedicationRequestMapper.Map(
            payload, "Patient/p-001", "Encounter/e-001", "Practitioner/dr-001");

        result.ResourceType.Should().Be("MedicationRequest");
        result.ResourceJson.Should().Contain("1049502");
        result.ResourceJson.Should().Contain("Patient/p-001");
    }

    [Fact]
    public void FhirDocumentReferenceMapper_ProducesValidJson_WithContent()
    {
        ClinicalNotePayload payload = new()
        {
            NoteType = "11488-4",
            NoteTypeDisplay = "Consultation note",
            Content = "Patient presents with sore throat.",
            AuthorFhirReference = "Practitioner/dr-001"
        };

        FhirMappedResource result = FhirDocumentReferenceMapper.Map(payload, "Patient/p-001", "Encounter/e-001");

        result.ResourceType.Should().Be("DocumentReference");
        result.ResourceJson.Should().Contain("11488-4");
    }

    [Fact]
    public void FhirTransactionBundleBuilder_CreatesBundle_WithAllEntries()
    {
        FhirMappedResource obs1 = new() { ResourceType = "Observation", ResourceJson = BuildMinimalObservationJson() };
        FhirMappedResource obs2 = new() { ResourceType = "Observation", ResourceJson = BuildMinimalObservationJson() };

        FhirMappedResource bundle = FhirTransactionBundleBuilder.Build([obs1, obs2]);

        bundle.ResourceType.Should().Be("Bundle");
        bundle.ResourceJson.Should().Contain("\"type\":\"transaction\"");
    }

    private static string BuildMinimalObservationJson()
    {
        return """
               {
                 "resourceType": "Observation",
                 "status": "final",
                 "code": { "text": "Test" },
                 "subject": { "reference": "Patient/p-001" }
               }
               """;
    }
}
