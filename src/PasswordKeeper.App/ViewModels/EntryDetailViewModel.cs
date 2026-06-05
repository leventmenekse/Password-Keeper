using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordKeeper.App.Services;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.App.ViewModels;

public partial class EntryDetailViewModel : ObservableObject
{
    private readonly IClipboardService _clipboard;
    private VaultEntry? _entry;

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private Guid? categoryId;
    [ObservableProperty] private bool isPasswordRevealed;
    [ObservableProperty] private bool hasEntry;

    public int PasswordStrength => ComputeStrength(Password);
    public int PasswordEntropyBits => (int)Math.Round((Password?.Length ?? 0) * 6.2);
    public int PasswordLength => Password?.Length ?? 0;
    public DateTimeOffset UpdatedAt => _entry?.UpdatedAt ?? DateTimeOffset.UtcNow;

    partial void OnPasswordChanged(string value)
    {
        OnPropertyChanged(nameof(PasswordStrength));
        OnPropertyChanged(nameof(PasswordEntropyBits));
        OnPropertyChanged(nameof(PasswordLength));
    }

    private static int ComputeStrength(string? pw)
    {
        if (string.IsNullOrEmpty(pw)) return 0;
        int s = 0;
        if (pw.Length >= 8) s++;
        if (pw.Length >= 14) s++;
        if (pw.Length >= 20) s++;
        bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
        foreach (var c in pw)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSymbol = true;
        }
        if (hasUpper && hasLower) s++;
        if (hasDigit) s++;
        if (hasSymbol) s++;
        return Math.Min(s, 5);
    }

    public ObservableCollection<CustomFieldViewModel> CustomFields { get; } = new();

    public IReadOnlyList<Category> Categories { get; private set; } = Array.Empty<Category>();

    public event EventHandler<VaultEntry>? Saved;
    public event EventHandler<VaultEntry>? Deleted;

    public EntryDetailViewModel(IClipboardService clipboard)
    {
        _clipboard = clipboard;
    }

    public void LoadEntry(VaultEntry? entry, IReadOnlyList<Category> categories)
    {
        Categories = categories;
        OnPropertyChanged(nameof(Categories));

        _entry = entry;
        HasEntry = entry is not null;
        CustomFields.Clear();

        if (entry is null)
        {
            Title = Username = Password = Url = Notes = string.Empty;
            CategoryId = null;
        }
        else
        {
            Title = entry.Title;
            Username = entry.Username;
            Password = entry.Password;
            Url = entry.Url;
            Notes = entry.Notes;
            CategoryId = entry.CategoryId;
            foreach (var f in entry.CustomFields)
                CustomFields.Add(new CustomFieldViewModel(f, _clipboard, RemoveField));
        }
        // New / empty-password entries start revealed so the user can immediately type.
        IsPasswordRevealed = string.IsNullOrEmpty(Password);
        OnPropertyChanged(nameof(UpdatedAt));
    }

    public void ApplyGeneratedPassword(string password) => Password = password;

    [RelayCommand]
    private void Save()
    {
        if (_entry is null) return;
        _entry.Title = Title;
        _entry.Username = Username;
        _entry.Password = Password;
        _entry.Url = Url;
        _entry.Notes = Notes;
        _entry.CategoryId = CategoryId;
        _entry.CustomFields = CustomFields.Select(f => f.ToModel()).ToList();
        _entry.UpdatedAt = DateTimeOffset.UtcNow;
        Saved?.Invoke(this, _entry);
    }

    [RelayCommand]
    private void Delete()
    {
        if (_entry is null) return;
        Deleted?.Invoke(this, _entry);
    }

    [RelayCommand]
    private void CopyUsername()
    {
        if (!string.IsNullOrEmpty(Username)) _clipboard.CopySensitive(Username);
    }

    [RelayCommand]
    private void CopyPassword()
    {
        if (!string.IsNullOrEmpty(Password)) _clipboard.CopySensitive(Password);
    }

    [RelayCommand]
    private void ToggleReveal() => IsPasswordRevealed = !IsPasswordRevealed;

    [RelayCommand]
    private void OpenUrl()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(Url) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
        }
        catch { /* invalid URL — ignore */ }
    }

    [RelayCommand]
    private void AddField()
    {
        if (_entry is null) return;
        var field = new CustomField { Label = "New field" };
        CustomFields.Add(new CustomFieldViewModel(field, _clipboard, RemoveField));
    }

    [RelayCommand]
    private void AddSecretField()
    {
        if (_entry is null) return;
        var field = new CustomField { Label = "New secret", IsSecret = true };
        CustomFields.Add(new CustomFieldViewModel(field, _clipboard, RemoveField));
    }

    [RelayCommand]
    private void AddSshTemplate()
    {
        if (_entry is null) return;
        // SSH preset: typical fields. User can adjust freely.
        CustomFields.Add(new CustomFieldViewModel(new CustomField { Label = "host" }, _clipboard, RemoveField));
        CustomFields.Add(new CustomFieldViewModel(new CustomField { Label = "port", Value = "22" }, _clipboard, RemoveField));
        CustomFields.Add(new CustomFieldViewModel(new CustomField { Label = "key path" }, _clipboard, RemoveField));
        CustomFields.Add(new CustomFieldViewModel(new CustomField { Label = "private key", IsSecret = true }, _clipboard, RemoveField));
        CustomFields.Add(new CustomFieldViewModel(new CustomField { Label = "key passphrase", IsSecret = true }, _clipboard, RemoveField));
    }

    private void RemoveField(CustomFieldViewModel vm) => CustomFields.Remove(vm);
}
