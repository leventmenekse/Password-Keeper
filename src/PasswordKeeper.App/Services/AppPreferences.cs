namespace PasswordKeeper.App.Services;

public sealed class AppPreferences
{
    // Hotkey: stored as individual modifier flags + key string ("A".."Z", "F1".."F12", "0".."9")
    public bool HotkeyCtrl  { get; set; } = true;
    public bool HotkeyAlt   { get; set; } = true;
    public bool HotkeyShift { get; set; } = false;
    public bool HotkeyWin   { get; set; } = false;
    public string HotkeyKey { get; set; } = "P";

    public int IdleLockMinutes      { get; set; } = 5;
    public int ClipboardClearSeconds { get; set; } = 15;

    // Launch PasswordKeeper automatically when Windows starts (per-user, via the
    // HKCU\...\Run registry key). The minimized-to-tray UX makes this unobtrusive.
    public bool LaunchAtStartup { get; set; } = false;
}
