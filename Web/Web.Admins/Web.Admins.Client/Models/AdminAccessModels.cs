using System.ComponentModel;

namespace Vitarish.Web.Admins.Client.Models;

public class Access : INotifyPropertyChanged
{
    private bool entrance;
    private string read = string.Empty;
    private string write = string.Empty;
    private bool admin;

    public bool Entrance
    {
        get => entrance;
        set
        {
            if(entrance != value)
            {
                entrance = value;
                OnPropertyChanged(nameof(Entrance));
            }
        }
    }

    public string Read
    {
        get => read;
        set
        {
            if(read != value)
            {
                read = value;
                OnPropertyChanged(nameof(Read));
            }
        }
    }

    public string Write
    {
        get => write;
        set
        {
            if(write != value)
            {
                write = value;
                OnPropertyChanged(nameof(Write));
            }
        }
    }

    public bool Admin
    {
        get => admin;
        set
        {
            if(admin != value)
            {
                admin = value;
                OnPropertyChanged(nameof(Admin));
            }
        }
    }

    public IEnumerable<string> Readers => (Read ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public IEnumerable<string> Writers => (Write ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string? propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record AccessClaimModel(string Name, Access Access);

public sealed record AdminUserModel(string Id, string UserName, string LastName, Access Access);
