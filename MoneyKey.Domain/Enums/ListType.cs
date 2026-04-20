namespace MoneyKey.Domain.Enums;

public enum ListType
{
    /// <summary>Checklist with checkbox items.</summary>
    CheckList = 1,
    /// <summary>Shopping list (checklist variant with shopping semantics).</summary>
    Shopping  = 2,
    /// <summary>Free-form text note.</summary>
    Note      = 3,
    /// <summary>To-do list (checklist variant).</summary>
    ToDo      = 4,
    /// <summary>Custom checklist type.</summary>
    Custom    = 5
}
