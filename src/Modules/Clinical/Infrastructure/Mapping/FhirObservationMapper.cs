using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Mapping;

internal static class FhirObservationMapper
{
    private static readonly FhirJsonSerializer Serializer = new();

    private const string LoincSystem = "http://loinc.org";

    public static IReadOnlyList<FhirMappedResource> Map(VitalsPayload payload, string patientFhirReference, string? encounterFhirReference)
    {
        List<FhirMappedResource> resources = [];

        if (payload.SystolicBp.HasValue && payload.DiastolicBp.HasValue)
        {
            resources.Add(BuildBloodPressure(payload, patientFhirReference, encounterFhirReference));
        }

        if (payload.PulseRate.HasValue)
        {
            resources.Add(BuildSimpleObservation("85354-9", "Blood pressure panel with all children optional", "8867-4", "Heart rate", "/min", payload.PulseRate.Value, patientFhirReference, encounterFhirReference));
        }

        if (payload.Temperature.HasValue)
        {
            string unit = payload.TemperatureUnit ?? "Cel";
            resources.Add(BuildSimpleObservation("8310-5", "Body temperature", "8310-5", "Body temperature", unit, payload.Temperature.Value, patientFhirReference, encounterFhirReference));
        }

        if (payload.WeightKg.HasValue)
        {
            resources.Add(BuildSimpleObservation("29463-7", "Body weight", "29463-7", "Body weight", "kg", payload.WeightKg.Value, patientFhirReference, encounterFhirReference));
        }

        if (payload.HeightCm.HasValue)
        {
            resources.Add(BuildSimpleObservation("8302-2", "Body height", "8302-2", "Body height", "cm", payload.HeightCm.Value, patientFhirReference, encounterFhirReference));
        }

        if (payload.SpO2Percent.HasValue)
        {
            resources.Add(BuildSimpleObservation("59408-5", "Oxygen saturation in Arterial blood by Pulse oximetry", "59408-5", "Oxygen saturation in Arterial blood by Pulse oximetry", "%", payload.SpO2Percent.Value, patientFhirReference, encounterFhirReference));
        }

        if (payload.RespiratoryRate.HasValue)
        {
            resources.Add(BuildSimpleObservation("9279-1", "Respiratory rate", "9279-1", "Respiratory rate", "/min", payload.RespiratoryRate.Value, patientFhirReference, encounterFhirReference));
        }

        return resources;
    }

    private static FhirMappedResource BuildBloodPressure(VitalsPayload payload, string patientRef, string? encounterRef)
    {
        Observation obs = BuildObservationBase("85354-9", "Blood pressure panel with all children optional", patientRef, encounterRef);
        obs.Component = [
            new Observation.ComponentComponent
            {
                Code = new CodeableConcept(LoincSystem, "8480-6", "Systolic blood pressure"),
                Value = new Quantity(payload.SystolicBp!.Value, "mm[Hg]", "http://unitsofmeasure.org")
            },
            new Observation.ComponentComponent
            {
                Code = new CodeableConcept(LoincSystem, "8462-4", "Diastolic blood pressure"),
                Value = new Quantity(payload.DiastolicBp!.Value, "mm[Hg]", "http://unitsofmeasure.org")
            }
        ];

        return new FhirMappedResource
        {
            ResourceType = "Observation",
            ResourceJson = Serializer.SerializeToString(obs)
        };
    }

    private static FhirMappedResource BuildSimpleObservation(string panelCode, string panelDisplay, string code, string display, string unit, decimal value, string patientRef, string? encounterRef)
    {
        Observation obs = BuildObservationBase(code, display, patientRef, encounterRef);
        obs.Value = new Quantity(value, unit, "http://unitsofmeasure.org");

        return new FhirMappedResource
        {
            ResourceType = "Observation",
            ResourceJson = Serializer.SerializeToString(obs)
        };
    }

    private static Observation BuildObservationBase(string code, string display, string patientRef, string? encounterRef)
    {
        Observation obs = new()
        {
            Status = ObservationStatus.Final,
            Category = [new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category", "vital-signs", "Vital Signs")],
            Code = new CodeableConcept(LoincSystem, code, display),
            Subject = new ResourceReference(patientRef),
            Effective = new FhirDateTime(DateTimeOffset.UtcNow.ToString("O"))
        };

        if (!string.IsNullOrWhiteSpace(encounterRef))
        {
            obs.Encounter = new ResourceReference(encounterRef);
        }

        return obs;
    }
}
