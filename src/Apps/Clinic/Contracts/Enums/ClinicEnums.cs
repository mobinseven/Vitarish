using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender { Unspecified, Female, Male }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Education { Unspecified, NoFormalEducation, PrimarySchool, HighSchool, AssociateDegree, BachelorDegree, MasterDegree, Doctorate, Professional }

[JsonConverter(typeof(JsonStringEnumConverter<PatientSortBy>))]
public enum PatientSortBy { Name, Template, IdentificationCode, DateOfBirth, DateOfSurgery, CreatedAt, UpdatedAt }
