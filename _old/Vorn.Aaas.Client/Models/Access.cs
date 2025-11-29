using System.ComponentModel;
using System.Runtime.CompilerServices;
using Vorn.Aaas.Client.Extensions;

namespace Vorn.Aaas.Client.Models;

public class Access
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    private bool entrance;
    private string? read;
    private string? write;
    private bool admin;
    public string? Read
    {
        get => read;
        set => SetField(ref read, value);
    }

    public string? Write
    {
        get => write;
        set => SetField(ref write, value);
    }

    public bool Entrance
    {
        get => entrance;
        set => SetField(ref entrance, value);
    }
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
