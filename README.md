# Deterministic Hash-Based Random Utilities
A lightweight C# utility for generating deterministic pseudo-random values from hexadecimal hashes.
Designed for scenarios requiring reproducible randomness such as gaming logic, provably-fair systems, shuffling, and outcome generation.

## Features
- Deterministic random int and long generation from hex strings
- Outcome range validation to ensure symmetric distribution
- SHA1-based hash rotation with nonce
- Deterministic array shuffling (Fisher–Yates)
- No mutation of input collections
- Pure static utility methods

## How It Works
The library converts a hexadecimal string into a numeric value and maps it proportionally into the range:
```c#
0 .. (n - 1)
```
Distribution symmetry is preserved by validating that the hex string has sufficient entropy for the requested number of outcomes.

## API
```c#
ValidateHexLength(string hex, long n)
```
Ensures the provided hex string contains enough entropy to fairly generate n outcomes.

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
Uses proportional scaling from the hex value.

```c#
long RandLong(string hex, long n)
```
Same as Rand, but supports long ranges.

```c#
string RotateHash(string hash, int nonce)
```
Generates a new SHA1-based hash using:
SHA1(hash + nonce)
Useful for generating sequential deterministic randomness.

```c#
IList<T> Shuffle<T>(IList<T> arr, string hash)
```
Returns a new shuffled copy of the array using a deterministic Fisher–Yates algorithm.
Seed is derived from first 8 hex characters.
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