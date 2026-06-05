namespace PasswordKeeper.Core.Models;

public sealed class KdfParameters
{
    public string Algo { get; set; } = "argon2id";
    public int M { get; set; } = 131_072;
    public int T { get; set; } = 3;
    public int P { get; set; } = 4;
    public string SaltB64 { get; set; } = string.Empty;
}
