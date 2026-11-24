namespace Flappybet.Tests;

public class ShuffleTests
{
	[Fact]
	public void MatchesTheReference()
	{
		for (int size = 0; size <= 60; size++)
			for (int i = 0; i < 40; i++)
			{
				string hash = Inputs.Hex($"shuffle reference {size}", i, 1 + (i * 7) % 128);
				var items = Enumerable.Range(0, size).ToList();
				Assert.Equal(Reference.Shuffle(items, hash), Rng.Shuffle(items, hash));
			}
		var large = Enumerable.Range(0, 5000).ToList();
		Assert.Equal(Reference.Shuffle(large, "large"), Rng.Shuffle(large, "large"));
	}

	[Fact]
	public void ReturnsEveryItemExactlyOnce()
	{
		for (int size = 0; size <= 200; size++)
		{
			var items = Enumerable.Range(0, size).ToList();
			Assert.Equal(items, Rng.Shuffle(items, Inputs.Hex("permutation", size)).Order());
		}
	}

	[Fact]
	public void KeepsRepeatedItems()
	{
		string[] items = ["a", "b", "a", "c", "a", "b", "", "c"];
		var shuffled = Rng.Shuffle(items, Inputs.Hex("repeated", 0));
		Assert.Equal(items.Order(), shuffled.Order());
	}

	[Fact]
	public void DoesNotChangeTheInput()
	{
		int[] array = [.. Enumerable.Range(0, 52)];
		var list = Enumerable.Range(0, 52).ToList();
		var fromArray = Rng.Shuffle(array, Inputs.Hex("input", 0));
		var fromList = Rng.Shuffle(list, Inputs.Hex("input", 0));
		Assert.Equal(Enumerable.Range(0, 52), array);
		Assert.Equal(Enumerable.Range(0, 52), list);
		Assert.NotSame(array, fromArray);
		Assert.NotSame(list, fromList);
		Assert.Equal(fromArray, fromList);
		Assert.NotEqual(Enumerable.Range(0, 52), fromList);
	}

	[Fact]
	public void EmptyAndSingleItemLists_ComeBackUnchanged()
	{
		Assert.Empty(Rng.Shuffle(Array.Empty<int>(), "abcd"));
		Assert.Equal([7], Rng.Shuffle([7], "abcd"));
	}

	[Fact]
	public void SameHash_AlwaysGivesTheSameOrder()
	{
		var items = Enumerable.Range(0, 52).ToList();
		for (int i = 0; i < 200; i++)
		{
			string hash = Inputs.Hex("repeat shuffle", i);
			var first = Rng.Shuffle(items, hash);
			Assert.Equal(first, Rng.Shuffle(items, hash));
			Assert.Equal(first, Rng.Shuffle(items, hash.ToUpperInvariant()));
		}
	}

	/// <summary>1.0.2 seeded from the first 8 hex characters only, so these two hashes gave the same order</summary>
	[Fact]
	public void TheWholeHashIsTheSeed()
	{
		var items = Enumerable.Range(0, 52).ToList();
		string a = "cba77dd1" + new string('0', 56);
		string b = "cba77dd1" + new string('0', 55) + "1";
		Assert.NotEqual(Rng.Shuffle(items, a), Rng.Shuffle(items, b));
	}

	/// <summary>Counts how often each of the n! orders comes up over many hashes: every order must appear, and equally often</summary>
	[Theory]
	[InlineData(2, 100000)]
	[InlineData(3, 150000)]
	[InlineData(4, 240000)]
	[InlineData(5, 480000)]
	[InlineData(6, 720000)]
	public void EveryOrder_ComesUpEquallyOften(int size, int shuffles)
	{
		int orders = Enumerable.Range(1, size).Aggregate(1, (product, k) => product * k);
		var items = Enumerable.Range(0, size).ToList();
		var counts = new Dictionary<string, long>();
		for (int i = 0; i < shuffles; i++)
		{
			string order = string.Concat(Rng.Shuffle(items, Inputs.Hex($"orders{size}", i)));
			counts[order] = counts.GetValueOrDefault(order) + 1;
		}
		Assert.Equal(orders, counts.Count);
		Statistics.AssertUniform(counts.Values);
	}

	/// <summary>A 52 card deck over 60 000 hashes: every card must land in every position equally often.
	/// Each position is tested on its own, and so is each card.</summary>
	[Fact]
	public void FiftyTwoCards_EveryCardLandsInEveryPositionEquallyOften()
	{
		const int size = 52;
		var landed = new long[size, size]; //[card, position]
		var deck = Enumerable.Range(0, size).ToList();
		for (int i = 0; i < 60000; i++)
		{
			var shuffled = Rng.Shuffle(deck, Inputs.Hex("deck", i, 128));
			for (int position = 0; position < size; position++)
				landed[shuffled[position], position]++;
		}
		double worstPosition = 0,
			worstCard = 0;
		for (int a = 0; a < size; a++)
		{
			worstPosition = Math.Max(worstPosition, Statistics.ChiSquare([.. Enumerable.Range(0, size).Select(card => landed[card, a])]));
			worstCard = Math.Max(worstCard, Statistics.ChiSquare([.. Enumerable.Range(0, size).Select(position => landed[a, position])]));
		}
		// 52 tests each way, so the limit is the 99.9% quantile for the largest of 52: about 103.8 at 51 degrees of freedom
		double limit = Statistics.Critical(size - 1, Statistics.Z999Of52);
		Assert.True(worstPosition < limit, $"worst position chi-square {worstPosition:F2} is not below {limit:F2}");
		Assert.True(worstCard < limit, $"worst card chi-square {worstCard:F2} is not below {limit:F2}");
	}
}
