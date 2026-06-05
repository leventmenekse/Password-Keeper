using System.Drawing;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace PasswordKeeper.App.Services;

public sealed class TrayService : ITrayService
{
    private readonly WinForms.NotifyIcon _icon;
    private readonly WinForms.ToolStripMenuItem _showItem;
    private readonly WinForms.ToolStripMenuItem _lockItem;
    private readonly WinForms.ToolStripMenuItem _prefsItem;

    public event EventHandler? ShowRequested;
    public event EventHandler? LockRequested;
    public event EventHandler? PreferencesRequested;
    public event EventHandler? QuitRequested;

    public TrayService()
    {
        _icon = new WinForms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "PasswordKeeper",
            Visible = false,
        };

        var menu = new WinForms.ContextMenuStrip();
        _showItem = new WinForms.ToolStripMenuItem("Show");
        _showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        _lockItem = new WinForms.ToolStripMenuItem("Lock vault");
        _lockItem.Click += (_, _) => LockRequested?.Invoke(this, EventArgs.Empty);
        _prefsItem = new WinForms.ToolStripMenuItem("Preferences...");
        _prefsItem.Click += (_, _) => PreferencesRequested?.Invoke(this, EventArgs.Empty);
        var quitItem = new WinForms.ToolStripMenuItem("Quit");
        quitItem.Click += (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(_showItem);
        menu.Items.Add(_lockItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(_prefsItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(quitItem);

        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Show() => _icon.Visible = true;
    public void Hide() => _icon.Visible = false;

    public void SetLocked(bool locked)
    {
        _icon.Text = locked ? "PasswordKeeper (locked)" : "PasswordKeeper";
        _lockItem.Enabled = !locked;
        // Preferences require an unlocked vault.
        _prefsItem.Enabled = !locked;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/Icons/PasswordKeeper.ico", UriKind.Absolute);
            var stream = Application.GetResourceStream(uri)?.Stream;
            return stream is not null ? new Icon(stream) : SystemIcons.Shield;
        }
        catch
        {
            return SystemIcons.Shield;
        }
    }
}
