namespace Vitarish.Admins.Interface.Services
{
    public interface IPackageProvider
    {
        IReadOnlyList<Package> GetPackages();
    }
}