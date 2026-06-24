
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Vitarish.Entities.Generators;

[Generator]
public sealed class EntitiesRegistrationGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        string registrationAttributeName = nameof(GenerateEntitiesRegistrationAttribute);
        IAssemblySymbol assembly = context.Compilation.Assembly;
        bool hasRegistration = assembly.GetAttributes()
            .Any(a => a.AttributeClass?.Name == registrationAttributeName);
        if (!hasRegistration)
        {
            return;
        }

        string dotlessName = assembly.Name.Replace(".", "");
        string descriptorAttributeName = nameof(EntityDescriptorForAttribute);
        INamedTypeSymbol? entityBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Core.Entity");
        INamedTypeSymbol? descriptorBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Core.EntityDescriptor");
        Dictionary<INamedTypeSymbol, INamedTypeSymbol> descriptors = new(SymbolEqualityComparer.Default);

        // Search in current compilation
        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            SemanticModel model = context.Compilation.GetSemanticModel(tree);
            foreach (TypeDeclarationSyntax node in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
                {
                    continue;
                }

                foreach (AttributeData attribute in symbol.GetAttributes())
                {
                    string? attrName = attribute.AttributeClass?.Name;
                    if (attrName == descriptorAttributeName
                        && attribute.ConstructorArguments.Length == 1
                        && attribute.ConstructorArguments[0].Value is INamedTypeSymbol entityType
                        && !entityType.IsAbstract
                        && (descriptorBase is null || InheritsFrom(symbol, descriptorBase))
                        && (entityBase is null || InheritsFrom(entityType, entityBase)))
                    {
                        descriptors[entityType] = symbol;
                    }
                }
            }
        }

        // Search in referenced assemblies for descriptors
        foreach (IAssemblySymbol refAssembly in context.Compilation.References
            .Select(r => context.Compilation.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol)
            .Where(a => a is not null))
        {
            FindDescriptorsInAssembly(refAssembly!, descriptorAttributeName, entityBase, descriptorBase, descriptors);
        }
        string extensionClassName = $"EntityManagementRegistrationOf{dotlessName}";
        StringBuilder sb = new();
        _ = sb.AppendLine("#nullable enable");
        _ = sb.AppendLine();
        _ = sb.AppendLine("using System.Collections.Generic;");
        _ = sb.AppendLine("using MediatR;");
        _ = sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        _ = sb.AppendLine("using Microsoft.Extensions.Logging;");
        _ = sb.AppendLine("using Vitarish.Entities.Server.Caching;");
        _ = sb.AppendLine("using Vitarish.Entities.Server.Commands;");
        _ = sb.AppendLine("using Vitarish.Entities.Server.Core;");
        _ = sb.AppendLine("using Vitarish.Entities.Server.Infrastructure;");
        _ = sb.AppendLine("using Vitarish.Entities.Contracts;");
        _ = sb.AppendLine("using Vitarish.Entities.Server.Queries;");
        _ = sb.AppendLine("using Vitarish.Entities.Server.Services;");
        _ = sb.AppendLine("using Microsoft.EntityFrameworkCore.Diagnostics;");
        _ = sb.AppendLine();
        _ = sb.AppendLine("/// <summary>");
        _ = sb.AppendLine($"/// Auto-generated registration extensions for {assembly.Name} entities.");
        _ = sb.AppendLine("/// </summary>");
        _ = sb.AppendLine($"public static class {extensionClassName}");
        _ = sb.AppendLine("{");
        _ = sb.AppendLine("    /// <summary>");
        _ = sb.AppendLine($"    /// Registers all entity handlers, repositories, and core services for {assembly.Name}.");
        _ = sb.AppendLine("    /// </summary>");
        _ = sb.AppendLine("    /// <remarks>");
        _ = sb.AppendLine("    /// This method registers:");
        _ = sb.AppendLine("    /// - MediatR command and query handlers for all entities");
        _ = sb.AppendLine("    /// - Memory cache implementations (IEntityCache)");
        _ = sb.AppendLine("    /// - Cache invalidation services");
        _ = sb.AppendLine("    /// - Entity interceptor for audit trails");
        _ = sb.AppendLine("    /// - User and time services");
        _ = sb.AppendLine("    /// ");
        _ = sb.AppendLine("    /// Optional enhancements (call separately):");
        _ = sb.AppendLine("    /// - services.AddEntityValidation() - FluentValidation pipeline behavior");
        _ = sb.AppendLine("    /// - services.AddDistributedEntityCache&lt;T&gt;() - Redis/distributed caching");
        _ = sb.AppendLine("    /// - services.AddCachedEntityRepository&lt;T, TDescriptor&gt;() - Cache-aside pattern");
        _ = sb.AppendLine("    /// - services.AddHealthChecks().AddEntityCacheHealthCheck&lt;T&gt;() - Health monitoring");
        _ = sb.AppendLine("    /// </remarks>");
        _ = sb.AppendLine($"    public static void Add{dotlessName}EntityHandlers(this IServiceCollection services)");
        _ = sb.AppendLine("    {");
        _ = sb.AppendLine("        // Core infrastructure");
        _ = sb.AppendLine("        services.AddMemoryCache();");
        _ = sb.AppendLine($"        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof({extensionClassName}).Assembly));");
        _ = sb.AppendLine("        services.AddSingleton<IEntityCacheInvalidationService, EntityCacheInvalidationService>();");
        _ = sb.AppendLine("        services.AddScoped<IInterceptor, EntityInterceptor>();");
        _ = sb.AppendLine();

        foreach (KeyValuePair<INamedTypeSymbol, INamedTypeSymbol> kvp in descriptors)
        {
            INamedTypeSymbol entity = kvp.Key;
            INamedTypeSymbol descriptor = kvp.Value;
            string entityFull = entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string descriptorFull = descriptor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string entityName = entity.Name;

            _ = sb.AppendLine($"        // {entityName} handlers and cache");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<AddEntityCommand<{entityFull}, {descriptorFull}>, {entityFull}?>, AddEntityHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<AddEntitiesCommand<{entityFull}, {descriptorFull}>, IReadOnlyList<{entityFull}>>, AddEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<UpdateEntityCommand<{entityFull}, {descriptorFull}>, {entityFull}?>, UpdateEntityHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<AddOrUpdateEntityCommand<{entityFull}, {descriptorFull}>, {entityFull}?>, AddOrUpdateEntityHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<UpdateEntitiesCommand<{entityFull}, {descriptorFull}>, IReadOnlyList<{entityFull}>>, UpdateEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<RemoveEntityCommand<{entityFull}, {descriptorFull}>>, RemoveEntityHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<RemoveEntitiesCommand<{entityFull}, {descriptorFull}>>, RemoveEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<RemoveEntityByIdCommand<{entityFull}, {descriptorFull}>>, RemoveEntityByIdHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<RemoveEntitiesByDescriptorCommand<{entityFull}, {descriptorFull}>>, RemoveEntitiesByDescriptorHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<HardDeleteEntityCommand<{entityFull}, {descriptorFull}>>, HardDeleteEntityHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<HardDeleteEntitiesCommand<{entityFull}, {descriptorFull}>>, HardDeleteEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<HardDeleteEntityByIdCommand<{entityFull}, {descriptorFull}>>, HardDeleteEntityByIdHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<HardDeleteEntitiesByDescriptorCommand<{entityFull}, {descriptorFull}>>, HardDeleteEntitiesByDescriptorHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<GetEntityByIdQuery<{entityFull}, {descriptorFull}>, {entityFull}?>, GetEntityByIdHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<GetEntitiesQuery<{entityFull}, {descriptorFull}>, IReadOnlyList<{entityFull}>>, GetEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IRequestHandler<CountEntitiesQuery<{entityFull}, {descriptorFull}>, int>, CountEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            _ = sb.AppendLine($"        services.AddSingleton<IEntityCache<{entityFull}>, EntityMemoryCache<{entityFull}>>();");
            _ = sb.AppendLine($"        services.AddTransient<IEntityCacheInvalidator, EntityCacheInvalidator<{entityFull}>>();");
            _ = sb.AppendLine();
        }

        _ = sb.AppendLine("    }");
        _ = sb.AppendLine("}");

        context.AddSource("EntityManagementRegistration.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool InheritsFrom(INamedTypeSymbol? symbol, INamedTypeSymbol? baseType)
    {
        for (INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static void FindDescriptorsInAssembly(
        IAssemblySymbol assembly,
        string descriptorAttributeName,
        INamedTypeSymbol? entityBase,
        INamedTypeSymbol? descriptorBase,
        Dictionary<INamedTypeSymbol, INamedTypeSymbol> descriptors) => FindDescriptorsInNamespace(assembly.GlobalNamespace, descriptorAttributeName, entityBase, descriptorBase, descriptors);

    private static void FindDescriptorsInNamespace(
        INamespaceSymbol namespaceSymbol,
        string descriptorAttributeName,
        INamedTypeSymbol? entityBase,
        INamedTypeSymbol? descriptorBase,
        Dictionary<INamedTypeSymbol, INamedTypeSymbol> descriptors)
    {
        foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
        {
            // Check for descriptor attribute
            foreach (AttributeData attribute in type.GetAttributes())
            {
                string? attrName = attribute.AttributeClass?.Name;
                if (attrName == descriptorAttributeName
                    && attribute.ConstructorArguments.Length == 1
                    && attribute.ConstructorArguments[0].Value is INamedTypeSymbol entityType
                    && !entityType.IsAbstract
                    && (descriptorBase is null || InheritsFrom(type, descriptorBase))
                    && (entityBase is null || InheritsFrom(entityType, entityBase)))
                {
                    descriptors[entityType] = type;
                }
            }
        }

        foreach (INamespaceSymbol childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            FindDescriptorsInNamespace(childNamespace, descriptorAttributeName, entityBase, descriptorBase, descriptors);
        }
    }
}
