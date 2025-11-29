using System;

namespace Vorn.Files.Client.Internal;

public static class NamespaceExtension
{
    public static string NamespaceToPath(Type type)
    {
        string ns = type.Namespace ?? throw new ArgumentNullException(nameof(ns));
        string assemblyName = type.Assembly.GetName().Name ?? "";
        var nsParts = ns.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var asmParts = assemblyName.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (ns.StartsWith(assemblyName))
        {
            string root = string.Join(".", asmParts);
            string path = string.Join("/", nsParts[asmParts.Length..]);
            return string.IsNullOrEmpty(path) ? root : root + "/" + path;
        }

        return ns;
    }
}
