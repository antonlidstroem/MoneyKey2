namespace MoneyKey.Domain.Enums;

public enum ListType
{
    CheckList = 0,
    Shopping = 1,
    Custom = 2,  // Progress list
    ToDo = 3,
    Note = 4,
    // ── Phase A — Fas A ─────────────────────
    Packing = 5,  // Packlista med mallar och kategorier
    Habit = 6,  // Rutiner med streaks (Fas B)
    Decision = 7,  // Beslutslista med magkänsla (Fas B)
    Project = 8,  // Projektlista med status-faser (Fas B)
    Problem = 9,  // Problem/Lösning (Fas B)
    Inventory = 10  // Inventarielista (Fas D)
}