using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ValidationCoordinatorService(
    IConflictHandler conflictHandler) : IValidationCoordinator
{
    public async Task<IReadOnlyList<WorkflowValidationResult>> ValidateAsync(
        ClinicalActionType actionType,
        object? payload,
        WorkflowPolicyDefinition policy,
        string patientFhirReference,
        string? encounterFhirReference,
        CancellationToken cancellationToken = default)
    {
        List<WorkflowValidationResult> errors = [];

        // Required field validation
        if (policy.RequiredFieldsByActionType.TryGetValue(actionType, out string[]? requiredFields))
        {
            foreach (string field in requiredFields)
            {
                if (!HasField(payload, field))
                {
                    errors.Add(WorkflowValidationResult.Error(
                        "REQUIRED_FIELD",
                        $"Required field '{field}' is missing for action '{actionType}'.",
                        field));
                }
            }
        }

        // Payload-specific validation
        errors.AddRange(ValidatePayload(actionType, payload));

        return errors;
    }

    private static IEnumerable<WorkflowValidationResult> ValidatePayload(ClinicalActionType actionType, object? payload)
    {
        return actionType switch
        {
            ClinicalActionType.RegisterPatient when payload is PatientRegistrationPayload p =>
                ValidateRegistration(p),
            ClinicalActionType.RecordVitals when payload is VitalsPayload p =>
                ValidateVitals(p),
            ClinicalActionType.AddDiagnosis when payload is DiagnosisPayload p =>
                ValidateDiagnosis(p),
            ClinicalActionType.AddMedicationRequest when payload is MedicationRequestPayload p =>
                ValidateMedicationRequest(p),
            ClinicalActionType.AddClinicalNote when payload is ClinicalNotePayload p =>
                ValidateNote(p),
            _ => []
        };
    }

    private static IEnumerable<WorkflowValidationResult> ValidateRegistration(PatientRegistrationPayload p)
    {
        if (string.IsNullOrWhiteSpace(p.FamilyName))
        {
            yield return WorkflowValidationResult.Error("REQUIRED_FIELD", "Patient family name is required.", "FamilyName");
        }

        if (string.IsNullOrWhiteSpace(p.GivenName))
        {
            yield return WorkflowValidationResult.Error("REQUIRED_FIELD", "Patient given name is required.", "GivenName");
        }
    }

    private static IEnumerable<WorkflowValidationResult> ValidateVitals(VitalsPayload p)
    {
        bool hasAnyVital = p.SystolicBp.HasValue || p.PulseRate.HasValue || p.Temperature.HasValue ||
                           p.WeightKg.HasValue || p.HeightCm.HasValue || p.SpO2Percent.HasValue || p.RespiratoryRate.HasValue;

        if (!hasAnyVital)
        {
            yield return WorkflowValidationResult.Error("NO_VITALS", "At least one vital sign must be provided.", null);
        }

        if ((p.SystolicBp.HasValue) != (p.DiastolicBp.HasValue))
        {
            yield return WorkflowValidationResult.Error("BP_INCOMPLETE", "Both systolic and diastolic blood pressure must be provided together.", "DiastolicBp");
        }
    }

    private static IEnumerable<WorkflowValidationResult> ValidateDiagnosis(DiagnosisPayload p)
    {
        if (string.IsNullOrWhiteSpace(p.Code))
        {
            yield return WorkflowValidationResult.Error("REQUIRED_FIELD", "Diagnosis code is required.", "Code");
        }

        if (string.IsNullOrWhiteSpace(p.CodeSystem))
        {
            yield return WorkflowValidationResult.Error("REQUIRED_FIELD", "Diagnosis code system is required.", "CodeSystem");
        }
    }

    private static IEnumerable<WorkflowValidationResult> ValidateMedicationRequest(MedicationRequestPayload p)
    {
        if (string.IsNullOrWhiteSpace(p.MedicationCode))
        {
            yield return WorkflowValidationResult.Error("REQUIRED_FIELD", "Medication code is required.", "MedicationCode");
        }

        if (string.IsNullOrWhiteSpace(p.MedicationCodeSystem))
        {
            yield return WorkflowValidationResult.Error("REQUIRED_FIELD", "Medication code system is required.", "MedicationCodeSystem");
        }
    }

    private static IEnumerable<WorkflowValidationResult> ValidateNote(ClinicalNotePayload p)
    {
        if (string.IsNullOrWhiteSpace(p.Content))
        {
            yield return WorkflowValidationResult.Error("REQUIRED_FIELD", "Note content is required.", "Content");
        }

        if (string.IsNullOrWhiteSpace(p.NoteType))
        {
            yield return WorkflowValidationResult.Error("REQUIRED_FIELD", "Note type is required.", "NoteType");
        }
    }

    private static bool HasField(object? payload, string fieldName)
    {
        if (payload is null)
        {
            return false;
        }

        System.Reflection.PropertyInfo? property = payload.GetType().GetProperty(fieldName);
        if (property is null)
        {
            return false;
        }

        object? value = property.GetValue(payload);
        return value is not null && (value is not string str || !string.IsNullOrWhiteSpace(str));
    }
}
