using System.Windows;

namespace PasswordKeeper.App.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string confirmText = "Delete", string cancelText = "Cancel")
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmBtn.Content = confirmText;
        CancelBtn.Content = cancelText;
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
