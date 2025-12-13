namespace Vitarish.Sea.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ProjectSortBy>))]
public enum ProjectSortBy
{
    Name,
    Template
}

