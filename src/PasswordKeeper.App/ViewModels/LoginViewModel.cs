using System.Security;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordKeeper.App.Services;

namespace PasswordKeeper.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly IDialogService _dialog;

    [ObservableProperty] private bool isCreateMode;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public event EventHandler? UnlockSucceeded;

    public LoginViewModel(IVaultService vault, IDialogService dialog)
    {
        _vault = vault;
        _dialog = dialog;
        IsCreateMode = !vault.VaultExists;
        StatusMessage = IsCreateMode
            ? "Set a master password to create your vault."
            : "Enter your master password to unlock.";
    }

    [RelayCommand]
    private async Task SubmitAsync(object? parameter)
    {
        if (parameter is not Tuple<SecureString, SecureString> args) return;
        var password = args.Item1;
        var confirmPassword = args.Item2;

        if (password is null || password.Length == 0)
        {
            StatusMessage = "Master password cannot be empty.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = IsCreateMode ? "Creating vault..." : "Unlocking...";

            if (IsCreateMode)
            {
                if (password.Length < 8)
                {
                    StatusMessage = "Master password must be at least 8 characters.";
                    return;
                }
                if (!SecureStringEquals(password, confirmPassword))
                {
                    StatusMessage = "Passwords do not match.";
                    return;
                }

                await _vault.CreateAsync(password);
                UnlockSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                bool ok = await _vault.UnlockAsync(password);
                if (!ok)
                {
                    StatusMessage = "Incorrect master password.";
                    return;
                }
                UnlockSucceeded?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error: " + ex.Message;
            _dialog.ShowError("Vault error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool SecureStringEquals(SecureString a, SecureString b)
    {
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;
        IntPtr pa = IntPtr.Zero, pb = IntPtr.Zero;
        try
        {
            pa = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(a);
            pb = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(b);
            int len = a.Length * 2;
            for (int i = 0; i < len; i++)
            {
                if (System.Runtime.InteropServices.Marshal.ReadByte(pa, i)
                    != System.Runtime.InteropServices.Marshal.ReadByte(pb, i))
                    return false;
            }
            return true;
        }
        finally
        {
            if (pa != IntPtr.Zero) System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(pa);
            if (pb != IntPtr.Zero) System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(pb);
        }
    }
}
