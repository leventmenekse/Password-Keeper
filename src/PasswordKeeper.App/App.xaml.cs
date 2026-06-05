using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PasswordKeeper.App.Services;
using PasswordKeeper.App.ViewModels;
using PasswordKeeper.App.Views;
using PasswordKeeper.Core.Crypto;
using PasswordKeeper.Core.Generators;
using PasswordKeeper.Core.Vault;

namespace PasswordKeeper.App;

public partial class App : Application
{
    private ServiceProvider? _services;
    private LoginWindow? _loginWindow;
    private MainWindow? _mainWindow;
    private ITrayService? _tray;
    private IGlobalHotkeyService? _hotkey;
    private bool _shuttingDown;

    private static string DefaultVaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "PasswordKeeper", "vault.json");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Tray app: don't quit when the last window closes; only Quit menu exits.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var services = new ServiceCollection();
        services.AddSingleton<IKeyDerivation, Argon2idKeyDerivation>();
        services.AddSingleton<IVaultCipher, AesGcmVaultCipher>();
        services.AddSingleton<IVaultStore>(_ => new FileVaultStore(DefaultVaultPath));
        services.AddSingleton<IPasswordGenerator, PasswordGenerator>();

        services.AddSingleton<IVaultService, VaultService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IIdleTimerService, IdleTimerService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ITrayService, TrayService>();
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        services.AddSingleton<IPreferencesService, PreferencesService>();
        services.AddSingleton<IStartupService, StartupService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<EntryDetailViewModel>();
        services.AddTransient<PasswordGeneratorViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<PreferencesViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<PreferencesWindow>();

        _services = services.BuildServiceProvider();

        _tray = _services.GetRequiredService<ITrayService>();
        _tray.ShowRequested += (_, _) => Dispatcher.Invoke(OnTrayShow);
        _tray.LockRequested += (_, _) => Dispatcher.Invoke(OnTrayLock);
        _tray.PreferencesRequested += (_, _) => Dispatcher.Invoke(OnTrayPreferences);
        _tray.QuitRequested += (_, _) => Dispatcher.Invoke(Quit);
        _tray.SetLocked(locked: true);
        _tray.Show();

        // Load preferences and apply to services
        var prefs = _services.GetRequiredService<IPreferencesService>();
        prefs.Load();
        ApplyPreferences(prefs.Current);

        // Global hotkey — wake up / focus the app from anywhere
        _hotkey = _services.GetRequiredService<IGlobalHotkeyService>();
        _hotkey.HotkeyPressed += (_, _) => Dispatcher.Invoke(OnTrayShow);
        _hotkey.Configure(prefs.Current.HotkeyCtrl, prefs.Current.HotkeyAlt,
                          prefs.Current.HotkeyShift, prefs.Current.HotkeyWin,
                          prefs.Current.HotkeyKey);

        // When Windows auto-launches us at sign-in we stay quietly in the tray (locked).
        // The user opens the app via the tray icon or global hotkey. Any other launch
        // shows the login window immediately.
        bool launchedAtStartup = e.Args.Contains(StartupService.StartupArg);
        if (!launchedAtStartup)
            ShowLogin();
    }

    private void ApplyPreferences(AppPreferences p)
    {
        var idle = _services?.GetService<IIdleTimerService>();
        if (idle is not null) idle.IdleThreshold = TimeSpan.FromMinutes(Math.Max(1, p.IdleLockMinutes));

        if (_services?.GetService<IClipboardService>() is ClipboardService clip)
            clip.DefaultClearAfter = TimeSpan.FromSeconds(Math.Max(1, p.ClipboardClearSeconds));

        // Reconcile the Windows "launch at startup" entry with the saved preference.
        // Re-applying when enabled also refreshes the recorded exe path if the app moved.
        _services?.GetService<IStartupService>()?.SetEnabled(p.LaunchAtStartup);
    }

    private void OnTrayPreferences()
    {
        // Preferences are gated behind an unlocked vault. The tray item is disabled
        // while locked, but guard here too in case it's reached another way — route
        // the user to the login window instead.
        if (!_services!.GetRequiredService<IVaultService>().IsUnlocked)
        {
            ShowLogin();
            return;
        }

        var dlg = _services!.GetRequiredService<PreferencesWindow>();

        // Owner must be a *different*, visible window. When the app was launched
        // straight to the tray (no login/main window yet) the freshly-created
        // PreferencesWindow can itself be Application.MainWindow — owning itself
        // throws "Cannot set Owner property to itself". A hidden (minimized-to-tray)
        // main window is also a poor owner, so require visibility and fall back to
        // centering on screen.
        var owner = Windows.OfType<Window>().FirstOrDefault(w => w != dlg && w.IsVisible);
        if (owner is not null)
        {
            dlg.Owner = owner;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (dlg.ShowDialog() == true)
        {
            var prefs = _services!.GetRequiredService<IPreferencesService>();
            ApplyPreferences(prefs.Current);
            _hotkey?.Configure(prefs.Current.HotkeyCtrl, prefs.Current.HotkeyAlt,
                               prefs.Current.HotkeyShift, prefs.Current.HotkeyWin,
                               prefs.Current.HotkeyKey);
        }
    }

    private void ShowLogin()
    {
        if (_loginWindow is not null)
        {
            _loginWindow.Activate();
            return;
        }

        _loginWindow = _services!.GetRequiredService<LoginWindow>();
        var vm = (LoginViewModel)_loginWindow.DataContext;
        vm.UnlockSucceeded += (_, _) =>
        {
            _tray?.SetLocked(false);
            ShowMain();
            _loginWindow?.Close();
        };
        _loginWindow.Closed += (_, _) => _loginWindow = null;
        _loginWindow.Show();
    }

    private void ShowMain()
    {
        if (_mainWindow is not null)
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Show();
            _mainWindow.Activate();
            return;
        }

        _mainWindow = _services!.GetRequiredService<MainWindow>();
        var mainVm = (MainViewModel)_mainWindow.DataContext;
        mainVm.LockRequested += (_, _) => Dispatcher.Invoke(OnTrayLock);
        _mainWindow.Closing += OnMainClosing;
        MainWindow = _mainWindow;
        _mainWindow.Show();
    }

    private void OnMainClosing(object? sender, CancelEventArgs e)
    {
        if (_shuttingDown) return;
        // Hide to tray instead of closing.
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void OnTrayShow()
    {
        if (_mainWindow is not null && _services!.GetRequiredService<IVaultService>().IsUnlocked)
        {
            ShowMain();
        }
        else
        {
            ShowLogin();
        }
    }

    private void OnTrayLock()
    {
        var vault = _services!.GetRequiredService<IVaultService>();
        var clip = _services!.GetRequiredService<IClipboardService>();
        clip.ClearIfOurs();
        vault.Lock();
        _tray?.SetLocked(true);

        if (_mainWindow is not null)
        {
            _mainWindow.Closing -= OnMainClosing;
            _mainWindow.Close();
            _mainWindow = null;
        }
        ShowLogin();
    }

    private void Quit()
    {
        _shuttingDown = true;
        if (_mainWindow is not null)
        {
            _mainWindow.Closing -= OnMainClosing;
            _mainWindow.Close();
            _mainWindow = null;
        }
        _loginWindow?.Close();
        _loginWindow = null;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            var vault = _services?.GetService<IVaultService>();
            vault?.Lock();
            var clip = _services?.GetService<IClipboardService>();
            clip?.ClearIfOurs();
            _hotkey?.Dispose();
            _tray?.Dispose();
        }
        catch { /* best effort on shutdown */ }

        _services?.Dispose();
        base.OnExit(e);
    }
}
