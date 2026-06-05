using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordKeeper.App.Services;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.App.ViewModels;

public partial class CustomFieldViewModel : ObservableObject
{
    private readonly IClipboardService _clipboard;
    private readonly Action<CustomFieldViewModel> _onRemove;

    public Guid Id { get; }

    [ObservableProperty] private string label;
    [ObservableProperty] private string value;
    [ObservableProperty] private bool isSecret;
    [ObservableProperty] private bool isRevealed;

    public CustomFieldViewModel(CustomField model, IClipboardService clipboard, Action<CustomFieldViewModel> onRemove)
    {
        Id = model.Id;
        label = model.Label;
        value = model.Value;
        isSecret = model.IsSecret;
        _clipboard = clipboard;
        _onRemove = onRemove;
    }

    public CustomField ToModel() => new()
    {
        Id = Id,
        Label = Label,
        Value = Value,
        IsSecret = IsSecret,
    };

    [RelayCommand]
    private void Copy()
    {
        if (!string.IsNullOrEmpty(Value)) _clipboard.CopySensitive(Value);
    }

    [RelayCommand]
    private void ToggleReveal() => IsRevealed = !IsRevealed;

    [RelayCommand]
    private void Remove() => _onRemove(this);
}
