namespace Vitarish.Modules.Base.Interface.Models;

public record SortMethod(Func<string> LabelAction, Action<bool> SortAction);
