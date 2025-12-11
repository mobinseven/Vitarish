namespace Vitarish.Clients.Interface.Localization;

[RegisterScoped(ImplementationType = typeof(CustomMudLocalizer), ServiceType = typeof(MudLocalizer))]
internal class CustomMudLocalizer(IStringLocalizer localizer) : MudLocalizer
{
    public override LocalizedString this[string key] => localizer[key];

    public override LocalizedString this[string key, params object[] arguments] => localizer[key, arguments];
}

