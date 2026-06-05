using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordKeeper.App.Services;
using PasswordKeeper.Core.Generators;

namespace PasswordKeeper.App.ViewModels;

public partial class PasswordGeneratorViewModel : ObservableObject
{
    private readonly IPasswordGenerator _generator;
    private readonly IClipboardService _clipboard;

    [ObservableProperty] private int length = 20;
    [ObservableProperty] private bool includeUppercase = true;
    [ObservableProperty] private bool includeLowercase = true;
    [ObservableProperty] private bool includeDigits = true;
    [ObservableProperty] private bool includeSymbols = true;
    [ObservableProperty] private bool excludeAmbiguous = true;
    [ObservableProperty] private string generatedPassword = string.Empty;

    public PasswordGeneratorViewModel(IPasswordGenerator generator, IClipboardService clipboard)
    {
        _generator = generator;
        _clipboard = clipboard;
        GenerateCommand.Execute(null);
    }

    [RelayCommand]
    private void Generate()
    {
        try
        {
            GeneratedPassword = _generator.Generate(new PasswordGeneratorOptions
            {
                Length = Length,
                IncludeUppercase = IncludeUppercase,
                IncludeLowercase = IncludeLowercase,
                IncludeDigits = IncludeDigits,
                IncludeSymbols = IncludeSymbols,
                ExcludeAmbiguous = ExcludeAmbiguous,
            });
        }
        catch (Exception ex)
        {
            GeneratedPassword = "(" + ex.Message + ")";
        }
    }

    [RelayCommand]
    private void Copy()
    {
        if (!string.IsNullOrEmpty(GeneratedPassword))
            _clipboard.CopySensitive(GeneratedPassword);
    }
}
