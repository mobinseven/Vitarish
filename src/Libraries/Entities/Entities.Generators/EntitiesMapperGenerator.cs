namespace Vitarish.Entities.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Source generator that creates Riok.Mapperly-based mappers for Entity types and their DTOs.
/// </summary>
[Generator]
public sealed class EntitiesMapperGenerator : ISourceGenerator
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

        // Find all Entity types
        Dictionary<INamedTypeSymbol, EntityMapping> entityMappings = new(SymbolEqualityComparer.Default);

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
                    string entityName = symbol.Name;
                    
                    // Find corresponding DTOs by naming convention
                    INamedTypeSymbol? readDto = FindTypeByName(context.Compilation, $"{entityName}ReadDto", readDtoBase);
                    INamedTypeSymbol? writeDto = FindTypeByName(context.Compilation, $"{entityName}WriteDto", writeDtoBase);
                    INamedTypeSymbol? descriptor = FindTypeByName(context.Compilation, $"{entityName}Descriptor", descriptorBase);
                    INamedTypeSymbol? descriptorDto = FindTypeByName(context.Compilation, $"{entityName}DescriptorDto", descriptorDtoBase);

                    // Only generate mapper if all required types are found
                    if (readDto is not null && writeDto is not null && descriptor is not null && descriptorDto is not null)
                    {
                        entityMappings[symbol] = new EntityMapping(
                            symbol,
                            readDto,
                            writeDto,
                            descriptor,
                            descriptorDto
                        );
                    }
                }
            }
        }

        // Generate mapper for each entity
        foreach (KeyValuePair<INamedTypeSymbol, EntityMapping> kvp in entityMappings)
        {
            EntityMapping mapping = kvp.Value;
            string mapperCode = GenerateMapper(mapping);
            string fileName = $"{mapping.Entity.Name}Mapper.g.cs";
            context.AddSource(fileName, SourceText.From(mapperCode, Encoding.UTF8));
        }
    }

    private static string GenerateMapper(EntityMapping mapping)
    {
        string entityFull = mapping.Entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string readDtoFull = mapping.ReadDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string writeDtoFull = mapping.WriteDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string descriptorFull = mapping.Descriptor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string descriptorDtoFull = mapping.DescriptorDto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        string entityNamespace = mapping.Entity.ContainingNamespace.ToDisplayString();
        string mapperName = $"{mapping.Entity.Name}Mapper";

        StringBuilder sb = new();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Riok.Mapperly.Abstractions;");
        sb.AppendLine("using Vitarish.Entities.Server.Services;");
        sb.AppendLine();
        sb.AppendLine($"namespace {entityNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Auto-generated mapper for {mapping.Entity.Name} entity and its DTOs.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]");
        sb.AppendLine($"internal partial class {mapperName} : IEntityMapper<{entityFull}, {readDtoFull}, {writeDtoFull}, {descriptorFull}, {descriptorDtoFull}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial {readDtoFull} ToDto({entityFull} entity);");
        sb.AppendLine($"    public partial {entityFull} ToDomain({writeDtoFull} dto);");
        sb.AppendLine($"    public partial {descriptorFull} ToDomain({descriptorDtoFull} dto);");
        sb.AppendLine("}");

        return sb.ToString();
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

    private sealed record EntityMapping(
        INamedTypeSymbol Entity,
        INamedTypeSymbol ReadDto,
        INamedTypeSymbol WriteDto,
        INamedTypeSymbol Descriptor,
        INamedTypeSymbol DescriptorDto
    );
}
