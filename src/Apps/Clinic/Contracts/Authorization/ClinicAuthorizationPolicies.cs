namespace Vitarish.Clinic.Contracts.Authorization;

public static class ClinicAuthorizationPolicies
{
    /// <summary>Only the clinic owner may plan and manage surgeries, their types, and their settings.</summary>
    public const string PlanSurgeries = "Clinic.Surgeries.Plan";

    /// <summary>Only the clinic owner or a platform admin may set the clinic up or set the guide aside for later.</summary>
    public const string ManageSetup = "Clinic.Setup.Manage";
}
