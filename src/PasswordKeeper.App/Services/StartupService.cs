using System.Diagnostics;
using Microsoft.Win32;

namespace PasswordKeeper.App.Services;

/// <summary>
/// Implements launch-at-startup via the per-user
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> registry key.
/// Per-user means no admin elevation is required.
/// </summary>
public sealed class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName  = "PasswordKeeper";

    /// <summary>
    /// Command-line flag added to the Run entry so the app can tell it was launched
    /// by Windows at sign-in and start silently in the tray instead of opening login.
    /// </summary>
    public const string StartupArg = "--startup";

    /// <summary>Full path to the running executable, quoted, with the startup flag.</summary>
    private static string ExecutableCommand
    {
        get
        {
            // Environment.ProcessPath points at the launched .exe for a published WPF app.
            var path = Environment.ProcessPath
                       ?? Process.GetCurrentProcess().MainModule?.FileName
                       ?? string.Empty;
            return $"\"{path}\" {StartupArg}";
        }
    }

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch
        {
            return false;
        }
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null) return false;

            if (enabled)
            {
                key.SetValue(ValueName, ExecutableCommand, RegistryValueKind.String);
            }
            else if (key.GetValue(ValueName) is not null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
            return true;
        }
        catch
        {
            // Registry write failed (locked-down policy, AV, etc.) — surface as failure
            // so the caller can decide whether to warn the user.
            return false;
        }
    }
}
