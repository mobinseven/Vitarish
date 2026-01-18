using Vitarish.Sea.Interface.Plugins.Clinic.Models;

public static class ClinicProjectMetadataExtensions
{
    private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    extension(ProjectDto projectDto)
    {
        public ClinicProjectMetadata GetClinicProjectMetadata() => JsonSerializer.Deserialize<ClinicProjectMetadata>(string.IsNullOrWhiteSpace(projectDto.Metadata) ? "{}" : projectDto.Metadata, JsonSerializerOptions) ?? new();
    }
}
