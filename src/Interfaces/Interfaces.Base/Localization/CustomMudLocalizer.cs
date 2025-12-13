using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Vitarish.Interfaces.Base.Localization;

public class CustomMudLocalizer(IStringLocalizer localizer) : MudLocalizer
{
    public override LocalizedString this[string key] => localizer[key];

    public override LocalizedString this[string key, params object[] arguments] => localizer[key, arguments];
}

