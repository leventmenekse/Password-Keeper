using System.Windows;
using PasswordKeeper.App.Views;

namespace PasswordKeeper.App.Services;

public sealed class DialogService : IDialogService
{
    public void ShowError(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string title, string message)
    {
        var dlg = new ConfirmDialog(title, message)
        {
            Owner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive)
                ?? Application.Current?.MainWindow,
        };
        return dlg.ShowDialog() == true;
    }
}
