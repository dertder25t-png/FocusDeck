using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Konscious.Security.Cryptography;

namespace FocusDeck.Shared.Security;

public record SrpKdfParameters
{
    [JsonPropertyName("alg")]
    public string Algorithm { get; }
    [JsonPropertyName("salt")]
    public string SaltBase64 { get; }
    [JsonPropertyName("p")]
    public int DegreeOfParallelism { get; }
    [JsonPropertyName("t")]
    public int Iterations { get; }
    [JsonPropertyName("m")]
    public int MemorySizeKiB { get; }

    [JsonConstructor]
    public SrpKdfParameters(string algorithm, string saltBase64, int degreeOfParallelism, int iterations, int memorySizeKiB)
    {
        Algorithm = algorithm;
        SaltBase64 = saltBase64;
        DegreeOfParallelism = degreeOfParallelism;
        Iterations = iterations;
        MemorySizeKiB = memorySizeKiB;
    }
}


/// <summary>
/// SRP-6a helpers and primitives shared between server and clients.
/// </summary>
public static class Srp
{
    // 2048-bit modulus from RFC 5054 (group 14)
    public const string Algorithm = "SRP-6a-2048-SHA256";

    public const string ModulusHex = "AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC3192943DB56050" +
                                "A37329CBB4A099ED8193E0757767A13DD52312AB4B03310DCD7F48A9DA04FD50" +
                                "E8083969EDB767B0CF6096A4FA3B58F90F6A54B42A59D53B3A2A7C5F4F5F4E46" +
                                "2E9F6A4E128E71B9F0C67C8E18CBF4C3BAFE8A31C5CFFFB4E90D54BD45BF37DF" +
                                "365C1A65E68CFDA76D4DA708DF1FB2BC2E4A4371";

    private static readonly BigInteger NValue = BigInteger.Parse(ModulusHex, System.Globalization.NumberStyles.HexNumber);
    private static readonly BigInteger GValue = new(2);
    private static readonly int PadLength = (int)(((long)NValue.GetBitLength() + 7L) / 8L);
    private static readonly BigInteger KValue = HashToInteger(Pad(NValue), Pad(GValue));

    /// <summary>Gets SRP modulus N.</summary>
    public static BigInteger N => NValue;

    /// <summary>Gets SRP generator g.</summary>
    public static BigInteger G => GValue;

    /// <summary>Gets SRP multiplier parameter k.</summary>
    public static BigInteger K => KValue;

    /// <summary>
    /// Generates random salt bytes.
    /// </summary>
    public static byte[] GenerateSalt(int length = 16)
    {
        var salt = new byte[length];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public static SrpKdfParameters GenerateKdfParameters()
    {
        var salt = GenerateSalt(16);
        return new SrpKdfParameters("argon2id", Convert.ToBase64String(salt), 2, 3, 65536);
    }

    /// <summary>
    /// Computes SRP private key x using Argon2id.
    /// </summary>
    public static BigInteger ComputePrivateKey(SrpKdfParameters kdf, string userId, string password)
    {
        var salt = Convert.FromBase64String(kdf.SaltBase64);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt = salt,
            DegreeOfParallelism = kdf.DegreeOfParallelism,
            Iterations = kdf.Iterations,
            MemorySize = kdf.MemorySizeKiB,
            AssociatedData = Encoding.UTF8.GetBytes(userId)
        };

        var hash = argon2.GetBytes(32);
        return HashBytesToInteger(hash);
    }


    /// <summary>
    /// Computes SRP private key x = H(salt || H(userId ":" password)).
    /// </summary>
    public static BigInteger ComputePrivateKey(byte[] salt, string userId, string password)
    {
        using var sha = SHA256.Create();
        var userPass = Encoding.UTF8.GetBytes($"{userId}:{password}");
        var inner = sha.ComputeHash(userPass);

        var combined = new byte[salt.Length + inner.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(inner, 0, combined, salt.Length, inner.Length);

        var digest = sha.ComputeHash(combined);
        return HashBytesToInteger(digest);
    }

    /// <summary>
    /// Computes verifier v = g^x mod N.
    /// </summary>
    public static BigInteger ComputeVerifier(BigInteger privateKey)
    {
        return BigInteger.ModPow(GValue, privateKey, NValue);
    }

    /// <summary>
    /// Generates random client ephemeral values (a, A).
    /// </summary>
    public static (BigInteger Secret, BigInteger Public) GenerateClientEphemeral()
    {
        var secret = GenerateRandomBigInteger();
        var pub = BigInteger.ModPow(GValue, secret, NValue);
        return (secret, pub);
    }

    /// <summary>
    /// Generates random server ephemeral values (b, B).
    /// </summary>
    public static (BigInteger Secret, BigInteger Public) GenerateServerEphemeral(BigInteger verifier)
    {
        var secret = GenerateRandomBigInteger();
        var gb = BigInteger.ModPow(GValue, secret, NValue);
        var serverPublic = (KValue * verifier + gb) % NValue;
        if (serverPublic.Sign < 0)
        {
            serverPublic += NValue;
        }
        return (secret, serverPublic);
    }

    /// <summary>
    /// Computes scramble parameter u = H(PAD(A) || PAD(B)).
    /// </summary>
    public static BigInteger ComputeScramble(BigInteger clientPublic, BigInteger serverPublic)
    {
        var uBytes = Hash(Pad(clientPublic), Pad(serverPublic));
        return HashBytesToInteger(uBytes);
    }

    /// <summary>
    /// Computes client session secret S.
    /// </summary>
    public static BigInteger ComputeClientSession(BigInteger serverPublic, BigInteger privateKeyX, BigInteger clientSecret, BigInteger scramble)
    {
        var gx = BigInteger.ModPow(GValue, privateKeyX, NValue);
        var tmp = serverPublic - KValue * gx;
        tmp %= NValue;
        if (tmp.Sign < 0) tmp += NValue;

        var exponent = clientSecret + scramble * privateKeyX;
        exponent %= NValue;
        if (exponent.Sign < 0) exponent += NValue;

        return BigInteger.ModPow(tmp, exponent, NValue);
    }

    /// <summary>
    /// Computes server session secret S.
    /// </summary>
    public static BigInteger ComputeServerSession(BigInteger clientPublic, BigInteger verifier, BigInteger serverSecret, BigInteger scramble)
    {
        var vu = BigInteger.ModPow(verifier, scramble, NValue);
        var tmp = (clientPublic * vu) % NValue;
        if (tmp.Sign < 0) tmp += NValue;
        return BigInteger.ModPow(tmp, serverSecret, NValue);
    }

    /// <summary>
    /// Computes shared session key K = H(PAD(S)).
    /// </summary>
    public static byte[] ComputeSessionKey(BigInteger session)
    {
        return Hash(Pad(session));
    }

    /// <summary>
    /// Computes client proof M1 = H(PAD(A) || PAD(B) || K).
    /// </summary>
    public static byte[] ComputeClientProof(BigInteger clientPublic, BigInteger serverPublic, byte[] sessionKey)
    {
        return Hash(Pad(clientPublic), Pad(serverPublic), sessionKey);
    }

    /// <summary>
    /// Computes server proof M2 = H(PAD(A) || M1 || K).
    /// </summary>
    public static byte[] ComputeServerProof(BigInteger clientPublic, byte[] clientProof, byte[] sessionKey)
    {
        return Hash(Pad(clientPublic), clientProof, sessionKey);
    }

    /// <summary>
    /// Converts big integer to unsigned big-endian byte array padded to modulus length.
    /// </summary>
    public static byte[] ToBigEndian(BigInteger value)
    {
        return value.ToByteArray(isUnsigned: true, isBigEndian: true);
    }

    /// <summary>
    /// Parses unsigned big-endian bytes into BigInteger.
    /// </summary>
    public static BigInteger FromBigEndian(ReadOnlySpan<byte> bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    /// <summary>
    /// Validates public ephemeral is within range (non-zero mod N).
    /// </summary>
    public static bool IsValidPublicEphemeral(BigInteger value)
    {
        return value > 0 && value % NValue != 0;
    }

    private static BigInteger GenerateRandomBigInteger()
    {
        var bytes = new byte[PadLength];
        BigInteger value;
        do
        {
            RandomNumberGenerator.Fill(bytes);
            value = FromBigEndian(bytes);
        } while (value.Sign <= 0 || value >= NValue);

        return value;
    }

    private static byte[] Pad(BigInteger value)
    {
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (bytes.Length == PadLength) return bytes;
        if (bytes.Length > PadLength)
        {
            var trimmed = new byte[PadLength];
            Buffer.BlockCopy(bytes, bytes.Length - PadLength, trimmed, 0, PadLength);
            return trimmed;
        }
        var padded = new byte[PadLength];
        Buffer.BlockCopy(bytes, 0, padded, PadLength - bytes.Length, bytes.Length);
        return padded;
    }

    private static byte[] Hash(params byte[][] inputs)
    {
        using var sha = SHA256.Create();
        foreach (var input in inputs)
        {
            sha.TransformBlock(input, 0, input.Length, null, 0);
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return sha.Hash ?? Array.Empty<byte>();
    }

    private static BigInteger HashToInteger(params byte[][] inputs)
    {
        var digest = Hash(inputs);
        return HashBytesToInteger(digest);
    }

    private static BigInteger HashBytesToInteger(byte[] digest)
    {
        return new BigInteger(digest, isUnsigned: true, isBigEndian: true);
    }
}
