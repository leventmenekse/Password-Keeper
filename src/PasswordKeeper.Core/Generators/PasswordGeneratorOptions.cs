namespace PasswordKeeper.Core.Generators;

public sealed class PasswordGeneratorOptions
{
    public int Length { get; set; } = 20;
    public bool IncludeUppercase { get; set; } = true;
    public bool IncludeLowercase { get; set; } = true;
    public bool IncludeDigits { get; set; } = true;
    public bool IncludeSymbols { get; set; } = true;
    public bool ExcludeAmbiguous { get; set; } = true;
}
