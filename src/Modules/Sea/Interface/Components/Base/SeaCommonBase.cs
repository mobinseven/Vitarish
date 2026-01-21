namespace Vitarish.Sea.Interface.Components.Base;

public class SeaCommonBase : CommonBase, IDisposable
{
    [Inject] protected IInterfaceDataProvider DataProvider { get; set; } = default!;
    [Inject] protected IUsageConfigurationService UsageConfigurationService { get; set; } = default!;
    /// <summary>
    /// Optional override for admin-specific rendering when the caller wants to force admin mode.
    /// </summary>
    [CascadingParameter] public bool IsAdmin { get; set; } = false;
    /// <summary>
    /// Effective admin flag resolved from the override or the access service.
    /// </summary>
    protected bool AdminMode => IsAdmin;
    protected UsageConfigurationDto? UsageConfiguration => DataProvider.UsageConfiguration;
    protected UsageConfigurationMetadata? UsageConfigurationMetadata => DataProvider.UsageConfigurationMetadata;
    protected IReadOnlyList<UserDto> Users => DataProvider.Users ?? [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await DataProvider.Initialize();
        await ReloadRequired();
    }
    protected string? UserIdToName(Guid id) => Users.FirstOrDefault(x => x.Id == id.ToString())?.FullName;
    protected string? UserIdToName(string id) => Users.FirstOrDefault(x => x.Id == id)?.FullName;
}
