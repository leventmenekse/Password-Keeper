namespace PasswordKeeper.App.Services;

public interface IClipboardService
{
    void CopySensitive(string value, TimeSpan? clearAfter = null);
    void ClearIfOurs();
}
