using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Mapping;

internal static class FhirEncounterMapper
{
    private static readonly FhirJsonSerializer Serializer = new();

    public static FhirMappedResource Map(StartEncounterPayload payload, string patientFhirReference)
    {
        Encounter encounter = new()
        {
            Status = Encounter.EncounterStatus.InProgress,
            Subject = new ResourceReference(patientFhirReference),
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", payload.EncounterClass.ToUpperInvariant())
        };

        if (!string.IsNullOrWhiteSpace(payload.ReasonCode))
        {
            encounter.ReasonCode = [new CodeableConcept { Text = payload.ReasonCode }];
        }

        if (!string.IsNullOrWhiteSpace(payload.LocationFhirReference))
        {
            encounter.Location = [new Encounter.LocationComponent
            {
                Location = new ResourceReference(payload.LocationFhirReference)
            }];
        }

        if (!string.IsNullOrWhiteSpace(payload.PractitionerFhirReference))
        {
            encounter.Participant = [new Encounter.ParticipantComponent
            {
                Individual = new ResourceReference(payload.PractitionerFhirReference)
            }];
        }

        encounter.Period = new Period
        {
            StartElement = new FhirDateTime((payload.PeriodStart ?? DateTimeOffset.UtcNow).ToString("O"))
        };

        if (!string.IsNullOrWhiteSpace(payload.ServiceType))
        {
            encounter.ServiceType = new CodeableConcept { Text = payload.ServiceType };
        }

        return new FhirMappedResource
        {
            ResourceType = "Encounter",
            ResourceJson = Serializer.SerializeToString(encounter)
        };
    }
}
