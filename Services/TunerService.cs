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
        public double A4Frequency { get; set; } = 440.0;

        public List<TunerInstrument> Instruments { get; } = new();

        // DSP & Stabilisation temporelle (filtre passe-bas, YIN, historique, médiane et hystérésis)
        private readonly double[] _filteredBuffer = new double[BufferSize];
        private readonly double[] _dBuffer = new double[BufferSize];
        private readonly double[] _dPrimeBuffer = new double[BufferSize];

        private readonly Queue<double> _frequencyHistory = new();
        private const int MaxHistoryCount = 5;
        private string _lastStableNoteName = string.Empty;
        private string _lastStableSolfegeName = string.Empty;
        private int _lastStableMidi = -1;
        private string _pendingNoteName = string.Empty;
        private int _pendingNoteCount = 0;
        private double _smoothedCents = 0.0;
        private bool _hasPreviousCents = false;
        private int _silentFrameCount = 0;

        public void ResetStabilityHistory()
        {
            lock (_frequencyHistory)
            {
                _frequencyHistory.Clear();
            }
            _lastStableNoteName = string.Empty;
            _lastStableSolfegeName = string.Empty;
            _lastStableMidi = -1;
            _pendingNoteName = string.Empty;
            _pendingNoteCount = 0;
            _smoothedCents = 0.0;
            _hasPreviousCents = false;
        }

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

            // 10. Saxophone Alto (en Mi♭) - Notes d'accordage de référence (concert / son réel)
            Instruments.Add(new TunerInstrument
            {
                Id = "sax_alto",
                Name = "Saxophone Alto (en Mi♭)",
                Icon = "🎷",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "La concert (Fa#4 sax)", NoteName = "A3", Solfege = "La3 (Fa#4 sax)", Frequency = 220.00, MidiNote = 57 },
                    new TuningNote { StringLabel = "Si♭ concert (Do4 sax)", NoteName = "A#3", Solfege = "Si♭3 (Do4 sax)", Frequency = 233.08, MidiNote = 58 },
                    new TuningNote { StringLabel = "Fa concert (Ré4 sax)", NoteName = "F3", Solfege = "Fa3 (Ré4 sax)", Frequency = 174.61, MidiNote = 53 },
                    new TuningNote { StringLabel = "La4 concert (Fa#5 sax)", NoteName = "A4", Solfege = "La4 (Fa#5 sax)", Frequency = 440.00, MidiNote = 69 },
                    new TuningNote { StringLabel = "Ré concert (Si3 sax)", NoteName = "D4", Solfege = "Ré4 (Si3 sax)", Frequency = 293.66, MidiNote = 62 },
                }
            });

            // 11. Saxophone Ténor (en Si♭)
            Instruments.Add(new TunerInstrument
            {
                Id = "sax_tenor",
                Name = "Saxophone Ténor (en Si♭)",
                Icon = "🎷",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "Si♭ concert (Do4 sax)", NoteName = "A#3", Solfege = "Si♭3 (Do4 sax)", Frequency = 233.08, MidiNote = 58 },
                    new TuningNote { StringLabel = "La concert (Si3 sax)", NoteName = "A3", Solfege = "La3 (Si3 sax)", Frequency = 220.00, MidiNote = 57 },
                    new TuningNote { StringLabel = "Fa concert (Sol3 sax)", NoteName = "F3", Solfege = "Fa3 (Sol3 sax)", Frequency = 174.61, MidiNote = 53 },
                    new TuningNote { StringLabel = "Si♭ grave (Do3 sax)", NoteName = "A#2", Solfege = "Si♭2 (Do3 sax)", Frequency = 116.54, MidiNote = 46 },
                    new TuningNote { StringLabel = "Ré concert (Mi4 sax)", NoteName = "D4", Solfege = "Ré4 (Mi4 sax)", Frequency = 293.66, MidiNote = 62 },
                }
            });

            // 12. Trompette & Clarinette (en Si♭)
            Instruments.Add(new TunerInstrument
            {
                Id = "trumpet_clarinet",
                Name = "Trompette / Clarinette (en Si♭)",
                Icon = "🎺",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "Si♭ concert (Do4)", NoteName = "A#3", Solfege = "Si♭3 (Do4)", Frequency = 233.08, MidiNote = 58 },
                    new TuningNote { StringLabel = "La concert (Si3)", NoteName = "A3", Solfege = "La3 (Si3)", Frequency = 220.00, MidiNote = 57 },
                    new TuningNote { StringLabel = "Fa concert (Sol4)", NoteName = "F4", Solfege = "Fa4 (Sol4)", Frequency = 349.23, MidiNote = 65 },
                    new TuningNote { StringLabel = "Si♭ aigu (Do5)", NoteName = "A#4", Solfege = "Si♭4 (Do5)", Frequency = 466.16, MidiNote = 70 },
                }
            });

            // 13. Flûte Traversière (en Ut)
            Instruments.Add(new TunerInstrument
            {
                Id = "flute",
                Name = "Flûte Traversière (en Ut)",
                Icon = "🪈",
                Notes = new List<TuningNote>
                {
                    new TuningNote { StringLabel = "La4 standard", NoteName = "A4", Solfege = "La4", Frequency = 440.00, MidiNote = 69 },
                    new TuningNote { StringLabel = "Ré4", NoteName = "D4", Solfege = "Ré4", Frequency = 293.66, MidiNote = 62 },
                    new TuningNote { StringLabel = "Sol4", NoteName = "G4", Solfege = "Sol4", Frequency = 392.00, MidiNote = 67 },
                    new TuningNote { StringLabel = "La5", NoteName = "A5", Solfege = "La5", Frequency = 880.00, MidiNote = 81 },
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

            ResetStabilityHistory();

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
                _silentFrameCount++;
                if (_silentFrameCount >= 4)
                {
                    ResetStabilityHistory();
                }

                PitchDetected?.Invoke(this, new PitchDetectedEventArgs
                {
                    HasSignal = false,
                    Amplitude = rms
                });
                return;
            }

            _silentFrameCount = 0;

            // 2. Pré-filtrage passe-bas à 2 pôles (~1200 Hz)
            // Élimine le grésillement des anches, le souffle d'air et les harmoniques très aigus (> 1.2 kHz)
            // qui faussent la détection sur les instruments à vent (saxophone, hautbois, clarinette).
            double alpha = 0.146; // Cutoff ~1200 Hz à 44.1 kHz
            double y1 = buffer[0];
            double y2 = buffer[0];
            int processCount = Math.Min(count, BufferSize);
            for (int i = 0; i < processCount; i++)
            {
                y1 += alpha * (buffer[i] - y1);
                y2 += alpha * (y1 - y2);
                _filteredBuffer[i] = y2;
            }

            // 3. Algorithme YIN pour la détection de pitch
            // Plage utile : 28 Hz (Si0 contrebasse / basse 5c) à 1100 Hz (Do6 flûte/violon)
            int minLag = (int)(SampleRate / 1100.0); // ~40
            int maxLag = (int)(SampleRate / 28.0);   // ~1575
            int windowSize = 1200;
            maxLag = Math.Min(maxLag, processCount - windowSize - 5);
            if (maxLag <= minLag) return;

            _dPrimeBuffer[0] = 1.0;
            double runningSum = 0;

            // Calcul de la fonction de différence au carré d(tau) et de la moyenne cumulative normalisée d'(tau)
            for (int tau = 1; tau <= maxLag; tau++)
            {
                double sum = 0;
                for (int j = 0; j < windowSize; j++)
                {
                    double diff = _filteredBuffer[j] - _filteredBuffer[j + tau];
                    sum += diff * diff;
                }
                _dBuffer[tau] = sum;
                runningSum += sum;
                _dPrimeBuffer[tau] = runningSum > 0 ? (sum * tau) / runningSum : 1.0;
            }

            // 4. Recherche du premier creux YIN avec seuil absolu
            double threshold = 0.15;
            int chosenLag = -1;
            bool enteredBelowThreshold = false;
            double bestGlobalVal = double.MaxValue;
            int bestGlobalLag = -1;

            for (int tau = minLag; tau <= maxLag; tau++)
            {
                double val = _dPrimeBuffer[tau];
                if (val < bestGlobalVal)
                {
                    bestGlobalVal = val;
                    bestGlobalLag = tau;
                }

                if (!enteredBelowThreshold)
                {
                    if (val < threshold)
                    {
                        enteredBelowThreshold = true;
                    }
                }
                else
                {
                    if (val > _dPrimeBuffer[tau - 1])
                    {
                        chosenLag = tau - 1;
                        break;
                    }
                }
            }

            // Si aucun creux n'est descendu sous 0.15, on accepte le minimum global s'il est net (< 0.38)
            if (chosenLag < 0)
            {
                if (bestGlobalLag >= minLag && bestGlobalVal < 0.38)
                {
                    chosenLag = bestGlobalLag;
                }
                else
                {
                    PitchDetected?.Invoke(this, new PitchDetectedEventArgs
                    {
                        HasSignal = false,
                        Amplitude = rms
                    });
                    return;
                }
            }

            // 5. Correction des erreurs d'octave et d'harmoniques dominants (Saxophone & Instruments à vent)
            // Sur un saxophone ou un violoncelle, le 2e harmonique (octave supérieure) ou 3e harmonique (quinte)
            // a souvent une amplitude acoustique plus forte que la fondamentale (f0) en raison de la perce conique.
            // On vérifie si un creux existe aussi à 2*tau ou 3*tau pour identifier la véritable fondamentale.
            if (chosenLag > 0)
            {
                // Vérification du sous-harmonique 2x (l'octave inférieure)
                int subLag2 = chosenLag * 2;
                if (subLag2 + 3 <= maxLag)
                {
                    int minSub2 = subLag2;
                    double minVal2 = double.MaxValue;
                    for (int t = Math.Max(minLag, subLag2 - 4); t <= Math.Min(maxLag, subLag2 + 4); t++)
                    {
                        if (_dPrimeBuffer[t] < minVal2)
                        {
                            minVal2 = _dPrimeBuffer[t];
                            minSub2 = t;
                        }
                    }

                    if (minVal2 < 0.30 && (minVal2 - _dPrimeBuffer[chosenLag] < 0.14))
                    {
                        chosenLag = minSub2;
                    }
                }

                // Vérification du sous-harmonique 3x (3e harmonique dominant)
                int subLag3 = chosenLag * 3;
                if (subLag3 + 4 <= maxLag)
                {
                    int minSub3 = subLag3;
                    double minVal3 = double.MaxValue;
                    for (int t = Math.Max(minLag, subLag3 - 5); t <= Math.Min(maxLag, subLag3 + 5); t++)
                    {
                        if (_dPrimeBuffer[t] < minVal3)
                        {
                            minVal3 = _dPrimeBuffer[t];
                            minSub3 = t;
                        }
                    }

                    if (minVal3 < 0.32 && (minVal3 - _dPrimeBuffer[chosenLag] < 0.12))
                    {
                        chosenLag = minSub3;
                    }
                }
            }

            // 6. Interpolation parabolique pour une précision sous-échantillon
            double refinedLag = chosenLag;
            if (chosenLag > minLag && chosenLag < maxLag)
            {
                double s0 = _dPrimeBuffer[chosenLag - 1];
                double s1 = _dPrimeBuffer[chosenLag];
                double s2 = _dPrimeBuffer[chosenLag + 1];
                double denom = 2.0 * (s0 - 2.0 * s1 + s2);
                if (Math.Abs(denom) > 1e-9)
                {
                    double delta = (s0 - s2) / denom;
                    refinedLag += Math.Clamp(delta, -1.0, 1.0);
                }
            }

            double rawFrequency = SampleRate / refinedLag;
            if (rawFrequency < 25 || rawFrequency > 1200)
            {
                PitchDetected?.Invoke(this, new PitchDetectedEventArgs { HasSignal = false, Amplitude = rms });
                return;
            }

            // 7. Filtre médian glissant (5 échantillons) pour éliminer les sursauts transitoires
            double frequency;
            lock (_frequencyHistory)
            {
                _frequencyHistory.Enqueue(rawFrequency);
                while (_frequencyHistory.Count > MaxHistoryCount)
                {
                    _frequencyHistory.Dequeue();
                }
                var sorted = _frequencyHistory.OrderBy(x => x).ToList();
                frequency = sorted[sorted.Count / 2];
            }

            // 8. Calcul de la note MIDI et de la déviation en cents selon A4Frequency
            double a4Ref = A4Frequency > 100.0 ? A4Frequency : 440.0;
            double midiNumber = 69.0 + 12.0 * Math.Log2(frequency / a4Ref);
            int nearestMidi = (int)Math.Round(midiNumber);
            double rawCents = (midiNumber - nearestMidi) * 100.0;
            double targetFreq = a4Ref * Math.Pow(2.0, (nearestMidi - 69.0) / 12.0);

            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            string[] solfegeNames = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };

            int noteIndex = (nearestMidi % 12 + 12) % 12;
            int octave = (nearestMidi / 12) - 1;

            string candidateNoteName = $"{noteNames[noteIndex]}{octave}";
            string candidateSolfege = $"{solfegeNames[noteIndex]}{octave}";

            // 9. Hystérésis de note : empêche le scintillement visuel entre deux notes adjacentes
            if (string.IsNullOrEmpty(_lastStableNoteName))
            {
                _lastStableNoteName = candidateNoteName;
                _lastStableSolfegeName = candidateSolfege;
                _lastStableMidi = nearestMidi;
            }
            else if (candidateNoteName == _lastStableNoteName)
            {
                _pendingNoteName = string.Empty;
                _pendingNoteCount = 0;
            }
            else
            {
                if (candidateNoteName == _pendingNoteName)
                {
                    _pendingNoteCount++;
                    if (_pendingNoteCount >= 2)
                    {
                        _lastStableNoteName = candidateNoteName;
                        _lastStableSolfegeName = candidateSolfege;
                        _lastStableMidi = nearestMidi;
                        _pendingNoteName = string.Empty;
                        _pendingNoteCount = 0;
                    }
                }
                else
                {
                    _pendingNoteName = candidateNoteName;
                    _pendingNoteCount = 1;
                }
            }

            // Calcul des cents par rapport à la note stable affichée
            if (_lastStableMidi >= 0)
            {
                double stableTarget = a4Ref * Math.Pow(2.0, (_lastStableMidi - 69.0) / 12.0);
                double stableCents = 1200.0 * Math.Log2(frequency / stableTarget);
                if (Math.Abs(stableCents) <= 65.0)
                {
                    rawCents = stableCents;
                    targetFreq = stableTarget;
                    octave = (_lastStableMidi / 12) - 1;
                }
            }

            // 10. Lissage exponentiel (EMA) de l'aiguille des cents
            if (!_hasPreviousCents)
            {
                _smoothedCents = rawCents;
                _hasPreviousCents = true;
            }
            else
            {
                _smoothedCents = _smoothedCents * 0.55 + rawCents * 0.45;
            }

            PitchDetected?.Invoke(this, new PitchDetectedEventArgs
            {
                Frequency = Math.Round(frequency, 1),
                TargetFrequency = Math.Round(targetFreq, 1),
                Cents = Math.Round(_smoothedCents, 1),
                NoteName = _lastStableNoteName,
                SolfegeName = _lastStableSolfegeName,
                Octave = octave,
                Amplitude = rms,
                HasSignal = true
            });
        }

        public static TuningNote CreateChromaticNote(int noteIndex, int octave, double a4Ref = 440.0)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            string[] solfegeNames = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };

            noteIndex = (noteIndex % 12 + 12) % 12;
            int midiNote = (octave + 1) * 12 + noteIndex;
            double refFreq = a4Ref > 100.0 ? a4Ref : 440.0;
            double freq = refFreq * Math.Pow(2.0, (midiNote - 69.0) / 12.0);

            return new TuningNote
            {
                NoteName = $"{noteNames[noteIndex]}{octave}",
                Solfege = $"{solfegeNames[noteIndex]}{octave}",
                StringLabel = $"{solfegeNames[noteIndex]}",
                MidiNote = midiNote,
                Frequency = Math.Round(freq, 2)
            };
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
