"""Original quiet sole impacts and ventilation. No third-party recordings."""
import math
import random
import struct
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / 'Assets/ApartmentVisuals/Audio'
RATE = 44100

def write(name, seconds, seed, ambience=False):
    rng = random.Random(seed)
    low = 0.0
    samples = []
    for i in range(round(seconds * RATE)):
        t = i / RATE
        white = rng.uniform(-1, 1)
        low += .06 * (white - low)
        if ambience:
            # Integer periods and tapered noise make the loop seam unobtrusive.
            edge = min(1, t / .1, (seconds-t) / .1)
            value = .11 * low * edge + .012 * math.sin(2*math.pi*100*t) + .007*math.sin(2*math.pi*150*t)
        else:
            heel = (1-math.exp(-t*900))*math.exp(-t*36)
            toe_age=max(0,t-.055)
            toe=(1-math.exp(-toe_age*150))*math.exp(-toe_age*28)
            value = .38*heel*math.sin(2*math.pi*(85+seed%13)*t) + .5*low*(heel+.45*toe) + .035*white*toe
            value *= min(1,(seconds-t)/.02)
        samples.append(round(max(-.99,min(.99,value))*32767))
    with wave.open(str(ROOT/(name+'.wav')),'wb') as out:
        out.setparams((1,2,RATE,len(samples),'NONE','not compressed'))
        out.writeframes(struct.pack('<'+'h'*len(samples),*samples))
    print(name, 'peak', round(max(map(abs,samples))/32767,4), 'rms', round(math.sqrt(sum(s*s for s in samples)/len(samples))/32767,4))

if __name__ == '__main__':
    ROOT.mkdir(parents=True,exist_ok=True)
    # Footsteps now use licensed leather/concrete recordings; never overwrite authored clips here.
    write('Ventilation',8,440,True)
