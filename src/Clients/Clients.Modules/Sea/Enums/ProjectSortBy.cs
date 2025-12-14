namespace Vitarish.Clients.Modules.Sea.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ProjectSortBy>))]
public enum ProjectSortBy
{
    Name,
    Template
}

