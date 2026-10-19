namespace Vitarish.Clinic.Application;

/// <summary>What a clinic tenant is: the owner app it belongs to and the modules it runs on.</summary>
public static class ClinicTenant
{
    public const string AppId = "private-practice-owner";

    /// <summary>Module names, as each module's <c>Name</c> reports it, that a new clinic starts with.</summary>
    public static IReadOnlyList<string> ActiveModules { get; } =
        ["Clinic", "Sea", "Appointments", "Medical", "Files", "Intelligence", "Communications"];
}
