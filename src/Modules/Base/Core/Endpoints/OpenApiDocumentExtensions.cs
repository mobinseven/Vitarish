using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Modules.Base.Core.Endpoints;

public static class OpenApiDocumentExtensions
{
    public static IServiceCollection AddModuleOpenApiDocument(this IServiceCollection services, string documentName, string moduleGroupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleGroupName);

        return services.AddOpenApi(documentName, options =>
        {
            options.ShouldInclude = description => IsModuleOpenApiDescription(description, moduleGroupName);
        });
    }

    public static IServiceCollection AddModuleOpenApiDocuments(this IServiceCollection services, IEnumerable<string> moduleNames)
    {
        foreach (string name in moduleNames)
        {
            _ = services.AddModuleOpenApiDocument(name, name);
        }
        return services;
    }

    private static bool IsModuleOpenApiDescription(ApiDescription description, string moduleGroupName)
    {
        if (string.Equals(description.GroupName, moduleGroupName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return description.ActionDescriptor.EndpointMetadata
            .OfType<ITagsMetadata>()
            .SelectMany(metadata => metadata.Tags)
            .Any(tag => string.Equals(tag, moduleGroupName, StringComparison.OrdinalIgnoreCase));
    }
}
