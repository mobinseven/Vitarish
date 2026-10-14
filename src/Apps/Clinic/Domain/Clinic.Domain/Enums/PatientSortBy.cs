using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<PatientSortBy>))]
public enum PatientSortBy
{
    Name,
    Template,
    IdentificationCode,
    DateOfBirth,
    CreatedAt,
    UpdatedAt,
    LastSurgery,
    NextSurgery
}
