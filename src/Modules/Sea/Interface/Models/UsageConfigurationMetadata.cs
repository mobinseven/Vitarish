namespace Vitarish.Sea.Interface.Models;

public sealed record UsageConfigurationMetadata
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string? ChatInstructions { get; set; }
    public string? ChatModel { get; set; }
    public string? ClinicPatientIntakeInstructions { get; set; }
    public string? ProjectOverviewInstructions { get; set; }
    public string? ProcedureOverviewInstructions { get; set; }
    public string? PluginIdentifier { get; set; }
    public string? ProjectsSortMethod
    {
        get;
        set => SetField(ref field, value);
    }
    public event EventHandler? OnChange;
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

    private void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnChange?.Invoke(this, new EventArgs());
    }
}
