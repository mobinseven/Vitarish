using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Mapping;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class FhirResourceMapperService : IFhirResourceMapper
{
    public Task<IReadOnlyList<FhirMappedResource>> MapAsync(
        ClinicalActionType actionType,
        object payload,
        string patientFhirReference,
        string? encounterFhirReference,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FhirMappedResource> result = actionType switch
        {
            ClinicalActionType.RegisterPatient when payload is PatientRegistrationPayload p =>
                [FhirPatientMapper.Map(p)],

            ClinicalActionType.StartEncounter when payload is StartEncounterPayload p =>
                [FhirEncounterMapper.Map(p, patientFhirReference)],

            ClinicalActionType.RecordVitals when payload is VitalsPayload p =>
                FhirObservationMapper.Map(p, patientFhirReference, encounterFhirReference),

            ClinicalActionType.AddDiagnosis when payload is DiagnosisPayload p =>
                [FhirConditionMapper.Map(p, patientFhirReference, encounterFhirReference)],

            ClinicalActionType.AddMedicationRequest when payload is MedicationRequestPayload p =>
                [FhirMedicationRequestMapper.Map(p, patientFhirReference, encounterFhirReference, null)],

            ClinicalActionType.AddClinicalNote when payload is ClinicalNotePayload p =>
                [FhirDocumentReferenceMapper.Map(p, patientFhirReference, encounterFhirReference)],

            ClinicalActionType.SignNote when payload is ClinicalNotePayload p =>
                [FhirDocumentReferenceMapper.Map(p with { Status = "current" }, patientFhirReference, encounterFhirReference)],

            _ => throw new InvalidOperationException(
                $"No FHIR mapper available for action type '{actionType}' with payload type '{payload.GetType().Name}'.")
        };

        return Task.FromResult(result);
    }
}
