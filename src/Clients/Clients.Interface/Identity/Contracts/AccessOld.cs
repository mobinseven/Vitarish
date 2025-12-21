using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Vitarish.Clients.Interface.Identity.Contracts;

public class AccessOld
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? Read
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Write
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Entrance
    {
        get;
        set => SetField(ref field, value);
    }
    public bool Admin
    {
        get;
        set => SetField(ref field, value);
    }

    public virtual string[] Readers
    {
        get => string.IsNullOrWhiteSpace(Read) ? [] : Read?.Split(',') ?? [];
        set => Read = string.Join(',', value);
    }

    public virtual string[] Writers
    {
        get => string.IsNullOrWhiteSpace(Write) ? [] : Write?.Split(',') ?? [];
        set => Write = string.Join(',', value);
    }

    public virtual string[] Roles => Readers.Union(Writers).Distinct().Order().ToArray();

    public virtual string Value => this.GetClaimAccessValue();

    public event PropertyChangedEventHandler? PropertyChanged;
    public bool PropertyChandedEventSet => PropertyChanged is not null;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
