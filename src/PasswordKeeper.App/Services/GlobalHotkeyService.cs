using System.Runtime.InteropServices;
using WinForms = System.Windows.Forms;

namespace PasswordKeeper.App.Services;

public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const uint MOD_ALT      = 0x0001;
    private const uint MOD_CONTROL  = 0x0002;
    private const uint MOD_SHIFT    = 0x0004;
    private const uint MOD_WIN      = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId  = 0xCAFE;

    private uint _modifiers = MOD_CONTROL | MOD_ALT | MOD_NOREPEAT;
    private uint _vk        = 0x50; // 'P'

    private readonly HotkeyWindow _window;
    private bool _registered;

    public event EventHandler? HotkeyPressed;

    public GlobalHotkeyService()
    {
        _window = new HotkeyWindow(OnHotkey);
    }

    public bool Register()
    {
        if (_registered) return true;
        bool ok = RegisterHotKey(_window.Handle, HotkeyId, _modifiers, _vk);
        _registered = ok;
        return ok;
    }

    public void Unregister()
    {
        if (!_registered) return;
        UnregisterHotKey(_window.Handle, HotkeyId);
        _registered = false;
    }

    /// <summary>
    /// Apply hotkey configuration. Returns true if registered successfully.
    /// Re-registers if already active.
    /// </summary>
    public bool Configure(bool ctrl, bool alt, bool shift, bool win, string key)
    {
        Unregister();
        uint mods = MOD_NOREPEAT;
        if (ctrl)  mods |= MOD_CONTROL;
        if (alt)   mods |= MOD_ALT;
        if (shift) mods |= MOD_SHIFT;
        if (win)   mods |= MOD_WIN;
        if (mods == MOD_NOREPEAT) return false; // require at least one modifier

        uint vk = KeyStringToVk(key);
        if (vk == 0) return false;

        _modifiers = mods;
        _vk = vk;
        return Register();
    }

    private static uint KeyStringToVk(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;
        key = key.Trim().ToUpperInvariant();

        // Function keys F1..F24
        if (key.Length >= 2 && key[0] == 'F' && int.TryParse(key.AsSpan(1), out int n) && n >= 1 && n <= 24)
            return (uint)(0x70 + (n - 1));

        // Single letter A..Z or digit 0..9
        if (key.Length == 1)
        {
            char c = key[0];
            if (c >= 'A' && c <= 'Z') return c;
            if (c >= '0' && c <= '9') return c;
        }
        return 0;
    }

    private void OnHotkey() => HotkeyPressed?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        Unregister();
        _window.DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private sealed class HotkeyWindow : WinForms.NativeWindow
    {
        private readonly Action _onHotkey;

        public HotkeyWindow(Action onHotkey)
        {
            _onHotkey = onHotkey;
            CreateHandle(new WinForms.CreateParams());
        }

        protected override void WndProc(ref WinForms.Message m)
        {
            if (m.Msg == WM_HOTKEY) _onHotkey();
            base.WndProc(ref m);
        }
    }
}
