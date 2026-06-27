namespace Vitarish.Modules.Base.Interface.Models;

public class SearchArgs(string? SearchString) : EventArgs
{
    public string? SearchedValue => SearchString;
}
