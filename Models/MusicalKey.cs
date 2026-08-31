namespace MusicScoreManager.Models
{
    public enum MusicalKey
    {
        None = 0,
        C_Major,        // Do Majeur (C)
        C_Minor,        // Do Mineur (Cm)
        C_Sharp_Major,  // Do# Majeur (C#)
        C_Sharp_Minor,  // Do# Mineur (C#m)
        D_Flat_Major,   // Réb Majeur (Db)
        D_Major,        // Ré Majeur (D)
        D_Minor,        // Ré Mineur (Dm)
        D_Sharp_Minor,  // Ré# Mineur (D#m)
        E_Flat_Major,   // Mib Majeur (Eb)
        E_Flat_Minor,   // Mib Mineur (Ebm)
        E_Major,        // Mi Majeur (E)
        E_Minor,        // Mi Mineur (Em)
        F_Major,        // Fa Majeur (F)
        F_Minor,        // Fa Mineur (Fm)
        F_Sharp_Major,  // Fa# Majeur (F#)
        F_Sharp_Minor,  // Fa# Mineur (F#m)
        G_Flat_Major,   // Solb Majeur (Gb)
        G_Major,        // Sol Majeur (G)
        G_Minor,        // Sol Mineur (Gm)
        G_Sharp_Minor,  // Sol# Mineur (G#m)
        A_Flat_Major,   // Lab Majeur (Ab)
        A_Flat_Minor,   // Lab Mineur (Abm)
        A_Major,        // La Majeur (A)
        A_Minor,        // La Mineur (Am)
        A_Sharp_Minor,  // La# Mineur (A#m)
        B_Flat_Major,   // Sib Majeur (Bb)
        B_Flat_Minor,   // Sib Mineur (Bbm)
        B_Major,        // Si Majeur (B)
        B_Minor         // Si Mineur (Bm)
    }

    public static class MusicalKeyExtensions
    {
        public static string ToDisplayName(this MusicalKey key)
        {
            return key switch
            {
                MusicalKey.None => "Non définie",
                MusicalKey.C_Major => "Do Majeur (C)",
                MusicalKey.C_Minor => "Do Mineur (Cm)",
                MusicalKey.C_Sharp_Major => "Do# Majeur (C#)",
                MusicalKey.C_Sharp_Minor => "Do# Mineur (C#m)",
                MusicalKey.D_Flat_Major => "Réb Majeur (Db)",
                MusicalKey.D_Major => "Ré Majeur (D)",
                MusicalKey.D_Minor => "Ré Mineur (Dm)",
                MusicalKey.D_Sharp_Minor => "Ré# Mineur (D#m)",
                MusicalKey.E_Flat_Major => "Mib Majeur (Eb)",
                MusicalKey.E_Flat_Minor => "Mib Mineur (Ebm)",
                MusicalKey.E_Major => "Mi Majeur (E)",
                MusicalKey.E_Minor => "Mi Mineur (Em)",
                MusicalKey.F_Major => "Fa Majeur (F)",
                MusicalKey.F_Minor => "Fa Mineur (Fm)",
                MusicalKey.F_Sharp_Major => "Fa# Majeur (F#)",
                MusicalKey.F_Sharp_Minor => "Fa# Mineur (F#m)",
                MusicalKey.G_Flat_Major => "Solb Majeur (Gb)",
                MusicalKey.G_Major => "Sol Majeur (G)",
                MusicalKey.G_Minor => "Sol Mineur (Gm)",
                MusicalKey.G_Sharp_Minor => "Sol# Majeur (G#m)",
                MusicalKey.A_Flat_Major => "Lab Majeur (Ab)",
                MusicalKey.A_Flat_Minor => "Lab Mineur (Abm)",
                MusicalKey.A_Major => "La Majeur (A)",
                MusicalKey.A_Minor => "La Mineur (Am)",
                MusicalKey.A_Sharp_Minor => "La# Mineur (A#m)",
                MusicalKey.B_Flat_Major => "Sib Majeur (Bb)",
                MusicalKey.B_Flat_Minor => "Sib Mineur (Bbm)",
                MusicalKey.B_Major => "Si Majeur (B)",
                MusicalKey.B_Minor => "Si Mineur (Bm)",
                _ => "Non définie"
            };
        }
    }
}
