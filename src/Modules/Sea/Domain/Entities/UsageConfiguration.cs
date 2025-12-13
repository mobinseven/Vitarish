namespace Vitarish.Sea.Domain.Entities;

public class UsageConfiguration : Entity
{
    private string? _reminderSignature;
    private string _metadata = "{}";
    private UsageData _usageData = new(0, 0);
    public string? ReminderSignature
    {
        get => _reminderSignature;
        set => SetField(ref _reminderSignature, value, nameof(ReminderSignature));
    }

    public string Metadata
    {
        get => _metadata;
        set => SetField(ref _metadata, value, nameof(Metadata));
    }

    public UsageData UsageData
    {
        get => _usageData;
        set => SetField(ref _usageData, value, nameof(UsageData));
    }

    public UsageConfiguration()
    {
        Id = Guid.NewGuid();
    }
}

