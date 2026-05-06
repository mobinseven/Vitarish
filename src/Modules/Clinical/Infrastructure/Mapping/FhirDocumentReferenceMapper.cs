using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Mapping;

internal static class FhirDocumentReferenceMapper
{
    private static readonly FhirJsonSerializer Serializer = new();

    private const string LoincSystem = "http://loinc.org";

    public static FhirMappedResource Map(ClinicalNotePayload payload, string patientFhirReference, string? encounterFhirReference)
    {
        DocumentReference docRef = new()
        {
            Status = ParseStatus(payload.Status),
            Subject = new ResourceReference(patientFhirReference),
            Type = new CodeableConcept(LoincSystem, payload.NoteType, payload.NoteTypeDisplay ?? payload.NoteType),
            Content = [new DocumentReference.ContentComponent
            {
                Attachment = new Attachment
                {
                    ContentType = payload.ContentType ?? "text/plain",
                    Data = System.Text.Encoding.UTF8.GetBytes(payload.Content)
                }
            }]
        };

        if (!string.IsNullOrWhiteSpace(encounterFhirReference))
        {
            docRef.Context = new DocumentReference.ContextComponent
            {
                Encounter = [new ResourceReference(encounterFhirReference)]
            };
        }

        if (!string.IsNullOrWhiteSpace(payload.AuthorFhirReference))
        {
            docRef.Author = [new ResourceReference(payload.AuthorFhirReference)];
        }

        if (!string.IsNullOrWhiteSpace(payload.Title))
        {
            docRef.Description = payload.Title;
        }

        return new FhirMappedResource
        {
            ResourceType = "DocumentReference",
            ResourceJson = Serializer.SerializeToString(docRef)
        };
    }

    private static DocumentReferenceStatus ParseStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "current" => DocumentReferenceStatus.Current,
        "superseded" => DocumentReferenceStatus.Superseded,
        "entered-in-error" => DocumentReferenceStatus.EnteredInError,
        _ => DocumentReferenceStatus.Current
    };
}
