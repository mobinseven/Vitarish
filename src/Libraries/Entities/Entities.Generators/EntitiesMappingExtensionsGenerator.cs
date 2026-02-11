namespace Vitarish.Entities.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Source generator that creates extension methods for mapping between Entity types and their DTOs.
/// Generates ToDto, ToDomain extension methods that can be used anywhere in the codebase.
/// </summary>
[Generator]
public sealed class EntitiesMappingExtensionsGenerator : ISourceGenerator
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
        INamedTypeSymbol? entityBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Core.Entity");
        INamedTypeSymbol? descriptorBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Core.EntityDescriptor");
        INamedTypeSymbol? readDtoBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Models.EntityReadDto");
        INamedTypeSymbol? writeDtoBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Models.EntityWriteDto");
        INamedTypeSymbol? descriptorDtoBase = context.Compilation.GetTypeByMetadataName("Vitarish.Entities.Server.Models.EntityDescriptorDto");

        // Find all Entity types and their corresponding DTOs
        List<EntityMappingInfo> mappings = new List<EntityMappingInfo>();

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

                // Check if it's an Entity type
                if (!symbol.IsAbstract && entityBase is not null && InheritsFrom(symbol, entityBase))
                {
                    TryAddMapping(context.Compilation, symbol, readDtoBase, writeDtoBase, descriptorBase, descriptorDtoBase, mappings);
                }
            }
        }

        // Search in referenced assemblies for Entity types
        foreach (IAssemblySymbol refAssembly in context.Compilation.References
            .Select(r => context.Compilation.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol)
            .Where(a => a is not null))
        {
            FindEntitiesInAssembly(refAssembly!, entityBase, context.Compilation, readDtoBase, writeDtoBase, descriptorBase, descriptorDtoBase, mappings);
        }

        // Generate mapper classes and extension methods for all found mappings
        if (mappings.Count > 0)
        {
            // Generate individual mapper class files
            foreach (var mapping in mappings)
            {
                string mapperCode = GenerateMapperClass(mapping);
                string entityName = mapping.Entity.Name;
                context.AddSource($"{entityName}EntityMapper.g.cs", SourceText.From(mapperCode, Encoding.UTF8));
            }

            // Generate extension methods class
            string extensionsCode = GenerateExtensionMethodsClass(dotlessName, mappings);
            context.AddSource($"EntityMappingExtensions.g.cs", SourceText.From(extensionsCode, Encoding.UTF8));
        }
    }

    private static void TryAddMapping(
        Compilation compilation,
        INamedTypeSymbol entityType,
        INamedTypeSymbol? readDtoBase,
        INamedTypeSymbol? writeDtoBase,
        INamedTypeSymbol? descriptorBase,
        INamedTypeSymbol? descriptorDtoBase,
        List<EntityMappingInfo> mappings)
    {
        string entityName = entityType.Name;
        
        // Find corresponding DTOs by naming convention
        INamedTypeSymbol? readDto = FindTypeByName(compilation, $"{entityName}ReadDto", readDtoBase);
        INamedTypeSymbol? writeDto = FindTypeByName(compilation, $"{entityName}WriteDto", writeDtoBase);
        INamedTypeSymbol? descriptor = FindTypeByName(compilation, $"{entityName}Descriptor", descriptorBase);
        INamedTypeSymbol? descriptorDto = FindTypeByName(compilation, $"{entityName}DescriptorDto", descriptorDtoBase);

        // Only generate mapping if all required types are found
        if (readDto is not null && writeDto is not null && descriptor is not null && descriptorDto is not null)
        {
            mappings.Add(new EntityMappingInfo(entityType, readDto, writeDto, descriptor, descriptorDto));
        }
    }

    private static string GenerateMapperClass(EntityMappingInfo mapping)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        
        string entityFull = mapping.Entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string readDtoFull = mapping.ReadDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string writeDtoFull = mapping.WriteDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string descriptorFull = mapping.Descriptor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string descriptorDtoFull = mapping.DescriptorDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        string entityName = mapping.Entity.Name;
        string entityNamespace = mapping.Entity.ContainingNamespace.ToDisplayString();

        sb.AppendLine($"namespace {entityNamespace}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Auto-generated mapper for {entityName} entity using Riok.Mapperly source generation.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [global::Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = global::Riok.Mapperly.Abstractions.RequiredMappingStrategy.Source)]");
        sb.AppendLine($"    public partial class {entityName}EntityMapper");
        sb.AppendLine("    {");
        
        // Generate ToDto method (Entity -> ReadDto)
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Converts a {entityName} entity to its read DTO representation.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public {readDtoFull} ToDto({entityFull} src)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            return new {readDtoFull}");
        sb.AppendLine($"            {{");
        GeneratePropertyMappings(sb, mapping.Entity, mapping.ReadDto, "src", "                ");
        sb.AppendLine($"            }};");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        // Generate ToDomain method (WriteDto -> Entity)
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Converts a {entityName} write DTO to its entity representation.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public {entityFull} ToDomain({writeDtoFull} src)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            return new {entityFull}");
        sb.AppendLine($"            {{");
        GeneratePropertyMappings(sb, mapping.WriteDto, mapping.Entity, "src", "                ");
        sb.AppendLine($"            }};");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        // Generate ToDomain method (DescriptorDto -> Descriptor)
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Converts a {entityName} descriptor DTO to its descriptor representation.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public {descriptorFull} ToDomain({descriptorDtoFull} src)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            return new {descriptorFull}");
        sb.AppendLine($"            {{");
        GeneratePropertyMappings(sb, mapping.DescriptorDto, mapping.Descriptor, "src", "                ");
        sb.AppendLine($"            }};");
        sb.AppendLine($"        }}");
        
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private static void GeneratePropertyMappings(StringBuilder sb, INamedTypeSymbol sourceType, INamedTypeSymbol targetType, string sourceVar, string indent)
    {
        // Get all settable properties from target type
        var targetProperties = GetAllProperties(targetType)
            .Where(p => !p.IsReadOnly && !p.IsStatic && p.SetMethod is not null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
            .ToDictionary(p => p.Name, p => p);

        // Get all readable properties from source type
        var sourceProperties = GetAllProperties(sourceType)
            .Where(p => !p.IsStatic && p.GetMethod is not null && p.GetMethod.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        foreach (var sourceProp in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProp.Name, out var targetProp))
            {
                // Check if types are compatible
                if (SymbolEqualityComparer.Default.Equals(sourceProp.Type, targetProp.Type) ||
                    IsAssignableFrom(targetProp.Type, sourceProp.Type))
                {
                    sb.AppendLine($"{indent}{targetProp.Name} = {sourceVar}.{sourceProp.Name},");
                }
            }
        }
    }

    private static IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol type)
    {
        var current = type;
        while (current is not null)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol prop)
                {
                    yield return prop;
                }
            }
            current = current.BaseType;
        }
    }

    private static bool IsAssignableFrom(ITypeSymbol target, ITypeSymbol source)
    {
        // Simple check for inheritance
        if (SymbolEqualityComparer.Default.Equals(target, source))
        {
            return true;
        }

        if (source is INamedTypeSymbol namedSource)
        {
            var baseType = namedSource.BaseType;
            while (baseType is not null)
            {
                if (SymbolEqualityComparer.Default.Equals(target, baseType))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
        }

        return false;
    }

    private static string GenerateExtensionMethodsClass(string dotlessName, List<EntityMappingInfo> mappings)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Vitarish.Entities.Server.Extensions");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Auto-generated mapping extension methods for {dotlessName} entities.");
        sb.AppendLine("    /// These methods provide convenient mapping between Entity types and their DTOs.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static class {dotlessName}EntityMappingExtensions");
        sb.AppendLine("    {");

        // Generate static mapper instances for efficiency
        foreach (var mapping in mappings)
        {
            string entityName = mapping.Entity.Name;
            string entityNamespace = mapping.Entity.ContainingNamespace.ToDisplayString();
            string mapperFull = $"global::{entityNamespace}.{entityName}EntityMapper";
            
            sb.AppendLine($"        private static readonly {mapperFull} {entityName}Mapper = new {mapperFull}();");
        }
        sb.AppendLine();

        foreach (var mapping in mappings)
        {
            string entityFull = mapping.Entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string readDtoFull = mapping.ReadDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string writeDtoFull = mapping.WriteDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string descriptorFull = mapping.Descriptor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string descriptorDtoFull = mapping.DescriptorDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string entityName = mapping.Entity.Name;

            // Generate ToDto extension method (Entity -> ReadDto) - delegates to mapper
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Converts a {entityName} entity to its read DTO representation.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public static {readDtoFull} ToDto(this {entityFull} entity)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return {entityName}Mapper.ToDto(entity);");
            sb.AppendLine($"        }}");
            sb.AppendLine();

            // Generate ToDomain extension method (WriteDto -> Entity) - delegates to mapper
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Converts a {entityName} write DTO to its entity representation.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public static {entityFull} ToDomain(this {writeDtoFull} dto)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return {entityName}Mapper.ToDomain(dto);");
            sb.AppendLine($"        }}");
            sb.AppendLine();

            // Generate ToDomain extension method (DescriptorDto -> Descriptor) - delegates to mapper
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Converts a {entityName} descriptor DTO to its descriptor representation.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public static {descriptorFull} ToDomain(this {descriptorDtoFull} dto)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return {entityName}Mapper.ToDomain(dto);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private static void FindEntitiesInAssembly(
        IAssemblySymbol assembly,
        INamedTypeSymbol? entityBase,
        Compilation compilation,
        INamedTypeSymbol? readDtoBase,
        INamedTypeSymbol? writeDtoBase,
        INamedTypeSymbol? descriptorBase,
        INamedTypeSymbol? descriptorDtoBase,
        List<EntityMappingInfo> mappings)
    {
        FindEntitiesInNamespace(assembly.GlobalNamespace, entityBase, compilation, readDtoBase, writeDtoBase, descriptorBase, descriptorDtoBase, mappings);
    }

    private static void FindEntitiesInNamespace(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol? entityBase,
        Compilation compilation,
        INamedTypeSymbol? readDtoBase,
        INamedTypeSymbol? writeDtoBase,
        INamedTypeSymbol? descriptorBase,
        INamedTypeSymbol? descriptorDtoBase,
        List<EntityMappingInfo> mappings)
    {
        foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
        {
            // Check if it's an Entity type
            if (!type.IsAbstract && entityBase is not null && InheritsFrom(type, entityBase))
            {
                TryAddMapping(compilation, type, readDtoBase, writeDtoBase, descriptorBase, descriptorDtoBase, mappings);
            }
        }

        foreach (INamespaceSymbol childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            FindEntitiesInNamespace(childNamespace, entityBase, compilation, readDtoBase, writeDtoBase, descriptorBase, descriptorDtoBase, mappings);
        }
    }

    private static INamedTypeSymbol? FindTypeByName(Compilation compilation, string typeName, INamedTypeSymbol? baseType)
    {
        // First, search in the current compilation
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

        // Search in referenced assemblies
        foreach (IAssemblySymbol assembly in compilation.References
            .Select(r => compilation.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol)
            .Where(a => a is not null))
        {
            INamedTypeSymbol? type = FindTypeInAssembly(assembly!, typeName, baseType);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? FindTypeInAssembly(IAssemblySymbol assembly, string typeName, INamedTypeSymbol? baseType)
    {
        return FindTypeInNamespace(assembly.GlobalNamespace, typeName, baseType);
    }

    private static INamedTypeSymbol? FindTypeInNamespace(INamespaceSymbol namespaceSymbol, string typeName, INamedTypeSymbol? baseType)
    {
        foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
        {
            if (type.Name == typeName 
                && !type.IsAbstract
                && (baseType is null || InheritsFrom(type, baseType)))
            {
                return type;
            }
        }

        foreach (INamespaceSymbol childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            INamedTypeSymbol? type = FindTypeInNamespace(childNamespace, typeName, baseType);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
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

    private sealed class EntityMappingInfo
    {
        public INamedTypeSymbol Entity { get; }
        public INamedTypeSymbol ReadDto { get; }
        public INamedTypeSymbol WriteDto { get; }
        public INamedTypeSymbol Descriptor { get; }
        public INamedTypeSymbol DescriptorDto { get; }

        public EntityMappingInfo(
            INamedTypeSymbol entity,
            INamedTypeSymbol readDto,
            INamedTypeSymbol writeDto,
            INamedTypeSymbol descriptor,
            INamedTypeSymbol descriptorDto)
        {
            Entity = entity;
            ReadDto = readDto;
            WriteDto = writeDto;
            Descriptor = descriptor;
            DescriptorDto = descriptorDto;
        }
    }
}
