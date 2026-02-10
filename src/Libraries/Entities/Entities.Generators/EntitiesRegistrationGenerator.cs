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
        INamedTypeSymbol? readDtoBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Models.EntityReadDto");
        INamedTypeSymbol? writeDtoBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Models.EntityWriteDto");
        INamedTypeSymbol? descriptorDtoBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Models.EntityDescriptorDto");
        Dictionary<INamedTypeSymbol, INamedTypeSymbol> descriptors = new(SymbolEqualityComparer.Default);
        Dictionary<INamedTypeSymbol, MapperInfo> mappers = new(SymbolEqualityComparer.Default);

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

                // Check if it's an Entity type to find mapper
                if (!symbol.IsAbstract && entityBase is not null && InheritsFrom(symbol, entityBase))
                {
                    string entityName = symbol.Name;
                    
                    // Find corresponding DTOs by naming convention
                    INamedTypeSymbol? readDto = FindTypeByName(context.Compilation, $"{entityName}ReadDto", readDtoBase);
                    INamedTypeSymbol? writeDto = FindTypeByName(context.Compilation, $"{entityName}WriteDto", writeDtoBase);
                    INamedTypeSymbol? descriptor = FindTypeByName(context.Compilation, $"{entityName}Descriptor", descriptorBase);
                    INamedTypeSymbol? descriptorDto = FindTypeByName(context.Compilation, $"{entityName}DescriptorDto", descriptorDtoBase);

                    // Only use mapper if all required types are found
                    if (readDto is not null && writeDto is not null && descriptor is not null && descriptorDto is not null)
                    {
                        mappers[symbol] = new MapperInfo(
                            readDto,
                            writeDto,
                            descriptor,
                            descriptorDto
                        );
                    }
                }
            }
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
        sb.AppendLine("        services.AddLogging(builder => builder.AddConsole());");
        sb.AppendLine($"        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof({extensionClassName}).Assembly));");
        sb.AppendLine("        services.AddSingleton<IEntityCacheInvalidationService, EntityCacheInvalidationService>();");
        sb.AppendLine("        services.AddScoped<EntityInterceptor>();");
        sb.AppendLine();

        foreach(KeyValuePair<INamedTypeSymbol, INamedTypeSymbol> kvp in descriptors)
        {
            INamedTypeSymbol entity = kvp.Key;
            INamedTypeSymbol descriptor = kvp.Value;
            string entityFull = entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string descriptorFull = descriptor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string entityName = entity.Name;

            sb.AppendLine($"        // {entityName} handlers and cache");

            // Register mapper if available
            if (mappers.TryGetValue(entity, out MapperInfo? mapperInfo))
            {
                string readDtoFull = mapperInfo.ReadDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string writeDtoFull = mapperInfo.WriteDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string descriptorDtoFull = mapperInfo.DescriptorDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string mapperFull = $"{entity.ContainingNamespace.ToDisplayString()}.{entityName}Mapper";
                sb.AppendLine($"        services.AddSingleton<IEntityMapper<{entityFull}, {readDtoFull}, {writeDtoFull}, {descriptorFull}, {descriptorDtoFull}>, {mapperFull}>();");
            }

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

    private static INamedTypeSymbol? FindTypeByName(Compilation compilation, string typeName, INamedTypeSymbol? baseType)
    {
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (TypeDeclarationSyntax node in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(node) is INamedTypeSymbol symbol 
                    && symbol.Name == typeName
                    && !symbol.IsAbstract
                    && (baseType is null || InheritsFrom(symbol, baseType)))
                {
                    return symbol;
                }
            }
        }
        return null;
    }

    private sealed record MapperInfo(
        INamedTypeSymbol ReadDto,
        INamedTypeSymbol WriteDto,
        INamedTypeSymbol Descriptor,
        INamedTypeSymbol DescriptorDto
    );
}
