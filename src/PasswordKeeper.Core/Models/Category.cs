using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PasswordKeeper.Core.Models;

public sealed class Category : INotifyPropertyChanged
{
    private string _name = string.Empty;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
