using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;
using MusicScoreManager.Models;

namespace MusicScoreManager.Services
{
    public class PedalMidiService
    {
        private static readonly Lazy<PedalMidiService> _instance = new(() => new PedalMidiService());
        public static PedalMidiService Instance => _instance.Value;

        private const string ActiveProfileIdKey = "MSM_ActivePedalProfileId";
        private const string CustomProfilesKey = "MSM_CustomPedalProfiles_Json";
        private const string LongPressThresholdKey = "MSM_PedalLongPressThresholdMs";
        private const string IsPedalEnabledKey = "MSM_PedalServiceEnabled";

        private readonly Dictionary<int, (DateTime PressTime, System.Threading.Timer? Timer)> _activeKeyDowns = new();
        private readonly object _lock = new();

        public event Action<PedalAction>? ActionTriggered;
        public event Action<PedalRawEvent>? RawEventReceived;
        public event Action? ActiveProfileChanged;

        public bool IsEnabled
        {
            get => Preferences.Get(IsPedalEnabledKey, true);
            set => Preferences.Set(IsPedalEnabledKey, value);
        }

        public int LongPressThresholdMs
        {
            get => Preferences.Get(LongPressThresholdKey, 450);
            set => Preferences.Set(LongPressThresholdKey, value);
        }

        public List<PedalProfile> Profiles { get; private set; } = new();

        private PedalProfile _activeProfile;
        public PedalProfile ActiveProfile
        {
            get => _activeProfile;
            set
            {
                if (value != null && _activeProfile?.Id != value.Id)
                {
                    _activeProfile = value;
                    Preferences.Set(ActiveProfileIdKey, value.Id);
                    ActiveProfileChanged?.Invoke();
                }
            }
        }

        public PedalMidiService()
        {
            InitializeProfiles();
            string savedActiveId = Preferences.Get(ActiveProfileIdKey, "preset_standard");
            _activeProfile = Profiles.FirstOrDefault(p => p.Id == savedActiveId) ?? Profiles.First();
        }

        public void InitializeProfiles()
        {
            Profiles = new List<PedalProfile>();

            // 1. Profil Standard (Défaut)
            Profiles.Add(new PedalProfile
            {
                Id = "preset_standard",
                Name = "Standard (Flèches / Page Up-Down / Espace)",
                Description = "Compatible avec toutes les pédales standards et claviers Bluetooth (PageUp, PageDown, Flèches, Espace, Entrée).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Page suivante (Long: Morceau suivant)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Page précédente (Long: Morceau précédent)" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Page suivante" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Page précédente" },
                    new() { KeyName = "ArrowDown", KeyCode = 20, Action = PedalAction.ScrollDown, Description = "Défiler vers le bas" },
                    new() { KeyName = "ArrowUp", KeyCode = 19, Action = PedalAction.ScrollUp, Description = "Défiler vers le haut" },
                    new() { KeyName = "Space", KeyCode = 62, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { KeyName = "Return", KeyCode = 66, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { KeyName = "Backspace", KeyCode = 67, Action = PedalAction.PreviousPage, Description = "Page précédente" },
                    new() { KeyName = "Home", KeyCode = 122, Action = PedalAction.FirstPage, Description = "Première page" },
                    new() { KeyName = "End", KeyCode = 123, Action = PedalAction.LastPage, Description = "Dernière page" },
                    // Support MIDI de base
                    new() { InputType = PedalInputType.MidiCC, KeyCode = 64, KeyName = "MIDI CC 64 (Sustain)", Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Pédale forte / Sustain" },
                    new() { InputType = PedalInputType.MidiCC, KeyCode = 66, KeyName = "MIDI CC 66 (Sostenuto)", Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Pédale tonale / Sostenuto" },
                    new() { InputType = PedalInputType.MidiCC, KeyCode = 67, KeyName = "MIDI CC 67 (Soft)", Action = PedalAction.ToggleMetronome, Description = "Pédale douce / Soft" }
                }
            });

            // 2. PageFlip Dragonfly (4 Pédales)
            Profiles.Add(new PedalProfile
            {
                Id = "preset_pageflip_dragonfly",
                Name = "PageFlip Dragonfly (4 Pédales)",
                Description = "Profil optimisé pour les 4 pédales du PageFlip Dragonfly (Tourne-page + Métronome + Audio).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Pédale Droite Principale" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Pédale Gauche Principale" },
                    new() { KeyName = "F3", KeyCode = 133, Action = PedalAction.ToggleMetronome, Description = "Pédale Auxiliaire 1 (Métronome)" },
                    new() { KeyName = "F4", KeyCode = 134, Action = PedalAction.ToggleAudio, Description = "Pédale Auxiliaire 2 (Piste Audio)" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, Description = "Mode Flèches Droite" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, Description = "Mode Flèches Gauche" }
                }
            });

            // 3. PageFlip Firefly / Butterfly
            Profiles.Add(new PedalProfile
            {
                Id = "preset_pageflip_firefly",
                Name = "PageFlip Firefly & Butterfly",
                Description = "Profil complet pour PageFlip Firefly (avec prises jack auxiliaires) et Butterfly.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Pédale Droite (Page suiv / Morceau suiv)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Pédale Gauche (Page préc / Morceau préc)" },
                    new() { KeyName = "ArrowDown", KeyCode = 20, Action = PedalAction.ScrollDown, Description = "Pédale Auxiliaire Bas" },
                    new() { KeyName = "ArrowUp", KeyCode = 19, Action = PedalAction.ScrollUp, Description = "Pédale Auxiliaire Haut" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, Description = "Mode 3 (Flèche Droite)" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, Description = "Mode 3 (Flèche Gauche)" }
                }
            });

            // 4. AirTurn Duo 500 / PEDpro / BT200
            Profiles.Add(new PedalProfile
            {
                Id = "preset_airturn_duo",
                Name = "AirTurn Duo 500 / PEDpro",
                Description = "Support des modes AirTurn (Mode 3 PageUp/Down, Mode 2 Flèches, Mode 1 Flèches verticales).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Switch 2 (Mode 3)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Switch 1 (Mode 3)" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Switch 2 (Mode 2)" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Switch 1 (Mode 2)" },
                    new() { KeyName = "ArrowDown", KeyCode = 20, Action = PedalAction.ScrollDown, Description = "Switch 2 (Mode 1)" },
                    new() { KeyName = "ArrowUp", KeyCode = 19, Action = PedalAction.ScrollUp, Description = "Switch 1 (Mode 1)" }
                }
            });

            // 5. AirTurn Quad 500 (4 Boutons)
            Profiles.Add(new PedalProfile
            {
                Id = "preset_airturn_quad",
                Name = "AirTurn Quad 500 (4 Pédales)",
                Description = "Configuration pour les 4 switches du pédalier AirTurn Quad 500.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, Description = "Switch 2 (Page suivante)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, Description = "Switch 1 (Page précédente)" },
                    new() { KeyName = "Digit1", KeyCode = 8, Action = PedalAction.ToggleMetronome, Description = "Switch 3 (Métronome On/Off)" },
                    new() { KeyName = "Digit2", KeyCode = 9, Action = PedalAction.ToggleAudio, Description = "Switch 4 (Lecture Audio)" },
                    new() { KeyName = "ArrowUp", KeyCode = 19, Action = PedalAction.ToggleAnnotationsLock, Description = "Mode Flèches Haut (Cadenas)" },
                    new() { KeyName = "ArrowDown", KeyCode = 20, Action = PedalAction.OpenPageJump, Description = "Mode Flèches Bas (Saut Page)" }
                }
            });

            // 6. Joyo JSP-01
            Profiles.Add(new PedalProfile
            {
                Id = "preset_joyo_jsp01",
                Name = "Joyo JSP-01 Wireless Page Turner",
                Description = "Pédalier sans fil Joyo JSP-01 (Modes PageUp/Down et Flèches).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Pédale Droite (Page suivante)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Pédale Gauche (Page précédente)" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, Description = "Mode 2 Droite" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, Description = "Mode 2 Gauche" }
                }
            });

            // 7. Thomann / Harley Benton PageTurn Pedal
            Profiles.Add(new PedalProfile
            {
                Id = "preset_thomann_pageturn",
                Name = "Thomann / Harley Benton PageTurn",
                Description = "Pédale Bluetooth Thomann / Harley Benton (5 modes de bascule).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Mode 1 Droite (PageDown)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Mode 1 Gauche (PageUp)" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, Description = "Mode 2 Droite (Flèche)" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, Description = "Mode 2 Gauche (Flèche)" },
                    new() { KeyName = "ArrowDown", KeyCode = 20, Action = PedalAction.ScrollDown, Description = "Mode 3 Bas" },
                    new() { KeyName = "ArrowUp", KeyCode = 19, Action = PedalAction.ScrollUp, Description = "Mode 3 Haut" }
                }
            });

            // 8. Donner Wireless Page Turner
            Profiles.Add(new PedalProfile
            {
                Id = "preset_donner_turner",
                Name = "Donner Wireless Page Turner",
                Description = "Pédalier Donner 5 modes (PageUp/Down, Flèches, Espace/Entrée).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Pédale Droite (PageDown)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Pédale Gauche (PageUp)" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, Description = "Pédale Droite (Flèche)" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, Description = "Pédale Gauche (Flèche)" },
                    new() { KeyName = "Space", KeyCode = 62, Action = PedalAction.NextPage, Description = "Mode Espace / Entrée" },
                    new() { KeyName = "Return", KeyCode = 66, Action = PedalAction.NextPage, Description = "Mode Espace / Entrée" }
                }
            });

            // 9. iRig BlueTurn
            Profiles.Add(new PedalProfile
            {
                Id = "preset_irig_blueturn",
                Name = "IK Multimedia iRig BlueTurn",
                Description = "Pédalier compact rétroéclairé iRig BlueTurn (Modes PageUp/Down, ArrowUp/Down, ArrowLeft/Right).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Bouton Droit (Mode 1)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Bouton Gauche (Mode 1)" },
                    new() { KeyName = "ArrowDown", KeyCode = 20, Action = PedalAction.NextPage, Description = "Bouton Droit (Mode 2)" },
                    new() { KeyName = "ArrowUp", KeyCode = 19, Action = PedalAction.PreviousPage, Description = "Bouton Gauche (Mode 2)" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, Description = "Bouton Droit (Mode 3)" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, Description = "Bouton Gauche (Mode 3)" }
                }
            });

            // 10. Coda Music Stomp
            Profiles.Add(new PedalProfile
            {
                Id = "preset_coda_stomp",
                Name = "Coda Music Technologies STOMP",
                Description = "Pédalier en aluminium ultra-robuste STOMP (Modes Page, Flèches et Audio).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { KeyName = "PageDown", KeyCode = 93, Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Footswitch Droit (Mode 1)" },
                    new() { KeyName = "PageUp", KeyCode = 92, Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Footswitch Gauche (Mode 1)" },
                    new() { KeyName = "ArrowRight", KeyCode = 22, Action = PedalAction.NextPage, Description = "Footswitch Droit (Mode 2)" },
                    new() { KeyName = "ArrowLeft", KeyCode = 21, Action = PedalAction.PreviousPage, Description = "Footswitch Gauche (Mode 2)" },
                    new() { KeyName = "ArrowDown", KeyCode = 20, Action = PedalAction.ScrollDown, Description = "Footswitch Droit (Mode 3)" },
                    new() { KeyName = "ArrowUp", KeyCode = 19, Action = PedalAction.ScrollUp, Description = "Footswitch Gauche (Mode 3)" }
                }
            });

            // 11. Contrôleur MIDI Pro (USB / Bluetooth)
            Profiles.Add(new PedalProfile
            {
                Id = "preset_midi_controller",
                Name = "Contrôleur MIDI Avancé (USB / Bluetooth)",
                Description = "Pédaliers et claviers MIDI (Pédales Sustain/CC 64, Sostenuto/CC 66, Notes C1-F1, Program Change).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { InputType = PedalInputType.MidiCC, KeyCode = 64, KeyName = "MIDI CC 64 (Sustain)", Action = PedalAction.NextPage, LongPressAction = PedalAction.NextScore, Description = "Pédale Sustain (CC 64)" },
                    new() { InputType = PedalInputType.MidiCC, KeyCode = 66, KeyName = "MIDI CC 66 (Sostenuto)", Action = PedalAction.PreviousPage, LongPressAction = PedalAction.PreviousScore, Description = "Pédale Sostenuto (CC 66)" },
                    new() { InputType = PedalInputType.MidiCC, KeyCode = 67, KeyName = "MIDI CC 67 (Soft Pedal)", Action = PedalAction.ToggleMetronome, Description = "Pédale Soft (CC 67)" },
                    new() { InputType = PedalInputType.MidiCC, KeyCode = 80, KeyName = "MIDI CC 80 (General 1)", Action = PedalAction.ToggleAudio, Description = "Bouton Général MIDI CC 80" },
                    new() { InputType = PedalInputType.MidiNote, KeyCode = 36, KeyName = "MIDI Note 36 (C1)", Action = PedalAction.PreviousPage, Description = "Note C1 (Page précédente)" },
                    new() { InputType = PedalInputType.MidiNote, KeyCode = 38, KeyName = "MIDI Note 38 (D1)", Action = PedalAction.NextPage, Description = "Note D1 (Page suivante)" },
                    new() { InputType = PedalInputType.MidiNote, KeyCode = 40, KeyName = "MIDI Note 40 (E1)", Action = PedalAction.ToggleAudio, Description = "Note E1 (Piste Audio)" },
                    new() { InputType = PedalInputType.MidiNote, KeyCode = 41, KeyName = "MIDI Note 41 (F1)", Action = PedalAction.ToggleMetronome, Description = "Note F1 (Métronome)" },
                    new() { InputType = PedalInputType.MidiProgramChange, KeyCode = 1, KeyName = "MIDI PC + (Morceau +)", Action = PedalAction.NextScore, Description = "Program Change Suivant" },
                    new() { InputType = PedalInputType.MidiProgramChange, KeyCode = 0, KeyName = "MIDI PC - (Morceau -)", Action = PedalAction.PreviousScore, Description = "Program Change Précédent" }
                }
            });

            // Charger les profils personnalisés enregistrés par l'utilisateur
            LoadCustomProfiles();
        }

        private void LoadCustomProfiles()
        {
            try
            {
                string json = Preferences.Get(CustomProfilesKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var customList = JsonSerializer.Deserialize<List<PedalProfile>>(json);
                    if (customList != null)
                    {
                        foreach (var cp in customList)
                        {
                            cp.IsBuiltIn = false;
                            Profiles.Add(cp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PedalMidiService] Erreur lors du chargement des profils custom: {ex.Message}");
            }
        }

        public void SaveCustomProfiles()
        {
            try
            {
                var customList = Profiles.Where(p => !p.IsBuiltIn).ToList();
                string json = JsonSerializer.Serialize(customList);
                Preferences.Set(CustomProfilesKey, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PedalMidiService] Erreur lors de la sauvegarde des profils custom: {ex.Message}");
            }
        }

        public PedalProfile CreateCustomProfile(string name, string description = "")
        {
            var profile = new PedalProfile
            {
                Id = "custom_" + Guid.NewGuid().ToString("N")[..8],
                Name = name,
                Description = description,
                IsBuiltIn = false,
                Bindings = new List<PedalBinding>()
            };

            // Copier par défaut les raccourcis du profil standard
            var standard = Profiles.FirstOrDefault(p => p.Id == "preset_standard");
            if (standard != null)
            {
                foreach (var b in standard.Bindings)
                {
                    profile.Bindings.Add(b.Clone());
                }
            }

            Profiles.Add(profile);
            SaveCustomProfiles();
            ActiveProfile = profile;
            return profile;
        }

        public void DeleteCustomProfile(string profileId)
        {
            var p = Profiles.FirstOrDefault(x => x.Id == profileId && !x.IsBuiltIn);
            if (p != null)
            {
                Profiles.Remove(p);
                SaveCustomProfiles();
                if (ActiveProfile.Id == profileId)
                {
                    ActiveProfile = Profiles.First();
                }
            }
        }

        #region Traitement des Événements Clavier / Bluetooth HID

        public bool ProcessKeyDown(int rawKeyCode, string? keyName = null, string source = "Clavier / Bluetooth HID")
        {
            if (!IsEnabled) return false;

            string standardKeyName = keyName ?? NormalizeKeyName(rawKeyCode);

            lock (_lock)
            {
                // Si la touche est déjà enfoncée, c'est une répétition automatique (repeat), on ignore
                if (_activeKeyDowns.ContainsKey(rawKeyCode))
                {
                    return true;
                }

                // Démarrer un timer pour la détection de pression longue (Long Press)
                var timer = new System.Threading.Timer(OnLongPressTimerFired, rawKeyCode, LongPressThresholdMs, System.Threading.Timeout.Infinite);
                _activeKeyDowns[rawKeyCode] = (DateTime.Now, timer);
            }

            return true;
        }

        public bool ProcessKeyUp(int rawKeyCode, string? keyName = null, string source = "Clavier / Bluetooth HID")
        {
            if (!IsEnabled) return false;

            string standardKeyName = keyName ?? NormalizeKeyName(rawKeyCode);
            bool wasLongPress = false;
            DateTime pressTime = DateTime.Now;

            lock (_lock)
            {
                if (_activeKeyDowns.TryGetValue(rawKeyCode, out var pressInfo))
                {
                    pressInfo.Timer?.Dispose();
                    pressTime = pressInfo.PressTime;
                    _activeKeyDowns.Remove(rawKeyCode);
                    
                    double durationMs = (DateTime.Now - pressTime).TotalMilliseconds;
                    wasLongPress = durationMs >= LongPressThresholdMs;
                }
            }

            // Trouver le binding correspondant dans le profil actif
            var binding = FindBinding(PedalInputType.KeyboardKey, rawKeyCode, standardKeyName);
            PedalAction action = PedalAction.None;

            if (binding != null)
            {
                if (wasLongPress && binding.LongPressAction != PedalAction.None)
                {
                    action = binding.LongPressAction;
                }
                else
                {
                    action = binding.Action;
                }
            }

            // Émettre l'événement brut pour le moniteur/testeur
            var rawEvent = new PedalRawEvent
            {
                Timestamp = DateTime.Now,
                InputType = PedalInputType.KeyboardKey,
                KeyCode = rawKeyCode,
                KeyName = standardKeyName,
                Value = wasLongPress ? 1 : 0,
                IsLongPress = wasLongPress,
                MatchedAction = action,
                Source = source
            };

            RawEventReceived?.Invoke(rawEvent);

            if (action != PedalAction.None)
            {
                TriggerAction(action);
                return true;
            }

            return false;
        }

        private void OnLongPressTimerFired(object? state)
        {
            if (state is int rawKeyCode)
            {
                lock (_lock)
                {
                    if (_activeKeyDowns.TryGetValue(rawKeyCode, out var pressInfo))
                    {
                        string standardKeyName = NormalizeKeyName(rawKeyCode);
                        var binding = FindBinding(PedalInputType.KeyboardKey, rawKeyCode, standardKeyName);
                        if (binding != null && binding.LongPressAction != PedalAction.None)
                        {
                            // Émettre notification temps réel
                            var rawEvent = new PedalRawEvent
                            {
                                Timestamp = DateTime.Now,
                                InputType = PedalInputType.KeyboardKey,
                                KeyCode = rawKeyCode,
                                KeyName = standardKeyName,
                                Value = 1,
                                IsLongPress = true,
                                MatchedAction = binding.LongPressAction,
                                Source = "Bluetooth HID (Long Press)"
                            };
                            RawEventReceived?.Invoke(rawEvent);
                        }
                    }
                }
            }
        }

        #endregion

        #region Traitement des Événements MIDI

        public void ProcessMidiMessage(PedalInputType inputType, int code, int value = 127, string source = "MIDI USB/BT")
        {
            if (!IsEnabled) return;

            // En MIDI, pour NoteOn, la vélocité 0 correspond à NoteOff.
            if (inputType == PedalInputType.MidiNote && value == 0)
            {
                return;
            }

            // Pour CC (Control Change), si valeur < 64, c'est généralement un relâchement de pédale (Pedal Release)
            if (inputType == PedalInputType.MidiCC && value < 64)
            {
                return;
            }

            string name = inputType switch
            {
                PedalInputType.MidiNote => $"MIDI Note {code} ({GetMidiNoteName(code)})",
                PedalInputType.MidiCC => $"MIDI CC {code} ({GetMidiCCName(code)})",
                PedalInputType.MidiProgramChange => $"MIDI Program {code}",
                _ => $"MIDI {code}"
            };

            var binding = FindBinding(inputType, code, name);
            PedalAction action = binding?.Action ?? PedalAction.None;

            var rawEvent = new PedalRawEvent
            {
                Timestamp = DateTime.Now,
                InputType = inputType,
                KeyCode = code,
                KeyName = name,
                Value = value,
                IsLongPress = false,
                MatchedAction = action,
                Source = source
            };

            RawEventReceived?.Invoke(rawEvent);

            if (action != PedalAction.None)
            {
                TriggerAction(action);
            }
        }

        #endregion

        private PedalBinding? FindBinding(PedalInputType inputType, int code, string keyName)
        {
            if (ActiveProfile?.Bindings == null) return null;

            // 1. Chercher par correspondance exacte de type et code
            var binding = ActiveProfile.Bindings.FirstOrDefault(b => b.InputType == inputType && b.KeyCode == code);
            if (binding != null) return binding;

            // 2. Chercher par nom normalisé pour le clavier (très utile entre Android et Windows)
            if (inputType == PedalInputType.KeyboardKey && !string.IsNullOrWhiteSpace(keyName))
            {
                binding = ActiveProfile.Bindings.FirstOrDefault(b => b.InputType == PedalInputType.KeyboardKey &&
                    string.Equals(b.KeyName, keyName, StringComparison.OrdinalIgnoreCase));
            }

            return binding;
        }

        public void TriggerAction(PedalAction action)
        {
            if (action == PedalAction.None) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    ActionTriggered?.Invoke(action);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PedalMidiService] Erreur lors de l'exécution de l'action {action}: {ex.Message}");
                }
            });
        }

        #region Helpers de conversion et normalisation

        public static string NormalizeKeyName(int rawKeyCode)
        {
            // Android Keycodes
            switch (rawKeyCode)
            {
                case 93: case 34: return "PageDown";
                case 92: case 33: return "PageUp";
                case 22: case 39: return "ArrowRight";
                case 21: case 37: return "ArrowLeft";
                case 20: case 40: return "ArrowDown";
                case 19: case 38: return "ArrowUp";
                case 62: case 32: return "Space";
                case 66: case 13: return "Return";
                case 67: return "Backspace";
                case 8: return "Digit1 / Backspace";
                case 111: case 27: return "Escape";
                case 122: case 36: return "Home";
                case 123: case 35: return "End";
                case 49:  return "Digit1";
                case 9: case 50:  return "Digit2";
                case 10: case 51: return "Digit3";
                case 11: case 52: return "Digit4";
                case 133: case 114: return "F3";
                case 134: case 115: return "F4";
                case 135: case 116: return "F5";
                case 136: case 117: return "F6";
                case 137: case 118: return "F7";
                case 138: case 119: return "F8";
                default:
                    if (rawKeyCode >= 29 && rawKeyCode <= 54) // Android A-Z
                        return ((char)('A' + (rawKeyCode - 29))).ToString();
                    if (rawKeyCode >= 65 && rawKeyCode <= 90) // Windows A-Z
                        return ((char)rawKeyCode).ToString();
                    return $"Key_{rawKeyCode}";
            }
        }

        public static string GetActionDisplayName(PedalAction action)
        {
            return action switch
            {
                PedalAction.None => "Aucune action",
                PedalAction.NextPage => "➡️ Tourner à la page suivante",
                PedalAction.PreviousPage => "⬅️ Tourner à la page précédente",
                PedalAction.ScrollDown => "⬇️ Défiler vers le bas",
                PedalAction.ScrollUp => "⬆️ Défiler vers le haut",
                PedalAction.FirstPage => "⏮️ Aller au début (1ère page)",
                PedalAction.LastPage => "⏭️ Aller à la fin (Dernière page)",
                PedalAction.NextScore => "📑 Morceau suivant (Setlist)",
                PedalAction.PreviousScore => "📑 Morceau précédent (Setlist)",
                PedalAction.ToggleMetronome => "⏱️ Démarrer / Arrêter le métronome",
                PedalAction.ToggleMetronomeSound => "🔇 Activer / Couper le son du métronome",
                PedalAction.ToggleAudio => "🎵 Lecture / Pause de la piste audio",
                PedalAction.RestartAudio => "🔁 Recommencer la piste audio",
                PedalAction.ResetZoom => "🔍 Rétablir le zoom à 100%",
                PedalAction.OpenPageJump => "🔢 Ouvrir le saut direct de page",
                PedalAction.ToggleAnnotationsLock => "🔒 Verrouiller / Déverrouiller annotations",
                PedalAction.UndoAnnotation => "↩️ Annuler la dernière annotation",
                PedalAction.RedoAnnotation => "↪️ Rétablir l'annotation",
                PedalAction.OpenQuickMenu => "📋 Ouvrir le menu central",
                PedalAction.CloseViewer => "🚪 Fermer le lecteur / Retour",
                _ => action.ToString()
            };
        }

        public static string GetMidiNoteName(int noteNumber)
        {
            string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int octave = (noteNumber / 12) - 1;
            int noteIndex = noteNumber % 12;
            return $"{notes[noteIndex]}{octave}";
        }

        public static string GetMidiCCName(int ccNumber)
        {
            return ccNumber switch
            {
                64 => "Sustain / Damper",
                66 => "Sostenuto",
                67 => "Soft Pedal",
                1 => "Modulation Wheel",
                7 => "Channel Volume",
                11 => "Expression",
                65 => "Portamento On/Off",
                68 => "Legato",
                _ => $"CC {ccNumber}"
            };
        }

        #endregion
    }
}
