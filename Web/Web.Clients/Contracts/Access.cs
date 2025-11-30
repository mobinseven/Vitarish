using System.ComponentModel;
using System.Runtime.CompilerServices;
using Vitarish.Web.Clients.Extensions;

namespace Vitarish.Web.Clients.Contracts;

/// <summary>
/// Represents user access permissions.
/// </summary>
public class Access
{
    /// <summary>
    /// Unique identifier for the access instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    private bool entrance;
    private string? read;
    private string? write;
    private bool admin;

    /// <summary>
    /// Read permission roles (comma-separated).
    /// </summary>
    public string? Read
    {
        get => read;
        set => SetField(ref read, value);
    }

    /// <summary>
    /// Write permission roles (comma-separated).
    /// </summary>
    public string? Write
    {
        get => write;
        set => SetField(ref write, value);
    }

    /// <summary>
    /// Indicates if the user has entrance permission.
    /// </summary>
    public bool Entrance
    {
        get => entrance;
        set => SetField(ref entrance, value);
    }

    /// <summary>
    /// Indicates if the user has admin permission.
    /// </summary>
    public bool Admin
    {
        get => admin;
        set => SetField(ref admin, value);
    }

    /// <summary>
    /// Array of read permission roles.
    /// </summary>
    public virtual string[] Readers
    {
        get => string.IsNullOrWhiteSpace(Read) ? [] : Read?.Split(',') ?? [];
        set => Read = string.Join(',', value);
    }

    /// <summary>
    /// Array of write permission roles.
    /// </summary>
    public virtual string[] Writers
    {
        get => string.IsNullOrWhiteSpace(Write) ? [] : Write?.Split(',') ?? [];
        set => Write = string.Join(',', value);
    }

    /// <summary>
    /// Combined array of all roles (readers and writers).
    /// </summary>
    public virtual string[] Roles => Readers.Union(Writers).Distinct().Order().ToArray();

    /// <summary>
    /// Serialized access value for claims.
    /// </summary>
    public virtual string Value => this.GetClaimAccessValue();

    /// <summary>
    /// Event raised when a property changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Indicates if PropertyChanged event is subscribed.
    /// </summary>
    public bool PropertyChangedEventSet => PropertyChanged is not null;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Sets a field and raises PropertyChanged if the value changed.
    /// </summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        if (PropertyChanged is not null)
        {
            OnPropertyChanged(propertyName);
        }
        return true;
    }
}
