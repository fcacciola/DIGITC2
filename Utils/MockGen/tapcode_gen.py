"""
tapcode_gen.py — generate Tap Code training/calibration WAVs for Transgraphier.

Design (as settled):
  * A message is encoded into audio "taps" via a chosen Polybius square.
  * For binary squares the encoder ALWAYS lands on the correct cell for the bit
    (the marked 00/11 centre when one exists, otherwise the most-interior cell).
    No coordinate-level error is ever injected, even at full temperature.
  * All difficulty comes from AUDIO degradation, scaled by `temperature`:
      - tap amplitude pulled from ~0.9 down toward (noise_floor + 0.3)
      - tap-width and gap jitter
      - additive noise at `noise_floor`
    Decode errors (misses, ?-erasures, flips) then EMERGE when the real pipeline
    reads the degraded audio — which is what exercises the envelope/gating front
    end and gives the coverage score a real distribution.
  * Letters are delimited by a SYNC burst: a single row of taps with NO column,
    long enough to be unmistakable -> sync_taps = max(5, grid_rows + 1).
  * Binary byte bits are unpacked LSB-first (little-endian).

Output: a folder of .wav files + manifest.csv.
"""

import csv
import os
import wave
import numpy as np


# --------------------------------------------------------------------------- #
#  Squares (exactly as in the C# source; tokens: 0 1 ? 00 11)
# --------------------------------------------------------------------------- #

LETTER_SQUARES = {
    "LatinAlphabet_Simple": [          # 5x5, J merged into I
        list("ABCDE"),
        list("FGHIK"),
        list("LMNOP"),
        list("QRSTU"),
        list("VWXYZ"),
    ],
    "LatinAlphabet_Extended": [        # 6x6
        list("ABCDEF"),
        list("GHIJKL"),
        list("MNOPQR"),
        list("STUVWX"),
        ["Y", "Z", " ", ".", ",", "?"],
        ["!", "-", "(", ")", "0", "1"],
    ],
}

BINARY_SQUARES = {
    "Binary": [
        ["0", "1", "0"],
        ["1", "?", "1"],
        ["0", "1", "0"],
    ],
    "Binary_2_1": [
        ["0", "0", "1", "1", "0", "0"],
        ["0", "0", "1", "1", "0", "0"],
        ["1", "1", "?", "?", "1", "1"],
        ["1", "1", "?", "?", "1", "1"],
        ["0", "0", "1", "1", "0", "0"],
        ["0", "0", "1", "1", "0", "0"],
    ],
    "Binary_2_1_Guarded": [
        ["0", "0", "?", "1", "1", "?", "0", "0"],
        ["0", "0", "?", "1", "1", "?", "0", "0"],
        ["?", "?", "?", "?", "?", "?", "?", "?"],
        ["1", "1", "?", "?", "?", "?", "1", "1"],
        ["1", "1", "?", "?", "?", "?", "1", "1"],
        ["?", "?", "?", "?", "?", "?", "?", "?"],
        ["0", "0", "?", "1", "1", "?", "0", "0"],
        ["0", "0", "?", "1", "1", "?", "0", "0"],
    ],
    "Binary_3_1": [
        ["0", "0",  "0", "1", "1",  "1", "0", "0",  "0"],
        ["0", "00", "0", "1", "1",  "1", "0", "00", "0"],
        ["0", "0",  "0", "1", "1",  "1", "0", "0",  "0"],
        ["1", "1",  "1", "?", "?",  "?", "1", "1",  "1"],
        ["1", "11", "1", "?", "?",  "?", "1", "11", "1"],
        ["1", "1",  "1", "?", "?",  "?", "1", "1",  "1"],
        ["0", "0",  "0", "1", "1",  "1", "0", "0",  "0"],
        ["0", "00", "0", "1", "1",  "1", "0", "00", "0"],
        ["0", "0",  "0", "1", "1",  "1", "0", "0",  "0"],
    ],
    "Binary_3_1_Guarded": [
        ["0", "0",  "0", "?", "1", "1",  "1", "?", "0", "0",  "0"],
        ["0", "00", "0", "?", "1", "11", "1", "?", "0", "00", "0"],
        ["0", "0",  "0", "?", "1", "1",  "1", "?", "0", "0",  "0"],
        ["?", "?",  "?", "?", "?", "?",  "?", "?", "?", "?",  "?"],
        ["1", "1",  "1", "?", "?", "?",  "?", "?", "1", "1",  "1"],
        ["1", "11", "1", "?", "?", "?",  "?", "?", "1", "11", "1"],
        ["1", "1",  "1", "?", "?", "?",  "?", "?", "1", "1",  "1"],
        ["?", "?",  "?", "?", "?", "?",  "?", "?", "?", "?",  "?"],
        ["0", "0",  "0", "?", "1", "1",  "1", "?", "0", "0",  "0"],
        ["0", "00", "0", "?", "1", "11", "1", "?", "0", "00", "0"],
        ["0", "0",  "0", "?", "1", "1",  "1", "?", "0", "0",  "0"],
    ],
}


def _bit_of(token):
    """Map a square token to its bit value, or '?' for guard/centre cells."""
    if token in ("0", "00"):
        return "0"
    if token in ("1", "11"):
        return "1"
    return "?"


# --------------------------------------------------------------------------- #
#  Encode-cell selection for binary squares
# --------------------------------------------------------------------------- #

def _canonical_cells(grid, bit):
    """
    Cells (1-indexed (row, col)) used to encode `bit`.
      * If the square marks centres (00 / 11), use those.
      * Otherwise use the most-interior cells: those with maximum min-Chebyshev
        distance to the nearest '?' or opposite-bit cell.
    Several equivalent cells may be returned; the caller picks one at random,
    which only varies the tap counts (the decoded bit is identical) and so adds
    harmless variety to the audio.
    """
    rows, cols = len(grid), len(grid[0])
    doubles = [(r + 1, c + 1)
               for r in range(rows) for c in range(cols)
               if grid[r][c] in ("00", "11") and _bit_of(grid[r][c]) == bit]
    if doubles:
        return doubles

    other = "1" if bit == "0" else "0"
    hazards = [(r, c) for r in range(rows) for c in range(cols)
               if _bit_of(grid[r][c]) in ("?", other)]
    cands = [(r, c) for r in range(rows) for c in range(cols)
             if _bit_of(grid[r][c]) == bit]

    def min_dist(rc):
        if not hazards:
            return 10 ** 9
        return min(max(abs(rc[0] - h[0]), abs(rc[1] - h[1])) for h in hazards)

    best = max(min_dist(c) for c in cands)
    return [(r + 1, c + 1) for (r, c) in cands if min_dist((r, c)) == best]


def _letter_index(grid):
    """char -> (row, col), 1-indexed."""
    idx = {}
    for r, row in enumerate(grid):
        for c, ch in enumerate(row):
            idx[ch] = (r + 1, c + 1)
    return idx


# --------------------------------------------------------------------------- #
#  Message -> burst sizes (a burst = a count of taps)
# --------------------------------------------------------------------------- #

def _sync_taps(grid):
    return max(5, len(grid) + 1)


def message_to_bursts(message, square_name, rng):
    """Return a list of burst sizes (ints). SYNC bursts use the scaled count."""
    if square_name in LETTER_SQUARES:
        grid = LETTER_SQUARES[square_name]
        sync = _sync_taps(grid)
        idx = _letter_index(grid)
        bursts = [sync]
        for ch in message.upper():
            key = "I" if (ch == "J" and "J" not in idx) else ch
            if key not in idx:
                continue                      # char not in this square -> skip
            r, c = idx[key]
            bursts += [r, c, sync]
        return bursts

    grid = BINARY_SQUARES[square_name]
    sync = _sync_taps(grid)
    cells = {"0": _canonical_cells(grid, "0"), "1": _canonical_cells(grid, "1")}
    bursts = [sync]
    for byte in message.encode("ascii", errors="ignore"):
        for i in range(8):                    # LSB-first
            bit = "1" if (byte >> i) & 1 else "0"
            r, c = cells[bit][rng.integers(len(cells[bit]))]
            bursts += [r, c]
        bursts.append(sync)
    return bursts


# --------------------------------------------------------------------------- #
#  Audio synthesis
# --------------------------------------------------------------------------- #

def _lerp(a, b, t):
    return a + (b - a) * t


def render_bursts(bursts, p, rng):
    sr = p["sample_rate"]
    tap_n = sr * p["tap_ms"] / 1000.0
    intra_n = sr * p["gap_intra_ms"] / 1000.0
    between_n = 3.0 * intra_n                  # between-coords gap = intra * 3
    t = p["temperature"] / 100.0
    nf = p["noise_floor"]
    amp_lo = _lerp(nf,0.8, t)

    def jit(n):
        return max(1, int(round(n * (1.0 + rng.uniform(-0.5 * t, 0.5 * t)))))

    def tap():
        amp = rng.uniform(amp_lo, 0.9)
        n = jit(tap_n)
        k = np.arange(n)
        sig = amp * np.sin(2 * np.pi * p["tap_freq"] * k / sr)
        ramp = min(int(sr * 0.004), n // 2)    # soft edges, no hard clicks
        if ramp > 0:
            e = 0.5 * (1 - np.cos(np.pi * np.arange(ramp) / ramp))
            sig[:ramp] *= e
            sig[-ramp:] *= e[::-1]
        return sig

    pieces = [np.zeros(int(sr * rng.uniform(0.05, 0.30)))]   # lead-in silence
    for bi, size in enumerate(bursts):
        for ti in range(size):
            pieces.append(tap())
            if ti < size - 1:
                pieces.append(np.zeros(jit(intra_n)))
        if bi < len(bursts) - 1:
            pieces.append(np.zeros(jit(between_n)))
    pieces.append(np.zeros(int(sr * rng.uniform(0.05, 0.30))))  # tail silence

    sig = np.concatenate(pieces)
    sig += rng.normal(0.0, nf, size=sig.shape)               # noise floor
    return np.clip(sig, -1.0, 1.0)


def render_noise(p, rng, seconds):
    sr = p["sample_rate"]
    return np.clip(rng.normal(0.0, p["noise_floor"], int(sr * seconds)), -1.0, 1.0)


def write_wav(path, sig, sr):
    data = (np.clip(sig, -1.0, 1.0) * 32767.0).astype("<i2").tobytes()
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data)


# --------------------------------------------------------------------------- #
#  Message text for positives (common words -> realistic length/letter stats)
# --------------------------------------------------------------------------- #

_WORDS = ("the of and to in is it you that he was for on are with as his they at "
          "be this have from or one had by word but not what all were we when your "
          "can said there use an each which she do how their if will up other about "
          "out many then them these so some her would make like him into time has "
          "look two more write go see number no way could people my than first water "
          "been call who oil its now find long down day did get come made may part").split()


def random_text(rng, min_len, max_len):
    target = int(rng.integers(min_len, max_len + 1))
    out = []
    while len(" ".join(out)) < target:
        out.append(_WORDS[rng.integers(len(_WORDS))])
    text = " ".join(out)
    return text[:max_len].strip()


# --------------------------------------------------------------------------- #
#  Corpus generation
# --------------------------------------------------------------------------- #

def generate(config, outdir, manifest_rows=None, start_index=0):
    os.makedirs(outdir, exist_ok=True)
    rows = [] if manifest_rows is None else manifest_rows
    sq = config["square"]
    is_letter = sq in LETTER_SQUARES
    alphabet = [ch for r in LETTER_SQUARES[sq] for ch in r] if is_letter else None

    idx = start_index
    plan = ([("positive", config["n_positive"])]
            + [("random", config["n_random_byte_negative"])]
            + [("noise",  config["n_pure_noise_negative"])])

    for klass, count in plan:
        for _ in range(count):
            rng = np.random.default_rng(config["seed"] + idx)
            temp = float(rng.uniform(0.0, config["temperature"]))   # spread up to the dial
            p = dict(config, temperature=temp)

            if klass == "noise":
                sig = render_noise(p, rng, rng.uniform(3.0, 8.0))
                message = ""
            else:
                if klass == "positive":
                    message = random_text(rng, *config["msg_len_chars"])
                elif is_letter:                                     # random, letter square
                    n = int(rng.integers(*config["msg_len_chars"]))
                    message = "".join(alphabet[rng.integers(len(alphabet))] for _ in range(n))
                else:                                               # random bytes
                    n = int(rng.integers(2, 8))
                    message = bytes(rng.integers(0, 256, n).tolist()).decode("latin-1")
                bursts = message_to_bursts(message, sq, rng)
                sig = render_bursts(bursts, p, rng)

            fname = f"{idx:05d}_{klass}_{sq}.wav"
            write_wav(os.path.join(outdir, fname), sig, p["sample_rate"])
            rows.append({
                "filename":    fname,
                "klass":       klass,
                "square":      sq,
                "bit_order":   config["bit_order"],
                "temperature": round(temp, 1),
                "noise_floor": config["noise_floor"],
                "tap_ms":      config["tap_ms"],
                "gap_intra_ms": config["gap_intra_ms"],
                "message":     message if klass != "noise" else "",
                "duration_s":  round(len(sig) / p["sample_rate"], 2),
                "seed":        config["seed"] + idx,
            })
            idx += 1
    return rows


CONFIG = {
    "square":      "Binary",        # LatinAlphabet_Simple|_Extended | Binary|_2_1|_2_1_Guarded|_3_1|_3_1_Guarded
    "bit_order":   "lsb",
    "msg_len_chars": (8, 60),
    "sample_rate": 44100,
    "tap_freq":    1000.0,           # carrier inside a tap; match to your real taps if needed
    "tap_ms":      50.0,
    "gap_intra_ms": 50.0,           # between-coords gap is derived as intra * 3; sync = max(5, rows+1)
    "temperature": 100,             # corpus spreads uniformly over [0, this]
    "noise_floor": 0.6,
    "n_positive":             2,
    "n_random_byte_negative": 0,
    "n_pure_noise_negative":  0,
    "seed":        1234,
}


if __name__ == "__main__":
    rows = generate(CONFIG, "out")
    print(f"wrote {len(rows)} files to out/")
