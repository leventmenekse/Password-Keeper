namespace PasswordKeeper.App.Services;

public interface ITrayService : IDisposable
{
    void Show();
    void Hide();
    void SetLocked(bool locked);

    event EventHandler? ShowRequested;
    event EventHandler? LockRequested;
    event EventHandler? PreferencesRequested;
    event EventHandler? QuitRequested;
}
