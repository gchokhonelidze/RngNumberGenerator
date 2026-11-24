"""Independent reference implementation of Flappybet.Rng 1.0.3, standard library only.

Written from the algorithm in README.md, not from Rng.cs. Running it prints the known-answer
vectors that KnownAnswerTests.cs holds:

    python tests/reference.py
"""
import hashlib


def derive_hash(seed, index):
    return hashlib.sha256(f"{seed.lower()}:{index}".encode("utf-8")).hexdigest()


def rand_with_retries(hex_str, n):
    """Returns (result, number of rejected draws)."""
    digits = min(len(hex_str), 15)
    span = 16**digits
    draw, attempt = hex_str, 0
    while True:
        m = int(draw[:digits], 16) * n
        if m % span >= span % n:
            return m // span, attempt
        attempt += 1
        draw = derive_hash(hex_str, attempt)


def rand(hex_str, n):
    return rand_with_retries(hex_str, n)[0]


def rotate_hash(hash_str, nonce):
    return hashlib.sha1(f"{hash_str}{nonce}".encode("utf-8")).hexdigest()


def shuffle(items, hash_str):
    a = list(items)
    for i in range(len(a) - 1, 0, -1):
        j = rand(derive_hash(f"{hash_str}:shuffle", i), i + 1)
        a[i], a[j] = a[j], a[i]
    return a


DOC_HASH = "cba77dd118605e6c6c8bd1807dc6f6ff2b79567f7e49e05fd96fcd25ec63443e"
LONG_HASH = DOC_HASH + "409a88302ce2d30f1d2e0d840dc1851eed68bec38c3963ba4fc10d3d66a9e900"

RAND_CASES = [
    # a 64 digit hash, the usual call: the first 15 digits are read
    (DOC_HASH, 2),
    (DOC_HASH, 6),
    (DOC_HASH, 37),
    (DOC_HASH, 1000000),
    (DOC_HASH, 2**31 - 1),
    (DOC_HASH, 16**13),
    (DOC_HASH, 2**59 + 12345),
    (DOC_HASH, 2**60),
    (LONG_HASH, 52),
    (DOC_HASH.upper(), 37),
    # short slices
    ("0", 2),
    ("f", 16),
    ("7", 9),
    ("0a3f", 2),
    ("0a3f", 6),
    ("ffff", 6),
    ("8000", 65536),
    ("c5", 100),
    # 13, 14 and 15 digits, where 1.0.2 rounded
    ("fffffffffffff", 16**13),
    ("ffffffffffffff", 16**14),
    ("fffffffffffffff", 16**15),
    ("123456789abcd", 10**12),
    ("123456789abcde", 10**15),
    ("123456789abcdef", 10**18),
    ("fedcba987654321", 999999999999999989),
    # first draw rejected: the result comes from a retry
    ("0", 3),
    ("0", 7),
    ("00", 255),
    ("0000", 1000),
    ("0000", 33000),
    ("0000000000000", 16**13 - 1),
    ("00000000000000", 16**14 - 1),
    ("000000000000000", 2**59 + 1),
    ("000000000000000abc", 2**59 + 1),
    ("000000000000000ABC", 2**59 + 1),
    ("000000000000000abd", 2**59 + 1),
]

ROTATE_CASES = [
    ("ab", 1),
    ("", 0),
    ("abc", -7),
    (DOC_HASH, 0),
    (DOC_HASH, 123456),
    ("lucky_pegs.gold.1718876951792", 7001),
    (DOC_HASH, 2**31 - 1),
    (DOC_HASH, -(2**31)),
]

SHUFFLE_CASES = [
    (2, "00"),
    (3, DOC_HASH),
    (5, DOC_HASH),
    (10, DOC_HASH),
    (10, DOC_HASH.upper()),
    (10, "seed that is not hex"),
    (10, ""),
    (52, DOC_HASH),
    (52, LONG_HASH),
]


def more_retries(minimum):
    """Smallest 2 digit input and n whose result needs at least `minimum` rejected draws."""
    for n in range(129, 256):
        for v in range(256):
            hex_str = f"{v:02x}"
            if rand_with_retries(hex_str, n)[1] >= minimum:
                return hex_str, n
    raise LookupError(minimum)


if __name__ == "__main__":
    cases = RAND_CASES + [more_retries(m) for m in (3, 5, 8)]
    print("// Rand / RandLong: hex, n, expected  (comment: rejected draws before the result)")
    for hex_str, n in cases:
        result, retries = rand_with_retries(hex_str, n)
        print(f'{{ "{hex_str}", {n}L, {result}L }}, // {retries}')
    print("// RotateHash: hash, nonce, expected")
    for hash_str, nonce in ROTATE_CASES:
        print(f'{{ "{hash_str}", {nonce}, "{rotate_hash(hash_str, nonce)}" }},')
    print("// Shuffle of 0..count-1: count, hash, expected")
    for count, hash_str in SHUFFLE_CASES:
        print(f'{{ {count}, "{hash_str}", "{",".join(map(str, shuffle(range(count), hash_str)))}" }},')
