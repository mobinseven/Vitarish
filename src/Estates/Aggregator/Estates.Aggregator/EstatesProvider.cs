using Microsoft.Extensions.Localization;
using Vitarish.Estates.Base;

namespace Vitarish.Estates.Aggregator;

public record EstateRecord(string Id, string Name);

public class EstatesProvider(IStringLocalizer localizer) : IEstatesProvider
{
    public IReadOnlyList<EstateRecord> GetEstateRecords()
    {
        List<IEstate> estates = EstatesExtensions.GetEstates();
        return [.. estates.Select(e => new EstateRecord(e.Name, localizer[e.Name]))];
    }
}

public interface IEstatesProvider
{
    IReadOnlyList<EstateRecord> GetEstateRecords();
}
