namespace Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Operations;

public interface IReminderSourceDefinition
{
    string Kind { get; }
}

public interface IReminderSourceRegistry
{
    bool IsRegistered(string kind);
}

public sealed class ReminderSourceRegistry(IEnumerable<IReminderSourceDefinition> definitions) : IReminderSourceRegistry
{
    private readonly IReadOnlySet<string> _kinds = definitions
        .Select(definition => definition.Kind)
        .Append("manual")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool IsRegistered(string kind) => _kinds.Contains(kind);
}

public sealed class AppointmentReminderSourceDefinition : IReminderSourceDefinition
{
    public string Kind => "appointment";
}

public sealed class SeaReminderSourceDefinition : IReminderSourceDefinition
{
    public string Kind => "sea_event";
}
