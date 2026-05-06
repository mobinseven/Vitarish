namespace Vitarish.Clinical.Domain.Models;

public sealed record VisitWorkflowFilter
{
    public VisitWorkflowStatus? Status { get; init; }
    public string? AssignedLocationId { get; init; }
    public string? AssignedProviderId { get; init; }
    public string? PatientFhirReference { get; init; }
    public string? VisitType { get; init; }
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public int Take { get; init; } = 50;
}
