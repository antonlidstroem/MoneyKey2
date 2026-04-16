using MoneyKey.Core.DTOs.Journal;
using MoneyKey.Domain.Enums;

namespace MoneyKey.Blazor.State;

public class JournalFilterState
{
    public JournalQuery Query            { get; private set; } = DefaultQuery();
    public bool         IsFilterPanelOpen { get; set; }        = false;

    public event Action? FilterChanged;

    private static JournalQuery DefaultQuery() => new()
    {
        PageSize = 50,
        SortBy   = "Date",
        SortDir  = "desc"
    };

    public bool IsTypeIncluded(JournalEntryType t) =>
        !Query.IncludeTypes.Any() || Query.IncludeTypes.Contains(t);

    public void ToggleType(JournalEntryType t)
    {
        if (!Query.IncludeTypes.Any())
            Query.IncludeTypes = Enum.GetValues<JournalEntryType>().Where(x => x != t).ToList();
        else if (Query.IncludeTypes.Contains(t))
        {
            Query.IncludeTypes.Remove(t);
            if (!Query.IncludeTypes.Any()) Query.IncludeTypes = new();
        }
        else
        {
            Query.IncludeTypes.Add(t);
            if (Query.IncludeTypes.Count == Enum.GetValues<JournalEntryType>().Length)
                Query.IncludeTypes = new();
        }
        Query.Page = 1;
        FilterChanged?.Invoke();
    }

    public void SetOnlyType(JournalEntryType t)
    {
        Query.IncludeTypes = new List<JournalEntryType> { t };
        Query.Page = 1;
        FilterChanged?.Invoke();
    }

    public void ShowAllTypes()
    {
        Query.IncludeTypes = new();
        Query.Page = 1;
        FilterChanged?.Invoke();
    }

    public void Update(Action<JournalQuery> modify)
    {
        modify(Query);
        Query.Page = 1;
        FilterChanged?.Invoke();
    }

    public void SetPage(int page) => Query.Page = page;

    public void SetSort(string column)
    {
        if (Query.SortBy == column)
            Query.SortDir = Query.SortDir == "asc" ? "desc" : "asc";
        else { Query.SortBy = column; Query.SortDir = "asc"; }
        Query.Page = 1;
        FilterChanged?.Invoke();
    }

    public void ResetFilters()
    {
        var size = Query.PageSize;
        Query = DefaultQuery();
        Query.PageSize = size;
        FilterChanged?.Invoke();
    }

    public bool HasActiveFilters =>
        Query.IncludeTypes.Any() || Query.FilterByStartDate || Query.FilterByEndDate ||
        Query.FilterByDescription || Query.FilterByCategory  || Query.FilterByProject ||
        Query.FilterByAmount      || Query.ReceiptStatuses.Any();
}
