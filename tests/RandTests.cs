using System.Collections.Concurrent;

namespace Flappybet.Tests;

public class RandTests
{
	// ------------------------------------------------------------------ exactness

	public static TheoryData<int> AllDigitCounts => [.. Enumerable.Range(1, 15)];

	/// <summary>With n = 16^L every draw is accepted and the result must be the input itself. Any rounding shows up here:
	/// 1.0.2 was inexact for 13, 14 and 15 digits.</summary>
	[Theory]
	[MemberData(nameof(AllDigitCounts))]
	public void RandLong_WithNEqualToTheInputRange_ReturnsTheInputExactly(int digits)
	{
		long span = 1L << (4 * digits);
		var values = new List<long> { 0, 1, 2, span / 2 - 1, span / 2, span / 2 + 1, span - 2, span - 1 };
		for (int i = 0; i < 500; i++)
			values.Add(Inputs.Number($"identity{digits}", i, 0, span - 1));
		foreach (long v in values)
			Assert.Equal(v, Rng.RandLong(v.ToString("x" + digits), span));
	}

	/// <summary>1.0.2 returned 72057594037927971 here, 36 above the largest allowed result</summary>
	[Fact]
	public void RandLong_FourteenDigits_TopInput_StaysInRange()
	{
		const long n = 72057594037927936; //16^14
		Assert.Equal(n - 1, Rng.RandLong("ffffffffffffff", n));
	}

	[Fact]
	public void Rand_LargestInt_TopInput_StaysInRange() => Assert.Equal(int.MaxValue - 1, Rng.Rand(new string('f', 40), int.MaxValue));

	/// <summary>The crash game's call: 15 digits scaled to 16^13 outcomes is the input without its last two hex digits</summary>
	[Fact]
	public void RandLong_SixteenToTheThirteenth_OnFifteenDigits_DropsTheLastTwoDigits()
	{
		for (int i = 0; i < 1000; i++)
		{
			string hash = Inputs.Hex("crash", i);
			Assert.Equal(long.Parse(hash[..13], System.Globalization.NumberStyles.HexNumber), Rng.RandLong(hash, 4503599627370496));
		}
	}

	// ------------------------------------------------------------------ exact uniformity

	/// <summary>Every possible input of 1, 2 and 3 hex digits against every n those digits support.
	/// For each n: the accepted inputs give each outcome exactly floor(16^L / n) times, each accepted input returns floor(v * n / 16^L),
	/// fewer than half the inputs are rejected, and each rejected input returns what the reference gets from its retries.</summary>
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void EveryInput_EveryN_AcceptedDrawsGiveEachOutcomeEquallyOften(int digits)
	{
		int span = 1 << (4 * digits);
		CheckWholeInputSpace(digits, Enumerable.Range(2, span - 1));
	}

	[Fact]
	public void EveryFourDigitInput_SelectedN_AcceptedDrawsGiveEachOutcomeEquallyOften() =>
		CheckWholeInputSpace(4, [2, 3, 4, 6, 7, 10, 37, 52, 100, 1000, 4097, 32768, 32769, 33000, 40000, 65535, 65536]);

	private static void CheckWholeInputSpace(int digits, IEnumerable<int> outcomeCounts)
	{
		long span = 1L << (4 * digits);
		string format = "x" + digits;
		var failures = new ConcurrentQueue<string>();
		void Fail(string message)
		{
			if (failures.Count < 20)
				failures.Enqueue(message);
		}

		Parallel.ForEach(
			outcomeCounts,
			n =>
			{
				var accepted = new long[n];
				long rejected = 0;
				long threshold = span % n;
				for (long v = 0; v < span; v++)
				{
					string hex = v.ToString(format);
					long result = Rng.RandLong(hex, n);
					if (result < 0 || result >= n)
					{
						Fail($"n={n} hex={hex}: {result} is out of range");
						continue;
					}
					if (Rng.Rand(hex, n) != result)
						Fail($"n={n} hex={hex}: Rand and RandLong differ");
					long scaled = v * n;
					if (scaled % span >= threshold)
					{
						accepted[result]++;
						if (result != scaled / span)
							Fail($"n={n} hex={hex}: accepted draw gave {result}, expected {scaled / span}");
					}
					else
					{
						rejected++;
						long expected = Reference.Rand(hex, n).Result;
						if (result != expected)
							Fail($"n={n} hex={hex}: rejected draw gave {result}, expected {expected}");
					}
				}
				long each = span / n;
				for (int outcome = 0; outcome < n; outcome++)
					if (accepted[outcome] != each)
					{
						Fail($"n={n}: outcome {outcome} has {accepted[outcome]} accepted inputs, expected {each}");
						break;
					}
				if (rejected != threshold)
					Fail($"n={n}: {rejected} inputs rejected, expected {threshold}");
				if (rejected * 2 >= span)
					Fail($"n={n}: {rejected} of {span} inputs rejected, expected fewer than half");
			}
		);
		Assert.Empty(failures);
	}

	// ------------------------------------------------------------------ against the reference, longer inputs

	public static TheoryData<int> LongDigitCounts => [.. Enumerable.Range(5, 11)];

	[Theory]
	[MemberData(nameof(LongDigitCounts))]
	public void MatchesTheReference_AndStaysInRange(int digits)
	{
		long span = 1L << (4 * digits);
		var outcomeCounts = new List<long> { 2, 3, 6, 37, 52, 1000000, span, span - 1, span - 2, span / 2, span / 2 + 1, span / 2 - 1, span / 3, span / 3 + 1, span / 16 + 1 };
		for (int i = 0; i < 100; i++)
			outcomeCounts.Add(Inputs.Number($"n{digits}", i, 2, span));
		var edges = new[]
		{
			new string('0', digits),
			new string('f', digits),
			new string('F', digits),
			"8" + new string('0', digits - 1),
			"7" + new string('f', digits - 1),
			new string('0', digits - 1) + "1"
		};
		foreach (long n in outcomeCounts)
		{
			var inputs = new List<string>(edges);
			for (int i = 0; i < 150; i++)
				inputs.Add(Inputs.Hex($"in{digits}:{n}", i, digits));
			foreach (string hex in inputs)
			{
				long result = Rng.RandLong(hex, n);
				Assert.InRange(result, 0, n - 1);
				Assert.Equal(Reference.Rand(hex, n).Result, result);
				if (n <= int.MaxValue)
					Assert.Equal(result, Rng.Rand(hex, (int)n));
			}
		}
	}

	[Theory]
	[InlineData(16)]
	[InlineData(40)]
	[InlineData(64)]
	[InlineData(128)]
	public void InputsLongerThanFifteenDigits_MatchTheReference(int length)
	{
		for (int i = 0; i < 3000; i++)
		{
			string hex = Inputs.Hex($"long{length}", i, length);
			long n = i % 3 == 0 ? Inputs.Number($"longn{length}", i, 2, 1L << 60) : Inputs.Number($"longn{length}", i, 2, 2000000);
			long result = Rng.RandLong(hex, n);
			Assert.InRange(result, 0, n - 1);
			Assert.Equal(Reference.Rand(hex, n).Result, result);
		}
	}

	// ------------------------------------------------------------------ distribution

	/// <summary>Outcome counts over 500 000 hashes, as games call it: a full hash and a small n</summary>
	[Theory]
	[InlineData(2)]
	[InlineData(6)]
	[InlineData(37)]
	[InlineData(52)]
	[InlineData(1000)]
	public void OutcomeFrequencies_OverManyHashes_AreUniform(int n)
	{
		var counts = new long[n];
		for (int i = 0; i < 500000; i++)
			counts[Rng.Rand(Inputs.Hex($"frequency{n}", i), n)]++;
		Statistics.AssertUniform(counts);
	}

	/// <summary>n just above half the input range rejects about half of all first draws, so this measures the retried draws.
	/// Results are grouped into 20 equal parts of 0..n-1.</summary>
	[Fact]
	public void RetriedDraws_AreUniform()
	{
		const long n = (1L << 59) + 12345;
		var all = new long[20];
		var retried = new long[20];
		for (int i = 0; i < 400000; i++)
		{
			string hex = Inputs.Hex("retried", i, 15);
			long result = Rng.RandLong(hex, n);
			int part = (int)((UInt128)result * 20 / (UInt128)n);
			all[part]++;
			if (Reference.Rand(hex, n).Rejected > 0)
				retried[part]++;
		}
		Assert.InRange(retried.Sum(), 190000, 210000);
		Statistics.AssertUniform(all);
		Statistics.AssertUniform(retried);
	}

	// ------------------------------------------------------------------ reproducibility

	[Fact]
	public void SameInput_AlwaysGivesTheSameResult()
	{
		for (int i = 0; i < 2000; i++)
		{
			string hex = Inputs.Hex("repeat", i, 1 + i % 40);
			long n = Inputs.Number("repeatn", i, 2, 1L << Math.Min(4 * hex.Length, 60));
			long first = Rng.RandLong(hex, n);
			Assert.Equal(first, Rng.RandLong(hex, n));
			Assert.Equal(first, Rng.RandLong(new string(hex.ToCharArray()), n));
		}
	}

	/// <summary>Upper and lower case hex give the same result, on accepted and on retried draws: every 2 digit input with every n above 128</summary>
	[Fact]
	public void HexCase_DoesNotChangeTheResult()
	{
		for (int n = 129; n <= 256; n++)
			for (int v = 0; v < 256; v++)
				Assert.Equal(Rng.Rand(v.ToString("x2"), n), Rng.Rand(v.ToString("X2"), n));
		for (int i = 0; i < 2000; i++)
		{
			string hex = Inputs.Hex("case", i);
			Assert.Equal(Rng.Rand(hex, 1000000), Rng.Rand(hex.ToUpperInvariant(), 1000000));
		}
	}

	[Fact]
	public void DigitsAfterTheFifteenth_DoNotChangeAnAcceptedDraw()
	{
		int checkedDraws = 0;
		for (int i = 0; i < 2000; i++)
		{
			string hex = Inputs.Hex("suffix", i);
			long n = Inputs.Number("suffixn", i, 2, 1000000);
			if (Reference.Rand(hex, n).Rejected > 0)
				continue;
			checkedDraws++;
			long result = Rng.RandLong(hex, n);
			Assert.Equal(result, Rng.RandLong(hex[..15], n));
			Assert.Equal(result, Rng.RandLong(hex[..15] + Inputs.Hex("other suffix", i), n));
		}
		Assert.InRange(checkedDraws, 1990, 2000);
	}

	/// <summary>A rejected draw is redrawn from the whole input string, so two inputs with the same first 15 digits can retry differently</summary>
	[Fact]
	public void RejectedDraw_IsRedrawnFromTheWholeInput()
	{
		const long n = (1L << 59) + 1;
		const string rejectedDigits = "000000000000000";
		Assert.True(Reference.Rand(rejectedDigits, n).Rejected > 0);
		long abc = Rng.RandLong(rejectedDigits + "abc", n);
		long abd = Rng.RandLong(rejectedDigits + "abd", n);
		Assert.Equal(Reference.Rand(rejectedDigits + "abc", n).Result, abc);
		Assert.Equal(Reference.Rand(rejectedDigits + "abd", n).Result, abd);
		Assert.NotEqual(abc, abd);
	}

	// ------------------------------------------------------------------ arguments

	/// <summary>16^k outcomes need k hex digits, one more outcome needs k + 1</summary>
	[Theory]
	[MemberData(nameof(AllDigitCounts))]
	public void ValidateHexLength_RequiresOneDigitPerPowerOfSixteen(int digits)
	{
		long span = 1L << (4 * digits);
		Rng.ValidateHexLength(new string('0', digits), span);
		Rng.ValidateHexLength(new string('0', digits + 5), span);
		if (digits > 1)
		{
			Assert.Throws<ArgumentException>(() => Rng.ValidateHexLength(new string('0', digits - 1), span));
			Assert.Throws<ArgumentException>(() => Rng.RandLong(new string('0', digits - 1), span));
		}
		if (digits < 15)
		{
			Assert.Throws<ArgumentException>(() => Rng.ValidateHexLength(new string('0', digits), span + 1));
			Assert.Throws<ArgumentException>(() => Rng.RandLong(new string('0', digits), span + 1));
			Rng.ValidateHexLength(new string('0', digits + 1), span + 1);
		}
	}

	[Theory]
	[InlineData(1)]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(long.MinValue)]
	public void FewerThanTwoOutcomes_Throws(long n)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => Rng.ValidateHexLength("ffffffffffffffff", n));
		Assert.Throws<ArgumentOutOfRangeException>(() => Rng.RandLong("ffffffffffffffff", n));
		if (n >= int.MinValue)
			Assert.Throws<ArgumentOutOfRangeException>(() => Rng.Rand("ffffffffffffffff", (int)n));
	}

	[Theory]
	[InlineData((1L << 60) + 1)]
	[InlineData(long.MaxValue)]
	public void MoreOutcomesThanSixtyBits_Throws(long n)
	{
		Assert.Throws<OverflowException>(() => Rng.ValidateHexLength(new string('f', 64), n));
		Assert.Throws<OverflowException>(() => Rng.RandLong(new string('f', 64), n));
	}

	[Fact]
	public void EmptyHex_Throws() => Assert.Throws<ArgumentException>(() => Rng.Rand("", 2));

	[Theory]
	[InlineData("zz")]
	[InlineData("0x1f")]
	[InlineData(" f")]
	[InlineData("f ")]
	[InlineData("-1")]
	[InlineData("+1")]
	[InlineData("1.5")]
	public void InputThatIsNotHex_Throws(string hex)
	{
		Assert.Throws<FormatException>(() => Rng.Rand(hex, 2));
		Assert.Throws<FormatException>(() => Rng.RandLong(hex, 2));
	}
}
