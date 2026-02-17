namespace Vitarish.Interfaces.Base.Models;

public class SearchArgs(string? SearchString) : EventArgs
{
    public string? SearchedValue => SearchString;
}
