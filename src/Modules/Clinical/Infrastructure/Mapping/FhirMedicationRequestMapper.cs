using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Mapping;

internal static class FhirMedicationRequestMapper
{
    private static readonly FhirJsonSerializer Serializer = new();

    public static FhirMappedResource Map(MedicationRequestPayload payload, string patientFhirReference, string? encounterFhirReference, string? requesterFhirReference)
    {
        MedicationRequest request = new()
        {
            Status = ParseStatus(payload.Status),
            Intent = ParseIntent(payload.Intent),
            Subject = new ResourceReference(patientFhirReference),
            Medication = new CodeableConcept(payload.MedicationCodeSystem, payload.MedicationCode, payload.MedicationDisplay)
        };

        if (!string.IsNullOrWhiteSpace(encounterFhirReference))
        {
            request.Encounter = new ResourceReference(encounterFhirReference);
        }

        if (!string.IsNullOrWhiteSpace(requesterFhirReference))
        {
            request.Requester = new ResourceReference(requesterFhirReference);
        }

        if (!string.IsNullOrWhiteSpace(payload.DosageInstruction))
        {
            request.DosageInstruction = [new Dosage { Text = payload.DosageInstruction }];
        }

        if (!string.IsNullOrWhiteSpace(payload.Notes))
        {
            request.Note = [new Annotation { Text = new Markdown(payload.Notes) }];
        }

        return new FhirMappedResource
        {
            ResourceType = "MedicationRequest",
            ResourceJson = Serializer.SerializeToString(request)
        };
    }

    private static MedicationRequest.MedicationrequestStatus ParseStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "active" => MedicationRequest.MedicationrequestStatus.Active,
        "on-hold" => MedicationRequest.MedicationrequestStatus.OnHold,
        "cancelled" => MedicationRequest.MedicationrequestStatus.Cancelled,
        "completed" => MedicationRequest.MedicationrequestStatus.Completed,
        "stopped" => MedicationRequest.MedicationrequestStatus.Stopped,
        _ => MedicationRequest.MedicationrequestStatus.Active
    };

    private static MedicationRequest.MedicationRequestIntent ParseIntent(string? intent) => intent?.ToLowerInvariant() switch
    {
        "proposal" => MedicationRequest.MedicationRequestIntent.Proposal,
        "plan" => MedicationRequest.MedicationRequestIntent.Plan,
        "order" => MedicationRequest.MedicationRequestIntent.Order,
        "original-order" => MedicationRequest.MedicationRequestIntent.OriginalOrder,
        _ => MedicationRequest.MedicationRequestIntent.Order
    };
}
