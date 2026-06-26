namespace Vitarish.Sea.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ProjectSortBy>))]
public enum ProjectSortBy
{
    Name,
    Template
}

