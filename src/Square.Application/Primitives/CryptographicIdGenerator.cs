using System.Numerics;
using System.Security.Cryptography;
using Square.Domain.Primitives;

namespace Square.Application.Primitives;

public sealed class CryptographicIdGenerator(IClock clock) : IIdGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public TId New<TId>() where TId : struct, IStrongId<TId>
    {
        Span<byte> bytes = stackalloc byte[16];
        long timestamp = clock.UtcNow.Value.ToUnixTimeMilliseconds();
        if (timestamp < 0 || timestamp > 0x0000_FFFF_FFFF_FFFFL)
            throw new InvalidOperationException("The UTC timestamp is outside the 48-bit sortable ID range.");
        bytes[0] = (byte)(timestamp >> 40);
        bytes[1] = (byte)(timestamp >> 32);
        bytes[2] = (byte)(timestamp >> 24);
        bytes[3] = (byte)(timestamp >> 16);
        bytes[4] = (byte)(timestamp >> 8);
        bytes[5] = (byte)timestamp;
        RandomNumberGenerator.Fill(bytes[6..]);
        return TId.FromCanonical(string.Concat(TId.Prefix, "_", EncodeCrockford(bytes)));
    }

    private static string EncodeCrockford(ReadOnlySpan<byte> bytes)
    {
        BigInteger number = new(bytes, isUnsigned: true, isBigEndian: true);
        Span<char> encoded = stackalloc char[26];
        for (int index = encoded.Length - 1; index >= 0; index--)
        {
            number = BigInteger.DivRem(number, 32, out BigInteger remainder);
            encoded[index] = Alphabet[(int)remainder];
        }
        return new string(encoded);
    }
}
