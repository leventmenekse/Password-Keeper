namespace PasswordKeeper.App.Services;

public interface IIdleTimerService
{
    TimeSpan IdleThreshold { get; set; }
    event EventHandler? IdleThresholdExceeded;
    void Start();
    void Stop();
    void Reset();
}
