namespace PasswordKeeper.App.Services;

/// <summary>
/// Controls whether PasswordKeeper launches automatically when the current user
/// signs in to Windows.
/// </summary>
public interface IStartupService
{
    /// <summary>True if a launch-at-startup entry is currently registered.</summary>
    bool IsEnabled();

    /// <summary>
    /// Register or remove the launch-at-startup entry. Returns true on success;
    /// false if the registry could not be updated (e.g. access denied).
    /// </summary>
    bool SetEnabled(bool enabled);
}
