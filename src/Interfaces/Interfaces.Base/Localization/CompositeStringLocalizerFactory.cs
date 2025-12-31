using Microsoft.Extensions.Options;

namespace Vitarish.Interfaces.Base.Localization;

public sealed class CompositeStringLocalizerFactory(
    IStringLocalizerFactory innerFactory,
    IOptions<CompositeStringLocalizerOptions> optionsAccessor) : IStringLocalizerFactory
{
    private readonly IStringLocalizerFactory _innerFactory = innerFactory;
    private readonly IOptions<CompositeStringLocalizerOptions> _optionsAccessor = optionsAccessor;

    public IStringLocalizer Create(Type resourceSource)
        => Create(resourceSource.FullName!, resourceSource.Assembly.GetName().Name!);

    public IStringLocalizer Create(string baseName, string location)
    {
        Type[] resourceTypes = _optionsAccessor.Value.ResourceTypesInPriorityOrder;
        if (resourceTypes.Length == 0)
        {
            return _innerFactory.Create(baseName, location);
        }

        Type? matchingType = string.IsNullOrWhiteSpace(baseName)
            ? null
            : FindMatchingType(baseName, resourceTypes);

        IReadOnlyList<Type> orderedResourceTypes = OrderResourceTypes(matchingType, resourceTypes);

        List<IStringLocalizer> localizers = new(orderedResourceTypes.Count + 1)
        {
            matchingType is null
                ? _innerFactory.Create(baseName, location)
                : _innerFactory.Create(matchingType)
        };

        foreach (Type type in orderedResourceTypes)
        {
            if (matchingType is not null && ReferenceEquals(type, matchingType))
            {
                continue;
            }

            localizers.Add(_innerFactory.Create(type));
        }

        return new CompositeStringLocalizer(localizers);
    }

    private static IReadOnlyList<Type> OrderResourceTypes(Type? matchingType, Type[] resourceTypes)
    {
        if (matchingType is null || ReferenceEquals(resourceTypes[0], matchingType))
        {
            return resourceTypes;
        }

        Type[] ordered = new Type[resourceTypes.Length];
        ordered[0] = matchingType;
        int index = 1;

        foreach (Type type in resourceTypes)
        {
            if (ReferenceEquals(type, matchingType))
            {
                continue;
            }

            ordered[index++] = type;
        }

        return ordered;
    }

    private static Type? FindMatchingType(string baseName, IEnumerable<Type> resourceTypes)
    {
        foreach (Type type in resourceTypes)
        {
            if (string.Equals(type.FullName, baseName, StringComparison.Ordinal) ||
               string.Equals(type.Name, baseName, StringComparison.Ordinal) ||
               baseName.EndsWith($".{type.Name}", StringComparison.Ordinal))
            {
                return type;
            }
        }

        return null;
    }
}
