using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;

namespace PasswordKeeper.App.Services;

public sealed class IdleTimerService : IIdleTimerService, IDisposable
{
    private readonly DispatcherTimer _timer;
    private bool _fired;

    public TimeSpan IdleThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public event EventHandler? IdleThresholdExceeded;

    public IdleTimerService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += OnTick;
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    public void Start()
    {
        _fired = false;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _fired = false;
    }

    public void Reset() => _fired = false;

    private void OnTick(object? sender, EventArgs e)
    {
        if (_fired) return;
        if (GetIdleTime() >= IdleThreshold)
        {
            _fired = true;
            IdleThresholdExceeded?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock ||
            e.Reason == SessionSwitchReason.RemoteDisconnect)
        {
            if (_fired) return;
            _fired = true;
            IdleThresholdExceeded?.Invoke(this, EventArgs.Empty);
        }
    }

    private static TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info)) return TimeSpan.Zero;
        uint ticks = (uint)Environment.TickCount;
        uint idleMs = ticks - info.dwTime;
        return TimeSpan.FromMilliseconds(idleMs);
    }

    public void Dispose()
    {
        _timer.Stop();
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
}
