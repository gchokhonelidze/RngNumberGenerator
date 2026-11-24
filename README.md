# Deterministic Hash-Based Random Utilities
A lightweight C# utility for generating deterministic pseudo-random values from hexadecimal hashes.
Designed for scenarios requiring reproducible randomness such as gaming logic, provably-fair systems, shuffling, and outcome generation.

## Features
- Deterministic random int and long generation from hex strings
- Exact integer arithmetic with rejection sampling: every outcome has the same probability
- Outcome range validation: the hex string must cover the requested number of outcomes
- SHA1-based hash rotation with nonce
- Deterministic array shuffling (Fisher–Yates)
- No mutation of input collections
- Pure static utility methods

## How It Works
The library converts a hexadecimal string into an integer and maps it into the range:
```c#
0 .. (n - 1)
```
All arithmetic is on integers, so there is no rounding at any hex length.

1. Take the first `L = min(hex.Length, 15)` hex digits and read them as an integer `v` in `0 .. 16^L - 1`.
2. Compute `m = v * n` (128-bit).
3. If `m mod 16^L >= 16^L mod n`, the draw is accepted and the result is `floor(m / 16^L)`.
4. Otherwise the draw is rejected and replaced by the first `L` hex digits of

   `hex( SHA-256( UTF-8( lowercase(hex) + ":" + attempt ) ) )`

   with `attempt` = 1, 2, 3, ... (decimal) until a draw is accepted. `hex` here is the whole string that was passed in, not only its first `L` digits.

Step 3 leaves exactly `floor(16^L / n)` accepted values of `v` for every outcome, so every outcome has the same probability for every supported `n`. A retry depends only on `hex` and the attempt number, so the same `hex` and `n` always give the same result. Fewer than half of all draws are rejected in the worst case, and for a 15-digit draw the chance is below `n / 2^60`.

The hex string must be long enough for the requested number of outcomes: `16^L >= n`.

## API
```c#
ValidateHexLength(string hex, long n)
```
Ensures the provided hex string is long enough to generate n outcomes. Supported n: 2 .. 16^15 (2^60).

Throws:
- ArgumentOutOfRangeException if n <= 1
- OverflowException if n exceeds supported range
- ArgumentException if hex length is insufficient

```c#
int Rand(string hex, int n)
```
Returns a deterministic random integer in:
```c#
0 .. (n - 1)
```
Uses the method described in How It Works. Throws FormatException if the digits it reads are not hexadecimal.

```c#
long RandLong(string hex, long n)
```
Same as Rand, but supports long ranges. `Rand(hex, n)` and `RandLong(hex, n)` return the same value.

```c#
string RotateHash(string hash, int nonce)
```
Generates a new SHA1-based hash, 40 lowercase hex characters:
hex(SHA-1(UTF-8(hash + nonce))), nonce written in decimal.
Useful for generating sequential deterministic randomness.

```c#
byte[] ToBytes(string s)
```
UTF-8 bytes of the string.

```c#
IList<T> Shuffle<T>(IList<T> arr, string hash)
```
Returns a new shuffled copy of the array using a deterministic Fisher–Yates algorithm.
For `i` from the last index down to 1, item `i` is swapped with item `j`:

`j = Rand( hex( SHA-256( UTF-8( lowercase(hash) + ":shuffle:" + i ) ) ), i + 1 )`

Every `j` is drawn by `Rand`, so each step is exactly uniform and no ordering is favoured.
The whole hash is the seed, so the orderings it can reach are limited only by how much randomness the hash holds.
Original collection is not mutated.

## Example
```c#
string hash = "a3f5c9e1b7d4";

// Random number between 0 and 9
int result = Rand(hash, 10);

// Random long between 0 and 999
long longResult = RandLong(hash, 1000);

// Rotate hash
string nextHash = RotateHash(hash, 1);

// Shuffle
var numbers = new[] { 1, 2, 3, 4, 5 };
var shuffled = Shuffle(numbers, hash);
```

## Use Cases
- Provably fair gaming systems
- Deterministic simulations
- Reproducible shuffling
- Blockchain or hash-seeded logic
- Server/client verifiable randomness

## Tests
```
dotnet test
```
runs the xUnit suite in `tests/`. The tests use fixed inputs only, so every run gives the same result on every machine.

- **Known answers.** Fixed inputs for `Rand`, `RandLong`, `RotateHash` and `Shuffle`, with answers from `tests/reference.py`, a separate Python implementation of the algorithm above. `python tests/reference.py` prints them.
- **Exactness.** With `n = 16^L` the result must be the input itself, for every `L` from 1 to 15.
- **Exact uniformity.** Every input of 1, 2 and 3 hex digits against every `n` those digits support, and every 4 digit input against selected `n`: the accepted inputs give each outcome exactly `floor(16^L / n)` times, and fewer than half the inputs are rejected.
- **Second implementation.** `tests/Reference.cs` repeats the algorithm in `BigInteger` arithmetic. The library must match it for hex lengths 1 to 128 and `n` up to 2^60, including retried draws.
- **Distribution.** Chi-square tests at the 99.9% level on outcome counts over many hashes, on retried draws alone, on the orderings `Shuffle` produces for 2 to 6 items, and on where each card of a 52 card deck lands.
- **Reproducibility.** The same input gives the same result, hex case does not matter, and digits after the 15th do not change an accepted draw.
- **Arguments.** The hex length each `n` needs, `n` outside 2 .. 2^60, and input that is not hex.

## Changes in 1.0.3
- `Rand` and `RandLong` use exact integer arithmetic. 1.0.2 divided in `decimal` by a rounded power of two, which gave inexact results for 13-15 hex digits and could return `n` or more for 14 digits.
- Rejection sampling removes the scaling bias. An accepted draw still returns `floor(v * n / 16^L)`, so results match 1.0.2 except where 1.0.2 rounded wrongly or the draw is rejected.
- `RotateHash`, `Sha1` and `ToBytes` hash the plain UTF-8 bytes of the string. Up to 1.0.2 they hashed its MessagePack encoding, so `RotateHash` values differ from 1.0.2.
- `ToBytes` takes a `string` instead of an `object`.
- `Shuffle` draws every position with `Rand` and is seeded by the whole hash. 1.0.2 seeded `System.Random` with the first 8 hex characters, so shuffle results differ from 1.0.2.
- No package dependencies: MessagePack and Menso.Tools.Scrambler are removed.
- Whitespace around the hex digits is no longer accepted.
