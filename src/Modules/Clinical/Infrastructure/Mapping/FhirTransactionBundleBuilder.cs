using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Mapping;

internal static class FhirTransactionBundleBuilder
{
    private static readonly FhirJsonSerializer Serializer = new();

    public static FhirMappedResource Build(IReadOnlyList<FhirMappedResource> resources)
    {
        Bundle bundle = new()
        {
            Type = Bundle.BundleType.Transaction,
            Entry = []
        };

        int index = 0;
        foreach (FhirMappedResource resource in resources)
        {
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{Guid.NewGuid():N}",
                Request = new Bundle.RequestComponent
                {
                    Method = string.IsNullOrWhiteSpace(resource.ResourceId)
                        ? Bundle.HTTPVerb.POST
                        : Bundle.HTTPVerb.PUT,
                    Url = string.IsNullOrWhiteSpace(resource.ResourceId)
                        ? resource.ResourceType
                        : $"{resource.ResourceType}/{resource.ResourceId}"
                },
                Resource = new FhirJsonParser().Parse<Resource>(resource.ResourceJson)
            });

            index++;
        }

        return new FhirMappedResource
        {
            ResourceType = "Bundle",
            ResourceJson = Serializer.SerializeToString(bundle)
        };
    }
}
