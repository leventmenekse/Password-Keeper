using System.Windows;
using System.Windows.Threading;

namespace PasswordKeeper.App.Services;

public sealed class ClipboardService : IClipboardService
{
    public TimeSpan DefaultClearAfter { get; set; } = TimeSpan.FromSeconds(15);

    private string? _lastCopied;
    private DispatcherTimer? _timer;

    public void CopySensitive(string value, TimeSpan? clearAfter = null)
    {
        if (string.IsNullOrEmpty(value)) return;

        Clipboard.SetText(value);
        _lastCopied = value;

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = clearAfter ?? DefaultClearAfter };
        _timer.Tick += (_, _) =>
        {
            _timer?.Stop();
            ClearIfOurs();
        };
        _timer.Start();
    }

    public void ClearIfOurs()
    {
        try
        {
            if (_lastCopied is null) return;
            string current = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
            if (current == _lastCopied)
            {
                Clipboard.Clear();
            }
            _lastCopied = null;
        }
        catch
        {
            // Clipboard access can fail transiently; ignore.
        }
    }
}
