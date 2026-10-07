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
        private const string AllProfilesKey = "MSM_AllPedalProfiles_Json_v250";
        private const string LongPressThresholdKey = "MSM_PedalLongPressThresholdMs";
        private const string IsPedalEnabledKey = "MSM_PedalServiceEnabled";
        private const string BlockFastTurnKey = "MSM_PedalBlockFastTurn";
        private const string FastTurnCooldownMsKey = "MSM_PedalFastTurnCooldownMs";

        private readonly Dictionary<int, (DateTime PressTime, System.Threading.Timer? Timer)> _activeKeyDowns = new();
        private readonly object _lock = new();
        private DateTime _lastPageTurnTime = DateTime.MinValue;

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

        public bool BlockFastPageTurn
        {
            get => Preferences.Get(BlockFastTurnKey, true);
            set => Preferences.Set(BlockFastTurnKey, value);
        }

        public int FastTurnCooldownMs
        {
            get => Preferences.Get(FastTurnCooldownMsKey, 450);
            set => Preferences.Set(FastTurnCooldownMsKey, value);
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
            _activeProfile = Profiles.FirstOrDefault(p => p.Id == savedActiveId) ?? Profiles.FirstOrDefault() ?? CreateCustomProfile("Standard");
        }

        public void InitializeProfiles()
        {
            Profiles = new List<PedalProfile>();

            // 1. Tenter de charger les profils sauvegardés par l'utilisateur (incluant les modifications sur les profils d'usine)
            string json = Preferences.Get(AllProfilesKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var savedList = JsonSerializer.Deserialize<List<PedalProfile>>(json);
                    if (savedList != null && savedList.Count > 0)
                    {
                        Profiles = savedList;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PedalMidiService] Erreur désérialisation profils v250: {ex.Message}");
                }
            }

            // 2. Si aucune sauvegarde n'existe, initialiser les profils d'usine
            BuildDefaultFactoryProfiles();
            SaveProfiles();
        }

        public void BuildDefaultFactoryProfiles()
        {
            Profiles = new List<PedalProfile>();

            // 1. Profil Standard (Défaut)
            Profiles.Add(new PedalProfile
            {
                Id = "preset_standard",
                Name = "Standard (Flèches / Page Up-Down / Espace)",
                Description = "Compatible avec toutes les pédales standards et claviers Bluetooth (PageUp, PageDown, Flèches, Espace, Entrée). Idéal pour les configurations universelles.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale Droite (Page suivante)", KeyName = "PageDown", KeyCode = 93, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Pédale Gauche (Page précédente)", KeyName = "PageUp", KeyCode = 92, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" },
                    new() { ButtonLabel = "Flèche Droite", KeyName = "ArrowRight", KeyCode = 22, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Flèche Gauche", KeyName = "ArrowLeft", KeyCode = 21, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" },
                    new() { ButtonLabel = "Flèche Bas", KeyName = "ArrowDown", KeyCode = 20, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Flèche Haut", KeyName = "ArrowUp", KeyCode = 19, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" },
                    new() { ButtonLabel = "Barre Espace", KeyName = "Space", KeyCode = 62, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Touche Entrée", KeyName = "Return", KeyCode = 66, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Pédale Sustain MIDI (CC 64)", InputType = PedalInputType.MidiCC, KeyCode = 64, KeyName = "MIDI CC 64 (Sustain)", PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Pédale forte / Sustain" },
                    new() { ButtonLabel = "Pédale Sostenuto MIDI (CC 66)", InputType = PedalInputType.MidiCC, KeyCode = 66, KeyName = "MIDI CC 66 (Sostenuto)", PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Pédale tonale / Sostenuto" },
                    new() { ButtonLabel = "Pédale Soft MIDI (CC 67)", InputType = PedalInputType.MidiCC, KeyCode = 67, KeyName = "MIDI CC 67 (Soft)", PressType = PedalPressType.Simple, Action = PedalAction.NextBookmark, Description = "Marqueur suivant" }
                }
            });

            // 2. PageFlip Dragonfly (4 Pédales)
            Profiles.Add(new PedalProfile
            {
                Id = "preset_pageflip_dragonfly",
                Name = "PageFlip Dragonfly (4 Pédales)",
                Description = "Configuration pour le pédalier PageFlip Dragonfly à 4 commutateurs. Réglage physique recommandé : commutateur REPEAT sur OFF et sélecteur sur Mode 3 (PageDown / PageUp) pour les deux grandes pédales principales. Les switches auxiliaires externes sont configurés pour les sauts de marqueurs ou le début/fin de morceau.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale Droite Principale", KeyName = "PageDown", KeyCode = 93, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Tourner la page suivante" },
                    new() { ButtonLabel = "Pédale Gauche Principale", KeyName = "PageUp", KeyCode = 92, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Tourner la page précédente" },
                    new() { ButtonLabel = "Pédale Auxiliaire Droite", KeyName = "F4", KeyCode = 134, PressType = PedalPressType.Simple, Action = PedalAction.NextBookmark, Description = "Aller au marqueur suivant" },
                    new() { ButtonLabel = "Pédale Auxiliaire Gauche", KeyName = "F3", KeyCode = 133, PressType = PedalPressType.Simple, Action = PedalAction.PreviousBookmark, Description = "Aller au marqueur précédent" }
                }
            });

            // 3. PageFlip Firefly / Butterfly
            Profiles.Add(new PedalProfile
            {
                Id = "preset_pageflip_firefly",
                Name = "PageFlip Firefly & Butterfly",
                Description = "Pédaliers PageFlip Firefly et Butterfly à 2 pédales. Réglage physique recommandé : sélecteur sur Mode 3 (PageDown / PageUp) et commutateur REPEAT sur OFF pour éviter les doubles tournes involontaires.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale Droite", KeyName = "PageDown", KeyCode = 93, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Pédale Gauche", KeyName = "PageUp", KeyCode = 92, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" }
                }
            });

            // 4. AirTurn Duo 500 / PEDpro / BT200
            Profiles.Add(new PedalProfile
            {
                Id = "preset_airturn_duo",
                Name = "AirTurn Duo 500 / PEDpro",
                Description = "Pédaliers Bluetooth AirTurn à 2 commutateurs silencieux. Réglage physique recommandé : Mode 3 (PageDown / PageUp) ou Mode 2 (Flèches gauche / droite). Fonctionne avec tous les appareils via Bluetooth BLE.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale Droite (Switch 2)", KeyName = "PageDown", KeyCode = 93, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Pédale Gauche (Switch 1)", KeyName = "PageUp", KeyCode = 92, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" }
                }
            });

            // 5. AirTurn Quad 500 (4 Pédales)
            Profiles.Add(new PedalProfile
            {
                Id = "preset_airturn_quad",
                Name = "AirTurn Quad 500 (4 Pédales)",
                Description = "Pédalier AirTurn à 4 commutateurs. Réglage recommandé : Mode 3. Pédales 1 & 3 pour tourner les pages, pédales 2 & 4 pour naviguer entre les marqueurs de la partition ou les morceaux.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale 3 (Droite Principale)", KeyName = "PageDown", KeyCode = 93, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Pédale 1 (Gauche Principale)", KeyName = "PageUp", KeyCode = 92, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" },
                    new() { ButtonLabel = "Pédale 4 (Auxiliaire Droite)", KeyName = "ArrowRight", KeyCode = 22, PressType = PedalPressType.Simple, Action = PedalAction.NextBookmark, Description = "Marqueur suivant" },
                    new() { ButtonLabel = "Pédale 2 (Auxiliaire Gauche)", KeyName = "ArrowLeft", KeyCode = 21, PressType = PedalPressType.Simple, Action = PedalAction.PreviousBookmark, Description = "Marqueur précédent" }
                }
            });

            // 6. Donner Wireless Page Turner
            Profiles.Add(new PedalProfile
            {
                Id = "preset_donner",
                Name = "Donner Wireless Page Turner",
                Description = "Pédale sans fil Donner à double pédalier. Réglage physique recommandé : commutateur arrière sur Mode 1 (PageDown / PageUp) ou Mode 2 (Flèches horizontales).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale Droite", KeyName = "PageDown", KeyCode = 93, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Pédale Gauche", KeyName = "PageUp", KeyCode = 92, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" }
                }
            });

            // 7. Joyo JSP-01 Wireless Page Turner
            Profiles.Add(new PedalProfile
            {
                Id = "preset_joyo",
                Name = "Joyo JSP-01 Wireless Page Turner",
                Description = "Pédale sans fil Joyo JSP-01. Réglage recommandé : commutateur sur Mode 1 (Flèches) ou Mode 2 (PageUp/PageDown). Veiller à désactiver la répétition continue.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale Droite", KeyName = "ArrowRight", KeyCode = 22, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Pédale Gauche", KeyName = "ArrowLeft", KeyCode = 21, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" }
                }
            });

            // 8. Thomann / Harley Benton PageTurn
            Profiles.Add(new PedalProfile
            {
                Id = "preset_harley_benton",
                Name = "Thomann / Harley Benton PageTurn",
                Description = "Pédale 2 commutateurs Harley Benton PageTurn. Réglage physique : sélecteur sur Mode 1 (Flèches) ou Mode 2 (PageUp/PageDown).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale Droite", KeyName = "ArrowRight", KeyCode = 22, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Pédale Gauche", KeyName = "ArrowLeft", KeyCode = 21, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" }
                }
            });

            // 9. IK Multimedia iRig BlueTurn
            Profiles.Add(new PedalProfile
            {
                Id = "preset_irig_blueturn",
                Name = "IK Multimedia iRig BlueTurn",
                Description = "Pédalier compact et rétroéclairé iRig BlueTurn à 2 touches silencieuses. Réglage recommandé : Mode 1 (PageUp / PageDown) ou Mode 2 (Flèches).",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Bouton Droit (PageDown)", KeyName = "PageDown", KeyCode = 93, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Bouton Gauche (PageUp)", KeyName = "PageUp", KeyCode = 92, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" }
                }
            });

            // 10. Coda Music Technologies STOMP
            Profiles.Add(new PedalProfile
            {
                Id = "preset_coda_stomp",
                Name = "Coda Music Technologies STOMP",
                Description = "Pédalier ultra-robuste STOMP en boîtier aluminium massif. Réglage matériel recommandé : sélecteur de mode sur Mode 1 (PageUp / PageDown) pour une réactivité instantanée sur scène.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Footswitch Droit", KeyName = "PageDown", KeyCode = 93, PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Page suivante" },
                    new() { ButtonLabel = "Footswitch Gauche", KeyName = "PageUp", KeyCode = 92, PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Page précédente" }
                }
            });

            // 11. Contrôleur MIDI Avancé (USB / Bluetooth)
            Profiles.Add(new PedalProfile
            {
                Id = "preset_midi_controller",
                Name = "Contrôleur MIDI Avancé (USB / Bluetooth)",
                Description = "Pédaliers et claviers maîtres MIDI. Assignations par défaut : Pédale Sustain (CC 64) = Page suivante, Pédale Sostenuto (CC 66) = Page précédente, Pédale Soft (CC 67) = Marqueur suivant.",
                IsBuiltIn = true,
                Bindings = new List<PedalBinding>
                {
                    new() { ButtonLabel = "Pédale Sustain (CC 64)", InputType = PedalInputType.MidiCC, KeyCode = 64, KeyName = "MIDI CC 64 (Sustain)", PressType = PedalPressType.Simple, Action = PedalAction.NextPage, Description = "Pédale Sustain (CC 64)" },
                    new() { ButtonLabel = "Pédale Sostenuto (CC 66)", InputType = PedalInputType.MidiCC, KeyCode = 66, KeyName = "MIDI CC 66 (Sostenuto)", PressType = PedalPressType.Simple, Action = PedalAction.PreviousPage, Description = "Pédale Sostenuto (CC 66)" },
                    new() { ButtonLabel = "Pédale Soft (CC 67)", InputType = PedalInputType.MidiCC, KeyCode = 67, KeyName = "MIDI CC 67 (Soft)", PressType = PedalPressType.Simple, Action = PedalAction.NextBookmark, Description = "Pédale Soft (CC 67)" }
                }
            });
        }

        public void SaveProfiles()
        {
            try
            {
                string json = JsonSerializer.Serialize(Profiles);
                Preferences.Set(AllProfilesKey, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PedalMidiService] Erreur lors de la sauvegarde des profils: {ex.Message}");
            }
        }

        public void SaveProfile(PedalProfile profile)
        {
            if (profile == null) return;
            var index = Profiles.FindIndex(p => p.Id == profile.Id);
            if (index >= 0)
            {
                Profiles[index] = profile;
            }
            else
            {
                Profiles.Add(profile);
            }
            SaveProfiles();
            if (ActiveProfile?.Id == profile.Id)
            {
                _activeProfile = profile;
                ActiveProfileChanged?.Invoke();
            }
        }

        public PedalProfile CreateCustomProfile(string name, string description = "")
        {
            var profile = new PedalProfile
            {
                Id = "profile_" + Guid.NewGuid().ToString("N")[..8],
                Name = string.IsNullOrWhiteSpace(name) ? "Nouveau Profil" : name.Trim(),
                Description = description.Trim(),
                IsBuiltIn = false,
                Bindings = new List<PedalBinding>()
            };

            // Copier les boutons du profil standard par défaut pour offrir une base de travail prête à l'emploi
            var standard = Profiles.FirstOrDefault(p => p.Id == "preset_standard");
            if (standard != null)
            {
                foreach (var b in standard.Bindings)
                {
                    profile.Bindings.Add(b.Clone());
                }
            }

            Profiles.Add(profile);
            SaveProfiles();
            ActiveProfile = profile;
            return profile;
        }

        public void DeleteProfile(string profileId)
        {
            var p = Profiles.FirstOrDefault(x => x.Id == profileId);
            if (p != null)
            {
                Profiles.Remove(p);
                if (Profiles.Count == 0)
                {
                    BuildDefaultFactoryProfiles();
                }
                SaveProfiles();
                if (ActiveProfile?.Id == profileId)
                {
                    ActiveProfile = Profiles.First();
                }
            }
        }

        public void ResetToFactoryDefaults()
        {
            BuildDefaultFactoryProfiles();
            SaveProfiles();
            ActiveProfile = Profiles.First();
        }

        #region Traitement des Événements Clavier / Bluetooth HID

        public bool ProcessKeyDown(int rawKeyCode, string? keyName = null, string source = "Clavier / Bluetooth HID")
        {
            if (!IsEnabled) return false;

            string standardKeyName = keyName ?? NormalizeKeyName(rawKeyCode);

            lock (_lock)
            {
                if (_activeKeyDowns.ContainsKey(rawKeyCode))
                {
                    return true;
                }

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

            // Trouver le binding correspondant dans le profil actif en respectant le type d'appui
            PedalBinding? binding = null;
            PedalAction action = PedalAction.None;

            if (ActiveProfile?.Bindings != null)
            {
                if (wasLongPress)
                {
                    // 1. Chercher un binding configuré pour appui long
                    binding = ActiveProfile.Bindings.FirstOrDefault(b =>
                        b.InputType == PedalInputType.KeyboardKey &&
                        (b.KeyCode == rawKeyCode || string.Equals(b.KeyName, standardKeyName, StringComparison.OrdinalIgnoreCase)) &&
                        b.PressType == PedalPressType.Long &&
                        b.Action != PedalAction.None);

                    if (binding != null)
                    {
                        action = binding.Action;
                    }
                    else
                    {
                        // 2. Fallback rétro-compatible sur LongPressAction si présent
                        var fallback = ActiveProfile.Bindings.FirstOrDefault(b =>
                            b.InputType == PedalInputType.KeyboardKey &&
                            (b.KeyCode == rawKeyCode || string.Equals(b.KeyName, standardKeyName, StringComparison.OrdinalIgnoreCase)) &&
                            b.LongPressAction != PedalAction.None);
                        if (fallback != null)
                        {
                            binding = fallback;
                            action = fallback.LongPressAction;
                        }
                    }
                }

                if (action == PedalAction.None)
                {
                    // Appui simple : chercher un binding configuré en appui simple
                    binding = ActiveProfile.Bindings.FirstOrDefault(b =>
                        b.InputType == PedalInputType.KeyboardKey &&
                        (b.KeyCode == rawKeyCode || string.Equals(b.KeyName, standardKeyName, StringComparison.OrdinalIgnoreCase)) &&
                        b.PressType == PedalPressType.Simple &&
                        b.Action != PedalAction.None);

                    if (binding != null)
                    {
                        action = binding.Action;
                    }
                    else
                    {
                        // Fallback générique
                        binding = ActiveProfile.Bindings.FirstOrDefault(b =>
                            b.InputType == PedalInputType.KeyboardKey &&
                            (b.KeyCode == rawKeyCode || string.Equals(b.KeyName, standardKeyName, StringComparison.OrdinalIgnoreCase)) &&
                            b.Action != PedalAction.None);
                        if (binding != null)
                        {
                            action = binding.Action;
                        }
                    }
                }
            }

            // Émettre l'événement brut pour les pages d'apprentissage/écoute
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
                        var binding = ActiveProfile?.Bindings?.FirstOrDefault(b =>
                            b.InputType == PedalInputType.KeyboardKey &&
                            (b.KeyCode == rawKeyCode || string.Equals(b.KeyName, standardKeyName, StringComparison.OrdinalIgnoreCase)) &&
                            (b.PressType == PedalPressType.Long || b.LongPressAction != PedalAction.None));

                        if (binding != null)
                        {
                            var rawEvent = new PedalRawEvent
                            {
                                Timestamp = DateTime.Now,
                                InputType = PedalInputType.KeyboardKey,
                                KeyCode = rawKeyCode,
                                KeyName = standardKeyName,
                                Value = 1,
                                IsLongPress = true,
                                MatchedAction = binding.PressType == PedalPressType.Long ? binding.Action : binding.LongPressAction,
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

            if (inputType == PedalInputType.MidiNote && value == 0)
            {
                return;
            }

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

            PedalBinding? binding = ActiveProfile?.Bindings?.FirstOrDefault(b =>
                b.InputType == inputType &&
                b.KeyCode == code &&
                b.Action != PedalAction.None);

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

        public void TriggerAction(PedalAction action)
        {
            if (action == PedalAction.None) return;

            // Protection anti-double saut de page (anti-rebond matériel / limitation de cadence scène)
            if (BlockFastPageTurn && (action == PedalAction.NextPage || action == PedalAction.PreviousPage))
            {
                lock (_lock)
                {
                    double elapsed = (DateTime.Now - _lastPageTurnTime).TotalMilliseconds;
                    if (elapsed < FastTurnCooldownMs)
                    {
                        Debug.WriteLine($"[PedalMidiService] Saut de page ignoré par sécurité anti-rebond ({elapsed:F0} ms < {FastTurnCooldownMs} ms)");
                        return;
                    }
                    _lastPageTurnTime = DateTime.Now;
                }
            }

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

        public static string GetActionDisplayName(PedalAction action) => PedalActionHelper.GetActionDisplayName(action);

        public static string NormalizeKeyName(int rawKeyCode)
        {
            switch (rawKeyCode)
            {
                case 92: return "PageUp";
                case 93: return "PageDown";
                case 19: return "ArrowUp";
                case 20: return "ArrowDown";
                case 21: return "ArrowLeft";
                case 22: return "ArrowRight";
                case 62: return "Space";
                case 66: return "Return";
                case 67: return "Backspace";
                case 122: return "Home";
                case 123: return "End";
                case 133: case 114: return "F3";
                case 134: case 115: return "F4";
                case 135: case 116: return "F5";
                case 136: case 117: return "F6";
                case 137: case 118: return "F7";
                case 138: case 119: return "F8";
                case 33: return "PageUp";
                case 34: return "PageDown";
                case 37: return "ArrowLeft";
                case 38: return "ArrowUp";
                case 39: return "ArrowRight";
                case 40: return "ArrowDown";
                case 32: return "Space";
                case 13: return "Return";
                default:
                    if (rawKeyCode >= 29 && rawKeyCode <= 54) // Android A-Z
                        return ((char)('A' + (rawKeyCode - 29))).ToString();
                    if (rawKeyCode >= 65 && rawKeyCode <= 90) // Windows A-Z
                        return ((char)rawKeyCode).ToString();
                    return $"Key_{rawKeyCode}";
            }
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
                80 => "General 1",
                81 => "General 2",
                _ => $"CC {ccNumber}"
            };
        }

        #endregion
    }
}
