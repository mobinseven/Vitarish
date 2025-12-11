namespace Vitarish.Client.Admin;

public partial class RoleEditor : ComponentBase
{
    [Parameter] public string? Read { get; set; }
    [Parameter] public EventCallback<string?> ReadChanged { get; set; }
    [Parameter] public string? Write { get; set; }
    [Parameter] public EventCallback<string?> WriteChanged { get; set; }
    [Parameter] public IEnumerable<string> Roles { get; set; } = [];

    private HashSet<string> readRoles = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> writeRoles = new(StringComparer.OrdinalIgnoreCase);

    protected override void OnParametersSet()
    {
        readRoles = Split(Read);
        writeRoles = Split(Write);
    }

    private static HashSet<string> Split(string? value) =>
        value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new(StringComparer.OrdinalIgnoreCase);

    private Task ToggleRead(string role, bool value)
    {
        _ = value ? readRoles.Add(role) : readRoles.Remove(role);

        return ReadChanged.InvokeAsync(readRoles.Count == 0 ? null : string.Join(',', readRoles));
    }

    private Task ToggleWrite(string role, bool value)
    {
        _ = value ? writeRoles.Add(role) : writeRoles.Remove(role);

        return WriteChanged.InvokeAsync(writeRoles.Count == 0 ? null : string.Join(',', writeRoles));
    }
}
