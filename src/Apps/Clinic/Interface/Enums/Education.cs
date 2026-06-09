namespace Vitarish.Clinic.Interface.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Education
{
    Unspecified,
    NoFormalEducation,
    PrimarySchool,
    HighSchool,
    AssociateDegree,
    BachelorDegree,
    MasterDegree,
    Doctorate,
    Professional
}
