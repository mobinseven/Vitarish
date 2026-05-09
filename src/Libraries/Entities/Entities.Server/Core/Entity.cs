using System.ComponentModel;

namespace Vitarish.Entities.Server.Core;


public abstract class Entity : INotifyPropertyChanged
{
    public Guid Id
    {
        get;
        set;
    }
    public DateTimeOffset CreatedAt
    {
        get;
        set => SetField(ref field, value, nameof(CreatedAt));
    }
    public DateTimeOffset? DeletedAt
    {
        get;
        set => SetField(ref field, value, nameof(DeletedAt));
    }

    internal bool IsMarkedForHardDelete { get; set; }

    public void Restore() => DeletedAt = null;

    public void HardDelete() => IsMarkedForHardDelete = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public override bool Equals(object? obj) => obj is Entity other && (ReferenceEquals(this, other) || Id.Equals(other.Id));

    public override int GetHashCode() => Id.GetHashCode();
}
