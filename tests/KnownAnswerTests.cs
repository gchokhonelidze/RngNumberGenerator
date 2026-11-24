namespace Flappybet.Tests;

/// <summary>Fixed inputs with the answers the Python implementation in reference.py gives (python tests/reference.py prints these tables).
/// The library and the C# reference must both agree with them, so three implementations in two languages produce the same values.</summary>
public class KnownAnswerTests
{
	private const string DocHash = "cba77dd118605e6c6c8bd1807dc6f6ff2b79567f7e49e05fd96fcd25ec63443e";
	private const string LongHash = DocHash + "409a88302ce2d30f1d2e0d840dc1851eed68bec38c3963ba4fc10d3d66a9e900";

	/// <summary>hex, n, expected. The trailing comment is the number of rejected draws before the result</summary>
	public static TheoryData<string, long, long> RandVectors =>
		new()
		{
			// a full hash, the usual call: the first 15 digits are read
			{ DocHash, 2L, 1L }, // 0
			{ DocHash, 6L, 4L }, // 0
			{ DocHash, 37L, 29L }, // 0
			{ DocHash, 1000000L, 795524L }, // 0
			{ DocHash, 2147483647L, 1708375783L }, // 0
			{ DocHash, 4503599627370496L, 3582723693315589L }, // 0
			{ DocHash, 576460752303435833L, 458588632744405327L }, // 0
			{ DocHash, 1152921504606846976L, 917177265488791014L }, // 0
			{ LongHash, 52L, 41L }, // 0
			{ DocHash.ToUpperInvariant(), 37L, 29L }, // 0
			// short slices
			{ "0", 2L, 0L }, // 0
			{ "f", 16L, 15L }, // 0
			{ "7", 9L, 3L }, // 0
			{ "0a3f", 2L, 0L }, // 0
			{ "0a3f", 6L, 0L }, // 0
			{ "ffff", 6L, 5L }, // 0
			{ "8000", 65536L, 32768L }, // 0
			{ "c5", 100L, 76L }, // 0
			// 13, 14 and 15 digits, where 1.0.2 rounded
			{ "fffffffffffff", 4503599627370496L, 4503599627370495L }, // 0
			{ "ffffffffffffff", 72057594037927936L, 72057594037927935L }, // 0
			{ "fffffffffffffff", 1152921504606846976L, 1152921504606846975L }, // 0
			{ "123456789abcd", 1000000000000L, 71111111111L }, // 0
			{ "123456789abcde", 1000000000000000L, 71111111111111L }, // 0
			{ "123456789abcdef", 1000000000000000000L, 71111111111111110L }, // 0
			{ "fedcba987654321", 999999999999999989L, 995555555555555544L }, // 0
			// first draw rejected: the result comes from a retry
			{ "0", 3L, 2L }, // 1
			{ "0", 7L, 6L }, // 1
			{ "00", 255L, 70L }, // 1
			{ "0000", 1000L, 58L }, // 1
			{ "0000", 33000L, 1946L }, // 1
			{ "0000000000000", 4503599627370495L, 769642368707989L }, // 1
			{ "00000000000000", 72057594037927935L, 39018996478726843L }, // 1
			{ "000000000000000", 576460752303423489L, 176086650682797598L }, // 2
			{ "000000000000000abc", 576460752303423489L, 27257412239996856L }, // 2
			{ "000000000000000ABC", 576460752303423489L, 27257412239996856L }, // 2
			{ "000000000000000abd", 576460752303423489L, 40341292795696464L }, // 1
			{ "02", 129L, 76L }, // 3
			{ "26", 129L, 46L }, // 6
			{ "43", 130L, 107L }, // 16
		};

	[Theory]
	[MemberData(nameof(RandVectors))]
	public void Rand_GivesTheKnownAnswer(string hex, long n, long expected)
	{
		Assert.Equal(expected, Rng.RandLong(hex, n));
		if (n <= int.MaxValue)
			Assert.Equal(expected, Rng.Rand(hex, (int)n));
		Assert.Equal(expected, Reference.Rand(hex, n).Result);
	}

	public static TheoryData<string, int, string> RotateHashVectors =>
		new()
		{
			{ "ab", 1, "e175ea4ce0a553260a14bc5e922a935b40425c1e" },
			{ "", 0, "b6589fc6ab0dc82cf12099d1c2d40ab994e8410c" },
			{ "abc", -7, "8cbb94eb889c58b7b9d5ee8d99594f737325ae54" },
			{ DocHash, 0, "7c9fb7285799beda38ce00bfffd9a621c339fc46" },
			{ DocHash, 123456, "ede006b02111d70b699eb19de893a3828efc7295" },
			{ "lucky_pegs.gold.1718876951792", 7001, "9ff59d33ee68c8e4eb0efc6ad35bc7a863764a60" },
			{ DocHash, 2147483647, "95047f748872a33332e23132daf7f72c4f2974ee" },
			{ DocHash, -2147483648, "c057dbb07bba4112982c535a794158dfc7c2ffb5" },
		};

	[Theory]
	[MemberData(nameof(RotateHashVectors))]
	public void RotateHash_GivesTheKnownAnswer(string hash, int nonce, string expected) => Assert.Equal(expected, Rng.RotateHash(hash, nonce));

	/// <summary>count, hash, expected order of the numbers 0..count-1</summary>
	public static TheoryData<int, string, string> ShuffleVectors =>
		new()
		{
			{ 2, "00", "1,0" },
			{ 3, DocHash, "2,0,1" },
			{ 5, DocHash, "3,0,1,2,4" },
			{ 10, DocHash, "3,7,8,2,4,1,5,0,9,6" },
			{ 10, DocHash.ToUpperInvariant(), "3,7,8,2,4,1,5,0,9,6" },
			{ 10, "seed that is not hex", "9,5,4,1,2,6,7,3,0,8" },
			{ 10, "", "7,2,6,5,8,0,3,9,1,4" },
			{
				52,
				DocHash,
				"38,31,10,28,4,35,12,22,46,24,8,19,39,26,23,34,45,1,7,6,27,49,13,17,33,44,43,2,47,18,30,25,21,42,41,14,29,51,3,40,5,20,11,50,36,16,9,15,37,48,32,0"
			},
			{
				52,
				LongHash,
				"3,33,36,47,50,29,39,38,43,10,51,49,15,0,45,23,8,16,35,46,30,37,44,31,6,28,40,25,2,5,34,12,22,20,4,18,7,21,26,13,24,42,32,14,41,1,27,17,19,48,11,9"
			},
		};

	[Theory]
	[MemberData(nameof(ShuffleVectors))]
	public void Shuffle_GivesTheKnownAnswer(int count, string hash, string expected)
	{
		var numbers = Enumerable.Range(0, count).ToList();
		Assert.Equal(expected, string.Join(",", Rng.Shuffle(numbers, hash)));
		Assert.Equal(expected, string.Join(",", Reference.Shuffle(numbers, hash)));
	}
}
