using Vitarish.Modules.Aggregator.Interface.Enums;

namespace Vitarish.Modules.Aggregator.Interface.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PageColorAttribute(Palettes palette) : Attribute
{
    public Palettes Palette { get; } = palette;
}
