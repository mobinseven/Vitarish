namespace Vorn.Aaas.Client.Admin;

using Microsoft.AspNetCore.Components;

public partial class RoleEditor : ComponentBase
{
    [Parameter] public string? Read { get; set; }
    [Parameter] public EventCallback<string?> ReadChanged { get; set; }
    [Parameter] public string? Write { get; set; }
    [Parameter] public EventCallback<string?> WriteChanged { get; set; }
    [Parameter] public IEnumerable<string> Roles { get; set; } = [];

    HashSet<string> readRoles = new(StringComparer.OrdinalIgnoreCase);
    HashSet<string> writeRoles = new(StringComparer.OrdinalIgnoreCase);

    protected override void OnParametersSet()
    {
        readRoles = Split(Read);
        writeRoles = Split(Write);
    }

    static HashSet<string> Split(string? value) =>
        value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new(StringComparer.OrdinalIgnoreCase);

    Task ToggleRead(string role, bool value)
    {
        if (value)
        {
            readRoles.Add(role);
        }
        else
        {
            readRoles.Remove(role);
        }

        return ReadChanged.InvokeAsync(readRoles.Count == 0 ? null : string.Join(',', readRoles));
    }

    Task ToggleWrite(string role, bool value)
    {
        if (value)
        {
            writeRoles.Add(role);
        }
        else
        {
            writeRoles.Remove(role);
        }

        return WriteChanged.InvokeAsync(writeRoles.Count == 0 ? null : string.Join(',', writeRoles));
    }
}
