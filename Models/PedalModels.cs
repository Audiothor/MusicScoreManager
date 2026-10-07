using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MusicScoreManager.Models
{
    public enum PedalAction
    {
        None = 0,
        NextPage = 1,
        PreviousPage = 2,
        ScrollDown = 3,
        ScrollUp = 4,
        FirstPage = 5,
        LastPage = 6,
        NextScore = 7,
        PreviousScore = 8,
        ToggleMetronome = 9,
        ToggleMetronomeSound = 10,
        ToggleAudio = 11,
        RestartAudio = 12,
        ResetZoom = 13,
        OpenPageJump = 14,
        ToggleAnnotationsLock = 15,
        UndoAnnotation = 16,
        RedoAnnotation = 17,
        OpenQuickMenu = 18,
        CloseViewer = 19,
        NextBookmark = 20,
        PreviousBookmark = 21,
        OpenSetlistDrawer = 22,
        ToggleNightMode = 23,
        ZoomIn = 24,
        ZoomOut = 25,
        ToggleAnnotationsVisibility = 26
    }

    public enum PedalPressType
    {
        Simple = 0, // Appui simple
        Long = 1    // Appui long
    }

    public enum PedalInputType
    {
        KeyboardKey = 0,
        MidiNote = 1,
        MidiCC = 2,
        MidiProgramChange = 3
    }

    public class PedalBinding
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Libellé personnalisé de la pédale ou du bouton (ex: "Pédale de gauche", "Pédale de droite", "Footswitch 1")
        /// </summary>
        public string ButtonLabel { get; set; } = string.Empty;

        public PedalInputType InputType { get; set; } = PedalInputType.KeyboardKey;
        
        /// <summary>
        /// Code brut (Android KeyCode, Windows VirtualKey, Numéro Note MIDI 0-127, Numéro CC 0-127, ou Numéro PC)
        /// </summary>
        public int KeyCode { get; set; }
        
        /// <summary>
        /// Nom lisible de la touche ou commande MIDI (ex: "PageDown", "ArrowRight", "Midi CC 64", "Midi Note 36")
        /// </summary>
        public string KeyName { get; set; } = string.Empty;

        /// <summary>
        /// Type d'appui : Simple ou Long
        /// </summary>
        public PedalPressType PressType { get; set; } = PedalPressType.Simple;
        
        public PedalAction Action { get; set; } = PedalAction.None;
        public PedalAction LongPressAction { get; set; } = PedalAction.None;
        public string Description { get; set; } = string.Empty;

        public PedalBinding Clone()
        {
            return new PedalBinding
            {
                Id = Guid.NewGuid().ToString(),
                ButtonLabel = this.ButtonLabel,
                InputType = this.InputType,
                KeyCode = this.KeyCode,
                KeyName = this.KeyName,
                PressType = this.PressType,
                Action = this.Action,
                LongPressAction = this.LongPressAction,
                Description = this.Description
            };
        }
    }

    public class PedalProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; } = false;
        public List<PedalBinding> Bindings { get; set; } = new();

        public PedalProfile Clone(string newName)
        {
            var profile = new PedalProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = newName,
                Description = this.Description,
                IsBuiltIn = false,
                Bindings = new List<PedalBinding>()
            };

            foreach (var binding in this.Bindings)
            {
                profile.Bindings.Add(binding.Clone());
            }

            return profile;
        }
    }

    public class PedalRawEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public PedalInputType InputType { get; set; } = PedalInputType.KeyboardKey;
        public int KeyCode { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsLongPress { get; set; }
        public PedalAction MatchedAction { get; set; } = PedalAction.None;
        public string Source { get; set; } = "Clavier / Bluetooth HID";

        public string HexCode => $"0x{KeyCode:X2} ({KeyCode})";
        public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");
    }

    public static class PedalActionHelper
    {
        public static string GetActionDisplayName(PedalAction action)
        {
            return action switch
            {
                PedalAction.PreviousPage => "Page précédente",
                PedalAction.NextPage => "Page suivante",
                PedalAction.FirstPage => "Aller au début du morceau",
                PedalAction.LastPage => "Aller à la fin du morceau",
                PedalAction.PreviousBookmark => "Aller au marqueur précédent",
                PedalAction.NextBookmark => "Aller au marqueur suivant",
                PedalAction.PreviousScore => "Aller au début du morceau précédent du setlist",
                PedalAction.NextScore => "Aller au début du morceau suivant du setlist",
                PedalAction.OpenSetlistDrawer => "Ouvrir le menu de liste de morceaux de la setlist",
                PedalAction.ToggleNightMode => "Activer le mode nuit (Implémentation future)",
                PedalAction.ZoomIn => "Zoom +",
                PedalAction.ZoomOut => "Zoom -",
                PedalAction.ToggleAnnotationsVisibility => "Afficher / retirer les annotations",
                _ => "Aucune action"
            };
        }

        public static readonly List<PedalAction> AvailableConfigurableActions = new()
        {
            PedalAction.PreviousPage,
            PedalAction.NextPage,
            PedalAction.FirstPage,
            PedalAction.LastPage,
            PedalAction.PreviousBookmark,
            PedalAction.NextBookmark,
            PedalAction.PreviousScore,
            PedalAction.NextScore,
            PedalAction.OpenSetlistDrawer,
            PedalAction.ToggleNightMode,
            PedalAction.ZoomIn,
            PedalAction.ZoomOut,
            PedalAction.ToggleAnnotationsVisibility
        };
    }
}

