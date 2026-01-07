namespace Vitarish.Sea.Interface.Components.UsageConfigurations;

public sealed record UsageConfigurationMetadata
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string? ProjectGridCellDisplayPluginId { get; set; }
    public string? ChatInstructions { get; set; }
    public string? ChatModel { get; set; }
    public string? ClinicPatientIntakeInstructions { get; set; }
    public string? ProjectOverviewInstructions { get; set; }
    public string? ProcedureOverviewInstructions { get; set; }
    public string[]? PluginIdentifiers { get; set; }

    public static UsageConfigurationMetadata FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new UsageConfigurationMetadata();
        }

        try
        {
            return JsonSerializer.Deserialize<UsageConfigurationMetadata>(json, SerializerOptions)
                ?? new UsageConfigurationMetadata();
        }
        catch (JsonException)
        {
            return new UsageConfigurationMetadata();
        }
    }

    public string ToJson()
        => JsonSerializer.Serialize(this, SerializerOptions);
}
