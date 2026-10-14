namespace Vitarish.Clinic.Domain.Models;

public static class PatientLegacyMetadataKeys
{
    /// <summary>
    /// The patient date-of-surgery field was removed in favour of surgery
    /// records; existing values are discarded when a patient is next saved.
    /// </summary>
    public const string DateOfSurgery = "DateOfSurgery";
}
