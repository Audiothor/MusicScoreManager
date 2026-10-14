using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if ANDROID
using Android.Media;
#endif

namespace MusicScoreManager.Services
{
    public class TuningNote
    {
        public string NoteName { get; set; } = string.Empty;
        public string Solfege { get; set; } = string.Empty;
        public string StringLabel { get; set; } = string.Empty;
        public double Frequency { get; set; }
        public int MidiNote { get; set; }

        public string DisplayText => $"{StringLabel} • {NoteName} ({Solfege})";
    }

    public class TunerInstrument
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsChromatic { get; set; }
        public List<TuningNote> Notes { get; set; } = new();

        public override string ToString() => $"{Icon} {Name}";
    }

    public class PitchDetectedEventArgs : EventArgs
    {
        public double Frequency { get; set; }
        public double TargetFrequency { get; set; }
        public double Cents { get; set; } // -50 à +50
        public string NoteName { get; set; } = string.Empty;
        public string SolfegeName { get; set; } = string.Empty;
        public int Octave { get; set; }
        public double Amplitude { get; set; }
        public bool HasSignal { get; set; }
        public bool IsInTune => HasSignal && Math.Abs(Cents) <= 3.5;
        public string TuningAdvice => !HasSignal ? "" : (IsInTune ? "✓ Parfait" : (Cents < 0 ? "◀ Trop bas (bémol)" : "Trop haut (dièse) ▶"));
    }

    public class TunerService
    {
        private static TunerService? _instance;
        public static TunerService Instance => _instance ??= new TunerService();

        public event EventHandler<PitchDetectedEventArgs>? PitchDetected;

        public bool IsListening { get; private set; }
        private CancellationTokenSource? _listeningCts;

        private const int SampleRate = 44100;
        private const int BufferSize = 4096;

        public List<TunerInstrument> Instruments { get; } = new();

        public TunerService()
        {
            InitializeInstruments();
        }

        private void InitializeInstruments()
        {
            // 1. Chromatique Universel
            Instruments.Add(new TunerInstrument
            {
                Id = "chromatic",
                Name = "Chromatique Universel",
                Icon = "🎼",
                IsChromatic = true,
                Notes = new List<TuningNote>()
            });

            // 2. Guitare Standard (6 cordes)
            Instruments.Add(new TunerInstrument
            {
                Id = "guitar",
                Name = "Guitare Standard (6 cordes)",
                Icon = "🎸",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "6", NoteName = "E2", Solfege = "Mi2", Frequency = 82.41, MidiNote = 40 },
                    new TuningNote { StringLabel = "5", NoteName = "A2", Solfege = "La2", Frequency = 110.00, MidiNote = 45 },
                    new TuningNote { StringLabel = "4", NoteName = "D3", Solfege = "Ré3", Frequency = 146.83, MidiNote = 50 },
                    new TuningNote { StringLabel = "3", NoteName = "G3", Solfege = "Sol3", Frequency = 196.00, MidiNote = 55 },
                    new TuningNote { StringLabel = "2", NoteName = "B3", Solfege = "Si3", Frequency = 246.94, MidiNote = 59 },
                    new TuningNote { StringLabel = "1", NoteName = "E4", Solfege = "Mi4", Frequency = 329.63, MidiNote = 64 },
                }
            });

            // 3. Basse Standard (4 cordes)
            Instruments.Add(new TunerInstrument
            {
                Id = "bass_4",
                Name = "Basse Standard (4 cordes)",
                Icon = "🎸",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "4", NoteName = "E1", Solfege = "Mi1", Frequency = 41.20, MidiNote = 28 },
                    new TuningNote { StringLabel = "3", NoteName = "A1", Solfege = "La1", Frequency = 55.00, MidiNote = 33 },
                    new TuningNote { StringLabel = "2", NoteName = "D2", Solfege = "Ré2", Frequency = 73.42, MidiNote = 38 },
                    new TuningNote { StringLabel = "1", NoteName = "G2", Solfege = "Sol2", Frequency = 98.00, MidiNote = 43 },
                }
            });

            // 4. Basse (5 cordes)
            Instruments.Add(new TunerInstrument
            {
                Id = "bass_5",
                Name = "Basse (5 cordes)",
                Icon = "🎸",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "5", NoteName = "B0", Solfege = "Si0", Frequency = 30.87, MidiNote = 23 },
                    new TuningNote { StringLabel = "4", NoteName = "E1", Solfege = "Mi1", Frequency = 41.20, MidiNote = 28 },
                    new TuningNote { StringLabel = "3", NoteName = "A1", Solfege = "La1", Frequency = 55.00, MidiNote = 33 },
                    new TuningNote { StringLabel = "2", NoteName = "D2", Solfege = "Ré2", Frequency = 73.42, MidiNote = 38 },
                    new TuningNote { StringLabel = "1", NoteName = "G2", Solfege = "Sol2", Frequency = 98.00, MidiNote = 43 },
                }
            });

            // 5. Violon
            Instruments.Add(new TunerInstrument
            {
                Id = "violin",
                Name = "Violon",
                Icon = "🎻",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "4", NoteName = "G3", Solfege = "Sol3", Frequency = 196.00, MidiNote = 55 },
                    new TuningNote { StringLabel = "3", NoteName = "D4", Solfege = "Ré4", Frequency = 293.66, MidiNote = 62 },
                    new TuningNote { StringLabel = "2", NoteName = "A4", Solfege = "La4", Frequency = 440.00, MidiNote = 69 },
                    new TuningNote { StringLabel = "1", NoteName = "E5", Solfege = "Mi5", Frequency = 659.25, MidiNote = 76 },
                }
            });

            // 6. Alto (Viola)
            Instruments.Add(new TunerInstrument
            {
                Id = "viola",
                Name = "Alto (Viola)",
                Icon = "🎻",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "4", NoteName = "C3", Solfege = "Do3", Frequency = 130.81, MidiNote = 48 },
                    new TuningNote { StringLabel = "3", NoteName = "G3", Solfege = "Sol3", Frequency = 196.00, MidiNote = 55 },
                    new TuningNote { StringLabel = "2", NoteName = "D4", Solfege = "Ré4", Frequency = 293.66, MidiNote = 62 },
                    new TuningNote { StringLabel = "1", NoteName = "A4", Solfege = "La4", Frequency = 440.00, MidiNote = 69 },
                }
            });

            // 7. Violoncelle (Cello)
            Instruments.Add(new TunerInstrument
            {
                Id = "cello",
                Name = "Violoncelle",
                Icon = "🎻",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "4", NoteName = "C2", Solfege = "Do2", Frequency = 65.41, MidiNote = 36 },
                    new TuningNote { StringLabel = "3", NoteName = "G2", Solfege = "Sol2", Frequency = 98.00, MidiNote = 43 },
                    new TuningNote { StringLabel = "2", NoteName = "D3", Solfege = "Ré3", Frequency = 146.83, MidiNote = 50 },
                    new TuningNote { StringLabel = "1", NoteName = "A3", Solfege = "La3", Frequency = 220.00, MidiNote = 57 },
                }
            });

            // 8. Contrebasse
            Instruments.Add(new TunerInstrument
            {
                Id = "double_bass",
                Name = "Contrebasse",
                Icon = "🎻",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "4", NoteName = "E1", Solfege = "Mi1", Frequency = 41.20, MidiNote = 28 },
                    new TuningNote { StringLabel = "3", NoteName = "A1", Solfege = "La1", Frequency = 55.00, MidiNote = 33 },
                    new TuningNote { StringLabel = "2", NoteName = "D2", Solfege = "Ré2", Frequency = 73.42, MidiNote = 38 },
                    new TuningNote { StringLabel = "1", NoteName = "G2", Solfege = "Sol2", Frequency = 98.00, MidiNote = 43 },
                }
            });

            // 9. Ukulélé Standard (G C E A)
            Instruments.Add(new TunerInstrument
            {
                Id = "ukulele",
                Name = "Ukulélé (Standard C)",
                Icon = "🪕",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "4", NoteName = "G4", Solfege = "Sol4", Frequency = 392.00, MidiNote = 67 },
                    new TuningNote { StringLabel = "3", NoteName = "C4", Solfege = "Do4", Frequency = 261.63, MidiNote = 60 },
                    new TuningNote { StringLabel = "2", NoteName = "E4", Solfege = "Mi4", Frequency = 329.63, MidiNote = 64 },
                    new TuningNote { StringLabel = "1", NoteName = "A4", Solfege = "La4", Frequency = 440.00, MidiNote = 69 },
                }
            });
        }

        public async Task<bool> StartListeningAsync()
        {
            if (IsListening) return true;

            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    return false;
                }
            }

            _listeningCts = new CancellationTokenSource();
            IsListening = true;

            var token = _listeningCts.Token;
            _ = Task.Run(() => CaptureAndAnalyzeLoop(token), token);

            return true;
        }

        public void StopListening()
        {
            IsListening = false;
            _listeningCts?.Cancel();
            _listeningCts?.Dispose();
            _listeningCts = null;

            // Envoi d'un événement neutre
            PitchDetected?.Invoke(this, new PitchDetectedEventArgs { HasSignal = false });
        }

        private void CaptureAndAnalyzeLoop(CancellationToken token)
        {
#if ANDROID
            AudioRecord? audioRecord = null;
            try
            {
                int minBuf = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
                int recordBufSize = Math.Max(minBuf, BufferSize * 2);

                audioRecord = new AudioRecord(
                    AudioSource.Mic,
                    SampleRate,
                    ChannelIn.Mono,
                    Encoding.Pcm16bit,
                    recordBufSize);

                if (audioRecord.State != State.Initialized)
                {
                    return;
                }

                audioRecord.StartRecording();
                short[] pcmBuffer = new short[BufferSize];

                while (!token.IsCancellationRequested && IsListening)
                {
                    int read = audioRecord.Read(pcmBuffer, 0, pcmBuffer.Length);
                    if (read > 0)
                    {
                        AnalyzePcmBuffer(pcmBuffer, read);
                    }
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TunerService] Android capture error: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (audioRecord != null)
                    {
                        if (audioRecord.RecordingState == RecordState.Recording)
                        {
                            audioRecord.Stop();
                        }
                        audioRecord.Release();
                        audioRecord.Dispose();
                    }
                }
                catch { }
            }
#else
            // Fallback non-Android (simulation douce ou attente)
            while (!token.IsCancellationRequested && IsListening)
            {
                Thread.Sleep(100);
            }
#endif
        }

        public void AnalyzePcmBuffer(short[] buffer, int count)
        {
            if (count < 512) return;

            // 1. Calcul du volume RMS
            double sumSquares = 0;
            for (int i = 0; i < count; i++)
            {
                double norm = buffer[i] / 32768.0;
                sumSquares += norm * norm;
            }
            double rms = Math.Sqrt(sumSquares / count);

            // Seuil de silence / bruit ambiant
            if (rms < 0.015)
            {
                PitchDetected?.Invoke(this, new PitchDetectedEventArgs
                {
                    HasSignal = false,
                    Amplitude = rms
                });
                return;
            }

            // 2. Détection de pitch par autocorrélation
            // Plage de fréquences utiles pour instruments : 28 Hz (B0 basse) à 1100 Hz (C6)
            int minLag = (int)(SampleRate / 1100.0);
            int maxLag = (int)(SampleRate / 28.0);
            maxLag = Math.Min(maxLag, count / 2);

            double bestCorrelation = 0;
            int bestLag = -1;

            // Normalisation à lag 0
            double r0 = 0;
            for (int i = 0; i < count - maxLag; i++)
            {
                r0 += buffer[i] * buffer[i];
            }

            if (r0 <= 0) return;

            // Trouver le premier creux puis le pic
            bool foundValley = false;
            double prevR = double.MaxValue;

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                double r = 0;
                for (int i = 0; i < count - maxLag; i++)
                {
                    r += (double)buffer[i] * buffer[i + lag];
                }

                if (!foundValley)
                {
                    if (r < prevR)
                    {
                        prevR = r;
                    }
                    else
                    {
                        foundValley = true;
                    }
                }
                else
                {
                    if (r > bestCorrelation)
                    {
                        bestCorrelation = r;
                        bestLag = lag;
                    }
                }
            }

            double normalizedCorrelation = bestCorrelation / r0;

            if (bestLag <= 0 || normalizedCorrelation < 0.35)
            {
                PitchDetected?.Invoke(this, new PitchDetectedEventArgs
                {
                    HasSignal = false,
                    Amplitude = rms
                });
                return;
            }

            // 3. Interpolation parabolique du pic pour une précision au centième de Hz
            double refinedLag = bestLag;
            if (bestLag > minLag && bestLag < maxLag)
            {
                double rLeft = 0;
                double rRight = 0;
                for (int i = 0; i < count - maxLag; i++)
                {
                    rLeft += (double)buffer[i] * buffer[i + (bestLag - 1)];
                    rRight += (double)buffer[i] * buffer[i + (bestLag + 1)];
                }

                double denom = 2.0 * (rLeft - 2.0 * bestCorrelation + rRight);
                if (Math.Abs(denom) > 1e-6)
                {
                    double delta = (rLeft - rRight) / denom;
                    refinedLag += Math.Clamp(delta, -1.0, 1.0);
                }
            }

            double frequency = SampleRate / refinedLag;

            if (frequency < 25 || frequency > 1200)
            {
                PitchDetected?.Invoke(this, new PitchDetectedEventArgs { HasSignal = false, Amplitude = rms });
                return;
            }

            // 4. Calcul de la note MIDI la plus proche et de la déviation en cents
            // MIDI 69 = A4 = 440 Hz
            double midiNumber = 69.0 + 12.0 * Math.Log2(frequency / 440.0);
            int nearestMidi = (int)Math.Round(midiNumber);
            double cents = (midiNumber - nearestMidi) * 100.0;
            double targetFreq = 440.0 * Math.Pow(2.0, (nearestMidi - 69.0) / 12.0);

            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            string[] solfegeNames = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };

            int noteIndex = (nearestMidi % 12 + 12) % 12;
            int octave = (nearestMidi / 12) - 1;

            string noteName = $"{noteNames[noteIndex]}{octave}";
            string solfegeName = $"{solfegeNames[noteIndex]}{octave}";

            PitchDetected?.Invoke(this, new PitchDetectedEventArgs
            {
                Frequency = Math.Round(frequency, 1),
                TargetFrequency = Math.Round(targetFreq, 1),
                Cents = Math.Round(cents, 1),
                NoteName = noteName,
                SolfegeName = solfegeName,
                Octave = octave,
                Amplitude = rms,
                HasSignal = true
            });
        }

        public string GenerateReferenceToneWav(double frequencyHz, double durationSec = 1.8)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * durationSec);
            short[] samples = new short[totalSamples];

            int attackSamples = (int)(sampleRate * 0.05); // 50ms fade in
            int decaySamples = (int)(sampleRate * 0.15);  // 150ms fade out

            for (int i = 0; i < totalSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = 1.0;
                if (i < attackSamples) env = (double)i / attackSamples;
                else if (i > totalSamples - decaySamples) env = (double)(totalSamples - i) / decaySamples;

                // Onde sinusoïdale pure avec harmoniques douces pour un timbre musical agréable
                double sin1 = Math.Sin(2.0 * Math.PI * frequencyHz * t);
                double sin2 = 0.25 * Math.Sin(4.0 * Math.PI * frequencyHz * t);
                double val = (sin1 + sin2) * 0.8 * env;

                samples[i] = (short)(Math.Clamp(val, -1.0, 1.0) * 32767);
            }

            string tempPath = Path.Combine(FileSystem.CacheDirectory, "tuner_reference_tone.wav");
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                // WAV Header
                bw.Write("RIFF"u8.ToArray());
                bw.Write(36 + totalSamples * 2);
                bw.Write("WAVE"u8.ToArray());
                bw.Write("fmt "u8.ToArray());
                bw.Write(16); // PCM Chunk size
                bw.Write((short)1); // Audio format 1 = PCM
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2); // Byte rate
                bw.Write((short)2); // Block align
                bw.Write((short)16); // Bits per sample
                bw.Write("data"u8.ToArray());
                bw.Write(totalSamples * 2);

                for (int i = 0; i < totalSamples; i++)
                {
                    bw.Write(samples[i]);
                }
            }

            return tempPath;
        }
    }
}
