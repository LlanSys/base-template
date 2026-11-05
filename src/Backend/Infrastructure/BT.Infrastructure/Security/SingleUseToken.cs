using System.Security.Cryptography;
using System.Text;

namespace BT.Infrastructure.Security;

internal static class SingleUseToken
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public static bool Matches(string? hash, string? token) => hash is { Length: 64 } && token is { Length: 64 }
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hash), Encoding.ASCII.GetBytes(Hash(token)));
}
