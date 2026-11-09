namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

/// <summary>
/// Tamin service-type codes. They select a coding list (<c>services?service-type=</c>) and are the
/// <c>srvType</c> the guide requires on every prescription item.
/// </summary>
internal static class TaminServiceTypeCodes
{
    public const string Drug = "01";
    public const string Laboratory = "02";
    public const string Visit = "16";
    public const string MedicalService = "17";
}
