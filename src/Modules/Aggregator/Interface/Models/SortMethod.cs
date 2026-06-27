namespace Vitarish.Modules.Aggregator.Interface.Models;

public record SortMethod(Func<string> LabelAction, Action<bool> SortAction);
