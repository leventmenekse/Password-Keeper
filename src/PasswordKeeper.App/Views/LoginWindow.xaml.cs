using System.Windows;
using PasswordKeeper.App.ViewModels;

namespace PasswordKeeper.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += (_, _) =>
        {
            PasswordBox.Focus();
            SubmitButton.Content = vm.IsCreateMode ? "Create" : "Unlock";
        };
    }

    private async void OnSubmitClick(object sender, RoutedEventArgs e)
    {
        var pw = PasswordBox.SecurePassword;
        var confirm = ConfirmPasswordBox.SecurePassword;
        await _vm.SubmitCommand.ExecuteAsync(Tuple.Create(pw, confirm));
        PasswordBox.Clear();
        ConfirmPasswordBox.Clear();
    }
}
