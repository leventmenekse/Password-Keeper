using System.Windows;
using PasswordKeeper.App.ViewModels;

namespace PasswordKeeper.App.Views;

public partial class PreferencesWindow : Window
{
    public PreferencesWindow(PreferencesViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.Applied   += (_, _) => { DialogResult = true;  Close(); };
        vm.Cancelled += (_, _) => { DialogResult = false; Close(); };
    }
}
