using System.Security.Cryptography;

namespace PasswordKeeper.Core.Generators;

public sealed class PasswordGenerator : IPasswordGenerator
{
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?/";
    private static readonly HashSet<char> Ambiguous = new("Il1O0|`'\".,;:");

    public string Generate(PasswordGeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Length <= 0) throw new ArgumentException("Length must be positive.", nameof(options));

        var classes = new List<string>();
        if (options.IncludeUppercase) classes.Add(Filter(Upper, options.ExcludeAmbiguous));
        if (options.IncludeLowercase) classes.Add(Filter(Lower, options.ExcludeAmbiguous));
        if (options.IncludeDigits)    classes.Add(Filter(Digits, options.ExcludeAmbiguous));
        if (options.IncludeSymbols)   classes.Add(Filter(Symbols, options.ExcludeAmbiguous));

        if (classes.Count == 0)
            throw new ArgumentException("At least one character class must be enabled.", nameof(options));
        if (options.Length < classes.Count)
            throw new ArgumentException("Length must be at least the number of enabled character classes.", nameof(options));

        string pool = string.Concat(classes);
        var chars = new char[options.Length];

        // Reserve one slot per class to guarantee class coverage
        for (int i = 0; i < classes.Count; i++)
        {
            string cls = classes[i];
            chars[i] = cls[RandomNumberGenerator.GetInt32(0, cls.Length)];
        }

        // Fill the rest from the combined pool
        for (int i = classes.Count; i < options.Length; i++)
        {
            chars[i] = pool[RandomNumberGenerator.GetInt32(0, pool.Length)];
        }

        // Fisher-Yates shuffle so reserved slots aren't always first
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(0, i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static string Filter(string set, bool excludeAmbiguous)
        => excludeAmbiguous ? new string(set.Where(c => !Ambiguous.Contains(c)).ToArray()) : set;
}
