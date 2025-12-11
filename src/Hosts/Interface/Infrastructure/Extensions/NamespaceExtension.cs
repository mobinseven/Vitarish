namespace Vitarish.Interface.Infrastructure.Extensions;

public static class NamespaceExtension
{
    public static string NamespaceToPath(Type type)
    {
        string ns = type.Namespace ?? throw new ArgumentNullException(nameof(ns));
        // Assembly name is usually the "root" namespace
        string assemblyName = type.Assembly.GetName().Name ?? "";
        string[] nsParts = ns.Split('.', StringSplitOptions.RemoveEmptyEntries);
        string[] asmParts = assemblyName.Split('.', StringSplitOptions.RemoveEmptyEntries);

        // If assembly name is a prefix, keep it as the root
        if (ns.StartsWith(assemblyName))
        {
            string root = string.Join(".", asmParts);
            string path = string.Join("/", nsParts[asmParts.Length..]);
            return string.IsNullOrEmpty(path) ? root : root + "/" + path;
        }

        // Fallback: just return unchanged
        return ns;
    }
}
