namespace Vitarish.Main.Infrastructure.Services;

public record Package(string Id, string Name);

public class PackageProvider(IStringLocalizer localizer) : IPackageProvider
{
    public IReadOnlyList<Package> GetPackages()
    {
        return new List<Package> { new Package("sea", localizer["Sea"]) };
    }
}
