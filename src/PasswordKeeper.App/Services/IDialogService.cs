namespace PasswordKeeper.App.Services;

public interface IDialogService
{
    void ShowError(string title, string message);
    bool Confirm(string title, string message);
}
