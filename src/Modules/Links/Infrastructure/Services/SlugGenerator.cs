using System.Security.Cryptography;

namespace Vitarish.Links.Infrastructure.Services;

/// <summary>
/// Generates cryptographically random Base62 slugs for short links.
/// </summary>
[RegisterTransient]
internal sealed class SlugGenerator
{
    private const string Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int SlugLength = 8;

    public string Generate()
    {
        char[] chars = new char[SlugLength];
        for (int i = 0; i < SlugLength; i++)
        {
            chars[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];
        }

        return new string(chars);
    }
}
