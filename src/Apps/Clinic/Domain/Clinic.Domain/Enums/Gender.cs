using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender { Unspecified, Female, Male }
