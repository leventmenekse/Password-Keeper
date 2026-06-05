namespace PasswordKeeper.App.Services;

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;

    /// <summary>Register the configured hotkey. Returns false if Windows refused (e.g. already in use).</summary>
    bool Register();
    void Unregister();
    bool Configure(bool ctrl, bool alt, bool shift, bool win, string key);
}
