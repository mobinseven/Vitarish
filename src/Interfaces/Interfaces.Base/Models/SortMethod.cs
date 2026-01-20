namespace Vitarish.Interfaces.Base.Models;

public record SortMethod(Func<string> LabelAction, Action SortAction);
