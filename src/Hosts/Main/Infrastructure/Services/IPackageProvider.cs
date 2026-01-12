
namespace Vitarish.Main.Infrastructure.Services
{
    public interface IPackageProvider
    {
        IReadOnlyList<Package> GetPackages();
    }
}