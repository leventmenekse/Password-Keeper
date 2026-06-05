using CommunityToolkit.Mvvm.ComponentModel;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.App.ViewModels;

public partial class CategoryItemViewModel : ObservableObject
{
    public Category? Model { get; }
    public Guid Id => Model?.Id ?? Guid.Empty;
    public bool IsAll => Model is null;

    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private int count;

    public string Name
    {
        get => IsAll ? "All entries" : Model!.Name;
        set
        {
            if (IsAll) return;
            if (Model!.Name == value) return;
            Model.Name = value;
            OnPropertyChanged();
        }
    }

    public CategoryItemViewModel(Category? model)
    {
        Model = model;
    }
}
