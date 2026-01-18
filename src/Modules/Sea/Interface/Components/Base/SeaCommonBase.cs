namespace Vitarish.Sea.Interface.Components.Base;

public class SeaCommonBase : CommonBase, IDisposable
{
    [Inject] protected IUsersService UsersService { get; set; } = default!;
    [Inject] protected IUsageConfigurationService UsageConfigurationService { get; set; } = default!;
    /// <summary>
    /// Optional override for admin-specific rendering when the caller wants to force admin mode.
    /// </summary>
    [CascadingParameter] public bool IsAdmin { get; set; }
    /// <summary>
    /// Effective admin flag resolved from the override or the access service.
    /// </summary>
    protected bool AdminMode => IsAdmin;
    protected UsageConfigurationDto? UsageConfiguration;
    protected UsageConfigurationMetadata? UsageConfigurationMetadata;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UsageConfiguration = await UsageConfigurationService.GetUsageConfigurationAsync(CancellationToken.None);
        UsageConfigurationMetadata = UsageConfigurationMetadata.FromJson(UsageConfiguration?.Metadata);
        if (IsAdmin)
        {
            UsageConfigurationMetadata.OnChange += OnUsageConfigurationChanged;
        }
    }

    private async void OnUsageConfigurationChanged(object? sender, EventArgs args)
    {
        if (UsageConfiguration is null)
        {
            return;
        }
        UsageConfiguration.Metadata = UsageConfigurationMetadata?.ToJson() ?? "{}";
        _ = await UsageConfigurationService.UpdateAsync(UsageConfiguration, CancellationToken.None);
    }
    public override void OnCompleted() => throw new NotImplementedException();
    public override void OnError(Exception error) => throw new NotImplementedException();
    protected override Task ReloadRequired() => Task.CompletedTask;

    void IDisposable.Dispose()
    {
        if (IsAdmin)
        {
            UsageConfigurationMetadata?.OnChange -= OnUsageConfigurationChanged;
        }
        GC.SuppressFinalize(this);
    }
}
