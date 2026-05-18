namespace Vitarish.Clinical.Interface.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender { Unspecified, Female, Male }
