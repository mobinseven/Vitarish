namespace Vitarish.Clinic.Contracts.Authorization;

public static class ClinicAuthorizationPolicies
{
    /// <summary>Only the clinic owner may plan and manage surgeries, their types, and their settings.</summary>
    public const string PlanSurgeries = "Clinic.Surgeries.Plan";
}
