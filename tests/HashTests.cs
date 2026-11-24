using System.Security.Cryptography;
using System.Text;

namespace Flappybet.Tests;

public class HashTests
{
	[Fact]
	public void BytesToString_IsLowercaseHex_TwoDigitsPerByte()
	{
		Assert.Equal("", Rng.BytesToString([]));
		Assert.Equal("000fa0ff", Rng.BytesToString([0x00, 0x0f, 0xa0, 0xff]));
		var all = Enumerable.Range(0, 256).Select(b => (byte)b).ToArray();
		Assert.Equal(Convert.ToHexString(all).ToLowerInvariant(), Rng.BytesToString(all));
	}

	[Fact]
	public void ToBytes_IsUtf8_WithNoPrefix()
	{
		Assert.Empty(Rng.ToBytes(""));
		Assert.Equal(new byte[] { 0x61, 0x62, 0x63 }, Rng.ToBytes("abc"));
		// U+10E5, U+20AC, U+1F600: three, three and four bytes
		Assert.Equal(new byte[] { 0xe1, 0x83, 0xa5, 0xe2, 0x82, 0xac, 0xf0, 0x9f, 0x98, 0x80 }, Rng.ToBytes("\u10e5\u20ac\U0001f600"));
	}

	/// <summary>The published SHA-1 test values</summary>
	[Theory]
	[InlineData("", "da39a3ee5e6b4b0d3255bfef95601890afd80709")]
	[InlineData("abc", "a9993e364706816aba3e25717850c26c9cd0d89d")]
	[InlineData("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "84983e441c3bd26ebaae4aa1f95129e5e54670f1")]
	[InlineData("The quick brown fox jumps over the lazy dog", "2fd4e1c67a2d28fced849ee1bb76e7391b93eb12")]
	public void Sha1_GivesThePublishedValues(string text, string expected) => Assert.Equal(expected, Rng.BytesToString(Rng.Sha1(text)));

	[Fact]
	public void RotateHash_IsSha1OfHashFollowedByTheNonceInDecimal()
	{
		for (int i = 0; i < 500; i++)
		{
			string hash = Inputs.Hex("rotate", i, i % 129);
			int nonce = (int)Inputs.Number("rotate nonce", i, int.MinValue, int.MaxValue);
			string expected = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(hash + nonce.ToString(System.Globalization.CultureInfo.InvariantCulture)))).ToLowerInvariant();
			string rotated = Rng.RotateHash(hash, nonce);
			Assert.Equal(expected, rotated);
			Assert.Matches("^[0-9a-f]{40}$", rotated);
		}
	}

	[Fact]
	public void RotateHash_DifferentNonces_GiveDifferentHashes()
	{
		string hash = Inputs.Hex("rotate distinct", 0);
		var seen = new HashSet<string>();
		for (int nonce = 0; nonce < 10000; nonce++)
			Assert.True(seen.Add(Rng.RotateHash(hash, nonce)));
	}

	/// <summary>A rotated hash is long enough for the largest n, and its draws are uniform like any other hash's</summary>
	[Fact]
	public void RotateHash_ChainedDraws_AreUniform()
	{
		var counts = new long[37];
		string hash = Inputs.Hex("rotate chain", 0);
		for (int i = 0; i < 200000; i++)
		{
			hash = Rng.RotateHash(hash, i);
			counts[Rng.Rand(hash, 37)]++;
		}
		Rng.ValidateHexLength(hash, 1L << 60);
		Statistics.AssertUniform(counts);
	}
}
