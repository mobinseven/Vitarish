namespace Vorn.Interface.Localization;

using System;

public sealed class CompositeStringLocalizerOptions
{
    public Type[] ResourceTypesInPriorityOrder { get; set; } = [];
}
