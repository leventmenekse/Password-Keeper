namespace PasswordKeeper.Core.Generators;

public interface IPasswordGenerator
{
    string Generate(PasswordGeneratorOptions options);
}
