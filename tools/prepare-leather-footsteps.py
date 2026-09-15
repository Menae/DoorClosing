"""Cut four heel/sole impacts from HerbertBoland's CC BY 4.0 ShoeLeatherSteps.
Input: decoded mono PCM16/44100 Hz public HQ preview, see docs/licenses/Footsteps.txt.
Usage: python tools/prepare-leather-footsteps.py artifacts/feedback-01/leather-source.wav
"""
import math,struct,sys,wave
from pathlib import Path
with wave.open(sys.argv[1],'rb') as w:
    assert (w.getnchannels(),w.getsampwidth(),w.getframerate())==(1,2,44100)
    samples=struct.unpack('<'+'h'*w.getnframes(),w.readframes(w.getnframes()))
root=Path(__file__).resolve().parents[1]/'Assets/ApartmentVisuals/Audio'
for index,peak_time in enumerate([.29,2.41,16.69,21.15],1):
    segment=samples[round((peak_time-.05)*44100):round((peak_time+.31)*44100)]
    low=0; filtered=[]
    for i,v in enumerate(segment):
        low+=.01*(v-low)
        edge=min(1,i/100,(len(segment)-1-i)/1500)
        filtered.append((v-low)*edge)
    gain=.60*32767/max(map(abs,filtered))
    pcm=[round(v*gain) for v in filtered]
    with wave.open(str(root/f'Footstep{index}.wav'),'wb') as out:
        out.setparams((1,2,44100,len(pcm),'NONE','not compressed'))
        out.writeframes(struct.pack('<'+'h'*len(pcm),*pcm))
    print(index,'peak=.60 rms=',round(math.sqrt(sum(v*v for v in pcm)/len(pcm))/32767,4))
