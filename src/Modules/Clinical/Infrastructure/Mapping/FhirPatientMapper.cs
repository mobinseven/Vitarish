using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Mapping;

internal static class FhirPatientMapper
{
    private static readonly FhirJsonSerializer Serializer = new();

    public static FhirMappedResource Map(PatientRegistrationPayload payload)
    {
        Patient patient = new()
        {
            Name = [new HumanName
            {
                Use = HumanName.NameUse.Official,
                Family = payload.FamilyName,
                Given = BuildGiven(payload)
            }],
            BirthDate = payload.BirthDate.ToString("yyyy-MM-dd"),
            Gender = ParseGender(payload.Gender)
        };

        if (!string.IsNullOrWhiteSpace(payload.Phone) || !string.IsNullOrWhiteSpace(payload.Email))
        {
            List<ContactPoint> telecoms = [];
            if (!string.IsNullOrWhiteSpace(payload.Phone))
            {
                telecoms.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = payload.Phone.Trim(), Use = ContactPoint.ContactPointUse.Home });
            }

            if (!string.IsNullOrWhiteSpace(payload.Email))
            {
                telecoms.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Email, Value = payload.Email.Trim() });
            }

            patient.Telecom = telecoms;
        }

        if (!string.IsNullOrWhiteSpace(payload.AddressLine) || !string.IsNullOrWhiteSpace(payload.City))
        {
            patient.Address = [new Address
            {
                Line = string.IsNullOrWhiteSpace(payload.AddressLine) ? [] : [payload.AddressLine],
                City = payload.City,
                State = payload.State,
                PostalCode = payload.PostalCode,
                Country = payload.Country
            }];
        }

        if (!string.IsNullOrWhiteSpace(payload.OrganizationFhirReference))
        {
            patient.ManagingOrganization = new ResourceReference(payload.OrganizationFhirReference);
        }

        if (!string.IsNullOrWhiteSpace(payload.ExternalIdentifier))
        {
            patient.Identifier = [new Identifier
            {
                System = payload.ExternalIdentifierSystem ?? "urn:oid:local",
                Value = payload.ExternalIdentifier
            }];
        }

        return new FhirMappedResource
        {
            ResourceType = "Patient",
            ResourceJson = Serializer.SerializeToString(patient)
        };
    }

    private static IEnumerable<string> BuildGiven(PatientRegistrationPayload payload)
    {
        yield return payload.GivenName;
        if (!string.IsNullOrWhiteSpace(payload.MiddleName))
        {
            yield return payload.MiddleName;
        }
    }

    private static AdministrativeGender? ParseGender(string? gender) => gender?.ToLowerInvariant() switch
    {
        "male" or "m" => AdministrativeGender.Male,
        "female" or "f" => AdministrativeGender.Female,
        "other" => AdministrativeGender.Other,
        _ => AdministrativeGender.Unknown
    };
}
