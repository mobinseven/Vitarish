using Vitarish.Modules.Base.Interface.Enums;

namespace Vitarish.Modules.Base.Interface.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PageColorAttribute(Palettes palette) : Attribute
{
    public Palettes Palette { get; } = palette;
}
