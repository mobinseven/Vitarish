using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<Gender>))]
public enum Gender { Unspecified, Female, Male }

[JsonConverter(typeof(JsonStringEnumConverter<Education>))]
public enum Education { Unspecified, NoFormalEducation, PrimarySchool, HighSchool, AssociateDegree, BachelorDegree, MasterDegree, Doctorate, Professional }

[JsonConverter(typeof(JsonStringEnumConverter<PatientSortBy>))]
public enum PatientSortBy
{
    Name,
    Template,
    IdentificationCode,
    DateOfBirth,
    /// <summary>Deprecated alias for <see cref="NextSurgery"/>; retained for wire compatibility.</summary>
    DateOfSurgery,
    CreatedAt,
    UpdatedAt,
    /// <summary>The most recent non-cancelled surgery that already started.</summary>
    LastSurgery,
    /// <summary>The earliest upcoming planned surgery.</summary>
    NextSurgery
}
