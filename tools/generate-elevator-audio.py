"""Original, deterministic mechanical sound layers; no downloaded samples or runtime synthesis."""
import math
import random
import struct
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Assets/ApartmentVisuals/Audio"
ROOT.mkdir(parents=True, exist_ok=True)
RATE = 44100


def write(name, seconds, kind):
    rng = random.Random(402)
    low = 0.0
    samples = []
    for i in range(round(seconds * RATE)):
        t = i / RATE
        low += .035 * (rng.uniform(-1, 1) - low)
        if kind == "drive":
            # Broad machinery hum, rotor harmonics and air/roller texture.
            value = (.24 * math.sin(2 * math.pi * 60 * t)
                     + .13 * math.sin(2 * math.pi * 120 * t + .3)
                     + .065 * math.sin(2 * math.pi * 180 * t)
                     + .045 * math.sin(2 * math.pi * 337 * t)
                     + .55 * low) * (.90 + .10 * math.cos(2 * math.pi * 3 * t))
        elif kind == "door":
            value = (.09 * math.sin(2 * math.pi * 145 * t)
                     + .06 * math.sin(2 * math.pi * 290 * t)
                     + .65 * low + .018 * rng.uniform(-1, 1))
            value *= .85 + .15 * math.cos(2 * math.pi * 7 * t)
        else:
            value = 0
            for onset, hz in [(0, 659.25), (.45, 523.25)]:
                age = t - onset
                if age >= 0:
                    value += .27 * (1 - math.exp(-age * 150)) * math.exp(-age * 3.8) * (
                        math.sin(2 * math.pi * hz * age) + .24 * math.sin(2 * math.pi * hz * 2.01 * age))
        # Only taper the tiny noise discontinuity at the loop seam; travel/door envelopes are runtime-controlled.
        value *= min(1, t / .008, (seconds - t) / .008)
        samples.append(max(-32767, min(32767, round(value * 32767))))
    with wave.open(str(ROOT / (name + ".wav")), "wb") as out:
        out.setparams((1, 2, RATE, len(samples), "NONE", "not compressed"))
        out.writeframes(struct.pack("<" + "h" * len(samples), *samples))
    print(name, "seconds=", seconds, "peak=", max(abs(s) for s in samples) / 32767,
          "rms=", math.sqrt(sum(s*s for s in samples)/len(samples))/32767)


write("ElevatorDrive", 8, "drive")
write("ElevatorDoor", 4, "door")
write("ElevatorArrival", 2.2, "arrival")
