namespace Vitarish.Entities.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        if(!hasRegistration)
        {
            return;
        }

        string dotlessName = assembly.Name.Replace(".", "");
        string descriptorAttributeName = nameof(EntityDescriptorForAttribute);
        INamedTypeSymbol? entityBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Core.Entity");
        INamedTypeSymbol? descriptorBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Core.EntityDescriptor");
        Dictionary<INamedTypeSymbol, INamedTypeSymbol> descriptors = new(SymbolEqualityComparer.Default);

        // Search in current compilation
        foreach(SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            SemanticModel model = context.Compilation.GetSemanticModel(tree);
            foreach(TypeDeclarationSyntax node in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if(model.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
                {
                    continue;
                }

                foreach(AttributeData attribute in symbol.GetAttributes())
                {
                    string? attrName = attribute.AttributeClass?.Name;
                    if(attrName == descriptorAttributeName
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
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using MediatR;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using Vitarish.Entities.Server.Caching;");
        sb.AppendLine("using Vitarish.Entities.Server.Commands;");
        sb.AppendLine("using Vitarish.Entities.Server.Core;");
        sb.AppendLine("using Vitarish.Entities.Server.Infrastructure;");
        sb.AppendLine("using Vitarish.Entities.Server.Models;");
        sb.AppendLine("using Vitarish.Entities.Server.Queries;");
        sb.AppendLine("using Vitarish.Entities.Server.Services;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore.Diagnostics;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Auto-generated registration extensions for {assembly.Name} entities.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {extensionClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Registers all entity handlers, repositories, and core services for {assembly.Name}.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <remarks>");
        sb.AppendLine("    /// This method registers:");
        sb.AppendLine("    /// - MediatR command and query handlers for all entities");
        sb.AppendLine("    /// - Memory cache implementations (IEntityCache)");
        sb.AppendLine("    /// - Cache invalidation services");
        sb.AppendLine("    /// - Entity interceptor for audit trails");
        sb.AppendLine("    /// - User and time services");
        sb.AppendLine("    /// ");
        sb.AppendLine("    /// Optional enhancements (call separately):");
        sb.AppendLine("    /// - services.AddEntityValidation() - FluentValidation pipeline behavior");
        sb.AppendLine("    /// - services.AddDistributedEntityCache&lt;T&gt;() - Redis/distributed caching");
        sb.AppendLine("    /// - services.AddCachedEntityRepository&lt;T, TDescriptor&gt;() - Cache-aside pattern");
        sb.AppendLine("    /// - services.AddHealthChecks().AddEntityCacheHealthCheck&lt;T&gt;() - Health monitoring");
        sb.AppendLine("    /// </remarks>");
        sb.AppendLine($"    public static void Add{dotlessName}EntityHandlers(this IServiceCollection services)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Core infrastructure");
        sb.AppendLine("        services.AddMemoryCache();");
        sb.AppendLine($"        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof({extensionClassName}).Assembly));");
        sb.AppendLine("        services.AddSingleton<IEntityCacheInvalidationService, EntityCacheInvalidationService>();");
        sb.AppendLine("        services.AddScoped<IInterceptor, EntityInterceptor>();");
        sb.AppendLine();

        foreach(KeyValuePair<INamedTypeSymbol, INamedTypeSymbol> kvp in descriptors)
        {
            INamedTypeSymbol entity = kvp.Key;
            INamedTypeSymbol descriptor = kvp.Value;
            string entityFull = entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string descriptorFull = descriptor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string entityName = entity.Name;

            sb.AppendLine($"        // {entityName} handlers and cache");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<AddEntityCommand<{entityFull}, {descriptorFull}>, {entityFull}?>, AddEntityHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<AddEntitiesCommand<{entityFull}, {descriptorFull}>, IReadOnlyList<{entityFull}>>, AddEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<UpdateEntityCommand<{entityFull}, {descriptorFull}>, {entityFull}?>, UpdateEntityHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<AddOrUpdateEntityCommand<{entityFull}, {descriptorFull}>, {entityFull}?>, AddOrUpdateEntityHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<UpdateEntitiesCommand<{entityFull}, {descriptorFull}>, IReadOnlyList<{entityFull}>>, UpdateEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<RemoveEntityCommand<{entityFull}, {descriptorFull}>>, RemoveEntityHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<RemoveEntitiesCommand<{entityFull}, {descriptorFull}>>, RemoveEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<RemoveEntityByIdCommand<{entityFull}, {descriptorFull}>>, RemoveEntityByIdHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<RemoveEntitiesByDescriptorCommand<{entityFull}, {descriptorFull}>>, RemoveEntitiesByDescriptorHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<HardDeleteEntityCommand<{entityFull}, {descriptorFull}>>, HardDeleteEntityHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<HardDeleteEntitiesCommand<{entityFull}, {descriptorFull}>>, HardDeleteEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<HardDeleteEntityByIdCommand<{entityFull}, {descriptorFull}>>, HardDeleteEntityByIdHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<HardDeleteEntitiesByDescriptorCommand<{entityFull}, {descriptorFull}>>, HardDeleteEntitiesByDescriptorHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<GetEntityByIdQuery<{entityFull}, {descriptorFull}>, {entityFull}?>, GetEntityByIdHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<GetEntitiesQuery<{entityFull}, {descriptorFull}>, IReadOnlyList<{entityFull}>>, GetEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddTransient<IRequestHandler<CountEntitiesQuery<{entityFull}, {descriptorFull}>, int>, CountEntitiesHandler<{entityFull}, {descriptorFull}>>();");
            sb.AppendLine($"        services.AddSingleton<IEntityCache<{entityFull}>, EntityMemoryCache<{entityFull}>>();");
            sb.AppendLine($"        services.AddTransient<IEntityCacheInvalidator, EntityCacheInvalidator<{entityFull}>>();");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("EntityManagementRegistration.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool InheritsFrom(INamedTypeSymbol? symbol, INamedTypeSymbol? baseType)
    {
        for(INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
        {
            if(SymbolEqualityComparer.Default.Equals(current, baseType))
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
        Dictionary<INamedTypeSymbol, INamedTypeSymbol> descriptors)
    {
        FindDescriptorsInNamespace(assembly.GlobalNamespace, descriptorAttributeName, entityBase, descriptorBase, descriptors);
    }

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
            foreach(AttributeData attribute in type.GetAttributes())
            {
                string? attrName = attribute.AttributeClass?.Name;
                if(attrName == descriptorAttributeName
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
