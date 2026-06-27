namespace Vitarish.Modules.Aggregator.Interface.Models;

public class SearchArgs(string? SearchString) : EventArgs
{
    public string? SearchedValue => SearchString;
}
