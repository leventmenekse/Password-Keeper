using PasswordKeeper.Core.Generators;

namespace PasswordKeeper.Tests.Generators;

public class PasswordGeneratorTests
{
    [Fact]
    public void Generates_password_of_requested_length()
    {
        var gen = new PasswordGenerator();
        var pw = gen.Generate(new PasswordGeneratorOptions { Length = 24 });
        Assert.Equal(24, pw.Length);
    }

    [Fact]
    public void Includes_all_enabled_classes()
    {
        var gen = new PasswordGenerator();
        var opts = new PasswordGeneratorOptions
        {
            Length = 40,
            IncludeUppercase = true,
            IncludeLowercase = true,
            IncludeDigits = true,
            IncludeSymbols = true,
        };

        for (int i = 0; i < 50; i++)
        {
            var pw = gen.Generate(opts);
            Assert.Contains(pw, char.IsUpper);
            Assert.Contains(pw, char.IsLower);
            Assert.Contains(pw, char.IsDigit);
            Assert.Contains(pw, c => !char.IsLetterOrDigit(c));
        }
    }

    [Fact]
    public void Excludes_ambiguous_characters_when_requested()
    {
        var gen = new PasswordGenerator();
        var opts = new PasswordGeneratorOptions { Length = 60, ExcludeAmbiguous = true };
        var ambiguous = new HashSet<char>("Il1O0|`'\".,;:");

        for (int i = 0; i < 20; i++)
        {
            var pw = gen.Generate(opts);
            foreach (var c in pw)
                Assert.DoesNotContain(c, ambiguous);
        }
    }

    [Fact]
    public void Throws_when_no_class_enabled()
    {
        var gen = new PasswordGenerator();
        var opts = new PasswordGeneratorOptions
        {
            Length = 10,
            IncludeUppercase = false,
            IncludeLowercase = false,
            IncludeDigits = false,
            IncludeSymbols = false,
        };
        Assert.Throws<ArgumentException>(() => gen.Generate(opts));
    }

    [Fact]
    public void Throws_when_length_below_class_count()
    {
        var gen = new PasswordGenerator();
        var opts = new PasswordGeneratorOptions { Length = 2 }; // 4 classes default
        Assert.Throws<ArgumentException>(() => gen.Generate(opts));
    }

    [Fact]
    public void Generated_passwords_are_not_repeated()
    {
        var gen = new PasswordGenerator();
        var opts = new PasswordGeneratorOptions { Length = 20 };
        var seen = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            Assert.True(seen.Add(gen.Generate(opts)));
        }
    }
}
