using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Mapping;

internal static class FhirConditionMapper
{
    private static readonly FhirJsonSerializer Serializer = new();

    public static FhirMappedResource Map(DiagnosisPayload payload, string patientFhirReference, string? encounterFhirReference)
    {
        Condition condition = new()
        {
            Subject = new ResourceReference(patientFhirReference),
            Code = new CodeableConcept(payload.CodeSystem, payload.Code, payload.DisplayText),
            ClinicalStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-clinical",
                payload.ClinicalStatus ?? "active"),
            VerificationStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-ver-status",
                payload.VerificationStatus ?? "confirmed")
        };

        if (!string.IsNullOrWhiteSpace(encounterFhirReference))
        {
            condition.Encounter = new ResourceReference(encounterFhirReference);
        }

        if (!string.IsNullOrWhiteSpace(payload.Notes))
        {
            condition.Note = [new Annotation { Text = new Markdown(payload.Notes) }];
        }

        return new FhirMappedResource
        {
            ResourceType = "Condition",
            ResourceJson = Serializer.SerializeToString(condition)
        };
    }
}
