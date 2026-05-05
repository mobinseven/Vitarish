namespace Vitarish.Clinical.Domain.Options;

public sealed class ClinicalOptions
{
    public const string Section = "Clinical";

    public bool Enabled { get; set; }
    public bool DefaultRequireAccessReason { get; set; } = true;
    public IList<string> AllowedResourceTypes { get; set; } =
    [
        "Patient",
        "Practitioner",
        "Organization",
        "Observation",
        "Condition",
        "Encounter",
        "DocumentReference"
    ];
    public IList<string> AllowedOperations { get; set; } =
    [
        nameof(Models.FhirInteractionType.Read),
        nameof(Models.FhirInteractionType.Search),
        nameof(Models.FhirInteractionType.Validate),
        nameof(Models.FhirInteractionType.History)
    ];

    public ClinicalFhirOptions Fhir { get; set; } = new();
}

public sealed class ClinicalFhirOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
