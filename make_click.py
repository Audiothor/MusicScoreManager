import wave, struct, math
import os

sample_rate = 44100
duration = 0.1 # Plus long pour permettre une queue de résonance propre
frequency = 1500.0 # Plus aigu pour un clic net
num_samples = int(sample_rate * duration)

os.makedirs('Resources/Raw', exist_ok=True)
f = wave.open('Resources/Raw/click.wav', 'w')
f.setnchannels(1)
f.setsampwidth(2)
f.setframerate(sample_rate)

for i in range(num_samples):
    t = float(i) / sample_rate
    # Attaque rapide (1ms) et déclin exponentiel
    attack = min(1.0, t / 0.001)
    decay = math.exp(-t * 60)
    envelope = attack * decay
    
    # Mix de deux fréquences pour un son plus "percussif" et moins bip électronique
    wave_val = (math.sin(2.0 * math.pi * frequency * t) + 0.5 * math.sin(2.0 * math.pi * (frequency * 2) * t)) / 1.5
    
    # Appliquer l'enveloppe et réduire un peu le volume max (0.8) pour éviter la saturation
    value = int(32767.0 * 0.8 * wave_val * envelope)
    
    data = struct.pack('<h', value)
    f.writeframesraw(data)

f.close()
