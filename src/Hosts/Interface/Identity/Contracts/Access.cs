using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Vitarish.Interface.Identity.Contracts;

public class Access
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    private string? read;
    public string? Read
    {
        get => read;
        set => SetField(ref read, value);
    }

    private string? write;
    public string? Write
    {
        get => write;
        set => SetField(ref write, value);
    }

    private bool entrance;
    public bool Entrance
    {
        get => entrance;
        set => SetField(ref entrance, value);
    }

    private bool admin;
    public bool Admin
    {
        get => admin;
        set => SetField(ref admin, value);
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
    public bool PropertyChangedEventSet => PropertyChanged is not null;
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
