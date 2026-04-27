namespace MoneyKey.Core.DTOs.Lists;

/// <summary>
/// Stored in ListItem.ItemData for each problem entry.
/// Each ListItem = one problem.
/// Solutions are embedded in the item's payload (not separate DB rows).
/// </summary>
public record ProblemItemData(
    string ProblemDescription = "",
    string Status = "open",    // "open" | "solved" | "parked"
    List<SolutionEntry>? Solutions = null,
    string? ChosenSolution = null,      // Name of solution that worked
    string? Context = null       // Optional extra context
);

public record SolutionEntry(
    string Name = "",
    string TestStatus = "untested", // "untested" | "testing" | "works" | "failed"
    string? TestedDate = null,       // ISO date
    string? ResultNote = null,
    bool IsChosen = false
);