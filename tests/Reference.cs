using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Flappybet.Tests;

/// <summary>The algorithm of README.md written a second time, in BigInteger arithmetic, sharing no code with Rng.cs.
/// The tests compare the library against it. reference.py is a third implementation, in Python, and the source of the known answers.</summary>
internal static class Reference
{
	public static string DeriveHash(string seed, int index) =>
		Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed.ToLowerInvariant() + ":" + index))).ToLowerInvariant();

	/// <summary>The result, and how many draws were rejected before it</summary>
	public static (long Result, int Rejected) Rand(string hex, long n)
	{
		int digits = Math.Min(hex.Length, 15);
		BigInteger span = BigInteger.Pow(16, digits);
		string draw = hex;
		for (int attempt = 1; ; attempt++)
		{
			BigInteger scaled = BigInteger.Parse("0" + draw[..digits], NumberStyles.HexNumber) * n;
			if (scaled % span >= span % n)
				return ((long)(scaled / span), attempt - 1);
			draw = DeriveHash(hex, attempt);
		}
	}

	public static List<T> Shuffle<T>(IEnumerable<T> items, string hash)
	{
		var copy = items.ToList();
		for (int i = copy.Count - 1; i >= 1; i--)
		{
			int j = (int)Rand(DeriveHash(hash + ":shuffle", i), i + 1).Result;
			(copy[i], copy[j]) = (copy[j], copy[i]);
		}
		return copy;
	}
}

/// <summary>Test inputs that are the same on every run and every machine: SHA-256 of a series name and a counter.
/// System.Random is not used, so the inputs do not depend on the .NET version.</summary>
internal static class Inputs
{
	public static string Hex(string series, long i, int length = 64)
	{
		var sb = new StringBuilder();
		for (int block = 0; sb.Length < length; block++)
			sb.Append(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"rng-tests:{series}:{i}:{block}"))));
		return sb.ToString(0, length);
	}

	/// <summary>A value from min to max inclusive</summary>
	public static long Number(string series, long i, long min, long max) =>
		min + (long)(ulong.Parse(Hex(series, i, 16), NumberStyles.AllowHexSpecifier) % (ulong)(max - min + 1));
}

internal static class Statistics
{
	/// <summary>Pearson chi-square of the counts against equal expected counts</summary>
	public static double ChiSquare(IReadOnlyCollection<long> observed)
	{
		double expected = (double)observed.Sum() / observed.Count;
		return observed.Sum(count => (count - expected) * (count - expected) / expected);
	}

	/// <summary>Standard normal quantiles: 99.9%, and 1 - 0.001 / 52 for the largest of 52 tests</summary>
	public const double Z999 = 3.0902,
		Z999Of52 = 4.1165;

	/// <summary>The value a chi-square statistic stays below 999 times in 1000 when the outcomes really are equally likely
	/// (Wilson-Hilferty approximation of the quantile). The inputs are fixed, so a test either always passes or always fails.</summary>
	public static double Critical(int degreesOfFreedom, double z = Z999)
	{
		double a = 2.0 / (9.0 * degreesOfFreedom);
		return degreesOfFreedom * Math.Pow(1 - a + z * Math.Sqrt(a), 3);
	}

	public static void AssertUniform(IReadOnlyCollection<long> observed)
	{
		double chiSquare = ChiSquare(observed);
		double critical = Critical(observed.Count - 1);
		Assert.True(chiSquare < critical, $"chi-square {chiSquare:F2} is not below {critical:F2} ({observed.Count - 1} degrees of freedom)");
	}
}
