namespace Vitarish.Sea.Interface.Plugins.Clinic.Models;

public static class ClinicProjectMetadataExtensions
{
    extension(ProjectDto projectDto)
    {
        public ClinicProjectMetadata? GetClinicProjectMetadata() => JsonSerializer.Deserialize<ClinicProjectMetadata>(string.IsNullOrWhiteSpace(projectDto.Metadata) ? "{}" : projectDto.Metadata);
    }
}
