using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordKeeper.App.Services;

namespace PasswordKeeper.App.ViewModels;

public partial class PreferencesViewModel : ObservableObject
{
    private readonly IPreferencesService _prefs;

    [ObservableProperty] private bool hotkeyCtrl;
    [ObservableProperty] private bool hotkeyAlt;
    [ObservableProperty] private bool hotkeyShift;
    [ObservableProperty] private bool hotkeyWin;
    [ObservableProperty] private string selectedKey = "P";
    [ObservableProperty] private int idleLockMinutes;
    [ObservableProperty] private int clipboardClearSeconds;
    [ObservableProperty] private bool launchAtStartup;
    [ObservableProperty] private string statusMessage = string.Empty;

    public ObservableCollection<string> AvailableKeys { get; } = new();

    public event EventHandler<AppPreferences>? Applied;
    public event EventHandler? Cancelled;

    public PreferencesViewModel(IPreferencesService prefs)
    {
        _prefs = prefs;

        // letters
        for (char c = 'A'; c <= 'Z'; c++) AvailableKeys.Add(c.ToString());
        // digits
        for (char c = '0'; c <= '9'; c++) AvailableKeys.Add(c.ToString());
        // function keys
        for (int i = 1; i <= 12; i++) AvailableKeys.Add("F" + i);

        var p = _prefs.Current;
        HotkeyCtrl  = p.HotkeyCtrl;
        HotkeyAlt   = p.HotkeyAlt;
        HotkeyShift = p.HotkeyShift;
        HotkeyWin   = p.HotkeyWin;
        SelectedKey = AvailableKeys.Contains(p.HotkeyKey) ? p.HotkeyKey : "P";
        IdleLockMinutes        = Math.Max(1, p.IdleLockMinutes);
        ClipboardClearSeconds  = Math.Max(1, p.ClipboardClearSeconds);
        LaunchAtStartup        = p.LaunchAtStartup;
    }

    public string HotkeyPreview
    {
        get
        {
            var parts = new List<string>();
            if (HotkeyCtrl)  parts.Add("Ctrl");
            if (HotkeyAlt)   parts.Add("Alt");
            if (HotkeyShift) parts.Add("Shift");
            if (HotkeyWin)   parts.Add("Win");
            parts.Add(SelectedKey);
            return string.Join(" + ", parts);
        }
    }

    partial void OnHotkeyCtrlChanged(bool value)    => OnPropertyChanged(nameof(HotkeyPreview));
    partial void OnHotkeyAltChanged(bool value)     => OnPropertyChanged(nameof(HotkeyPreview));
    partial void OnHotkeyShiftChanged(bool value)   => OnPropertyChanged(nameof(HotkeyPreview));
    partial void OnHotkeyWinChanged(bool value)     => OnPropertyChanged(nameof(HotkeyPreview));
    partial void OnSelectedKeyChanged(string value) => OnPropertyChanged(nameof(HotkeyPreview));

    [RelayCommand]
    private void Save()
    {
        if (!HotkeyCtrl && !HotkeyAlt && !HotkeyShift && !HotkeyWin)
        {
            StatusMessage = "Hotkey must include at least one modifier (Ctrl/Alt/Shift/Win).";
            return;
        }

        var updated = new AppPreferences
        {
            HotkeyCtrl  = HotkeyCtrl,
            HotkeyAlt   = HotkeyAlt,
            HotkeyShift = HotkeyShift,
            HotkeyWin   = HotkeyWin,
            HotkeyKey   = SelectedKey,
            IdleLockMinutes        = Math.Max(1, IdleLockMinutes),
            ClipboardClearSeconds  = Math.Max(1, ClipboardClearSeconds),
            LaunchAtStartup        = LaunchAtStartup,
        };

        _prefs.Save(updated);
        Applied?.Invoke(this, updated);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
