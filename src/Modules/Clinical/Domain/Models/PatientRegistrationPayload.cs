namespace Vitarish.Clinical.Domain.Models;

public sealed record PatientRegistrationPayload
{
    public required string FamilyName { get; init; }
    public required string GivenName { get; init; }
    public string? MiddleName { get; init; }
    public required DateOnly BirthDate { get; init; }
    public string? Gender { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? AddressLine { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? OrganizationFhirReference { get; init; }
    public string? ExternalIdentifier { get; init; }
    public string? ExternalIdentifierSystem { get; init; }
}
