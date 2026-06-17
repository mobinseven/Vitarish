using Vitarish.Interfaces.Base.Enums;

namespace Vitarish.Interfaces.Base.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PageColorAttribute(Palettes palette) : Attribute
{
    public Palettes Palette { get; } = palette;
}
