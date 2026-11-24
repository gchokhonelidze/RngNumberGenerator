using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Flappybet;

public static class Rng
{
	public static string BytesToString(byte[] bytes)
	{
		var sb = new StringBuilder();
		foreach (byte b in bytes)
			sb.Append(b.ToString("x2"));
		return sb.ToString();
	}

	/// <summary>UTF-8 bytes of the string</summary>
	public static byte[] ToBytes(string s) => Encoding.UTF8.GetBytes(s);

	public static byte[] Sha1(string s) => SHA1.HashData(ToBytes(s));

	public static void ValidateHexLength(string hex, long n)
	{
		long minRequiredLength = n switch
		{
			<= 1 => throw new ArgumentOutOfRangeException(nameof(n)),
			<= 16 => 1,
			<= 256 => 2,
			<= 4096 => 3,
			<= 65536 => 4,
			<= 1048576 => 5,
			<= 16777216 => 6,
			<= 268435456 => 7,
			<= 4294967296 => 8,
			<= 68719476736 => 9,
			<= 1099511627776 => 10,
			<= 17592186044416 => 11,
			<= 281474976710656 => 12,
			<= 4503599627370496 => 13,
			<= 72057594037927936 => 14,
			<= 1152921504606846976 => 15,
			_ => throw new OverflowException(nameof(n))
		};
		if (hex.Length < minRequiredLength)
			throw new ArgumentException($"hex length is not enough to make symmetrical randomization spread for {n} outcomes");
	}

	/// <summary>Returns random int result from (0) to (n-1) inclusive. Same draw as RandLong</summary>
	public static int Rand(string hex, int n) => (int)RandLong(hex, n);

	/// <summary>Returns random long result from (0) to (n-1) inclusive, every outcome equally likely.
	/// The first L = min(hex.Length, 15) hex digits are read as an integer v in 0..16^L-1 and scaled to floor(v * n / 16^L) in exact integer arithmetic.
	/// A draw is rejected when (v * n) mod 16^L is below 16^L mod n, which leaves exactly floor(16^L / n) accepted values of v per outcome.
	/// A rejected draw is replaced by the first L hex digits of DeriveHash(hex, attempt), attempt = 1, 2, ... so the result depends on hex and n only.</summary>
	public static long RandLong(string hex, long n)
	{
		ValidateHexLength(hex, n);
		int digits = Math.Min(hex.Length, MaxHexDigits);
		int bits = digits * 4;
		ulong range = 1UL << bits; //16^digits, at most 2^60
		ulong outcomes = (ulong)n;
		ulong threshold = range % outcomes;
		string draw = hex;
		for (int attempt = 1; ; attempt++)
		{
			ulong v = ulong.Parse(draw.AsSpan(0, digits), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
			UInt128 scaled = (UInt128)v * outcomes; //below 2^120
			if ((ulong)(scaled & (range - 1)) >= threshold)
				return (long)(scaled >> bits);
			draw = DeriveHash(hex, attempt);
		}
	}

	/// <summary>Max hex digits read per draw: 60 bits</summary>
	private const int MaxHexDigits = 15;

	/// <summary>Hex of SHA-256 over UTF-8 of "{seed in lowercase}:{index}". Its own derivation, so it never repeats a RotateHash value</summary>
	private static string DeriveHash(string seed, int index) =>
		Convert.ToHexStringLower(SHA256.HashData(ToBytes($"{seed.ToLowerInvariant()}:{index}")));

	public static string RotateHash(string hash, int nonce) => BytesToString(Sha1($"{hash}{nonce}"));

	/// <summary>Shuffles array using Fisher-Yates algorithm, every ordering equally likely. Does not mutate input array.
	/// For i = last index down to 1, item i is swapped with item j = Rand(DeriveHash(hash + ":shuffle", i), i + 1). The whole hash is the seed.</summary>
	public static IList<T> Shuffle<T>(IList<T> arr, string hash)
	{
		IList<T> copy = [.. arr];
		for (int i = copy.Count - 1; i > 0; i--)
		{
			int j = Rand(DeriveHash($"{hash}:shuffle", i), i + 1);
			(copy[i], copy[j]) = (copy[j], copy[i]);
		}
		return copy;
	}
}
