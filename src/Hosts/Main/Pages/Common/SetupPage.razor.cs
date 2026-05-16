using Microsoft.Extensions.Options;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Modules.Base;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Main.Pages.Common;

public partial class SetupPage
{
    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Inject] private IOptions<FeaturesOptions> FeaturesOptions { get; set; } = default!;
    [Inject] private IReadOnlyList<IModule> Modules { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private readonly TenantSetupContext context = new();
    private bool submitting;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (!FeaturesOptions.Value.TenantOnboarding)
        {
            NavigationManager.NavigateTo(AppRoutes.Home);
            return;
        }

        if (RequestContext.UserId is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Account.Login);
        }
    }

    private Task GoToIndicator()
    {
        if (string.IsNullOrWhiteSpace(context.OwnerName) ||
            string.IsNullOrWhiteSpace(context.TenantName) ||
            string.IsNullOrWhiteSpace(context.AppId))
        {
            Snackbar.Add(Localizer["RequiredFields"], Severity.Warning);
            return Task.CompletedTask;
        }

        context.CurrentStep = SetupStep.Indicator;
        return Task.CompletedTask;
    }

    private Task GoToModules()
    {
        if (string.IsNullOrWhiteSpace(context.IndicatorValue))
        {
            Snackbar.Add(Localizer["RequiredFields"], Severity.Warning);
            return Task.CompletedTask;
        }

        context.CurrentStep = SetupStep.Modules;
        return Task.CompletedTask;
    }

    private Task GoBack()
    {
        context.CurrentStep = context.CurrentStep switch
        {
            SetupStep.Indicator => SetupStep.Details,
            SetupStep.Modules => SetupStep.Indicator,
            _ => SetupStep.Details
        };
        return Task.CompletedTask;
    }

    private void ToggleModule(string moduleName, bool selected)
    {
        if (selected)
            context.SelectedModules.Add(moduleName);
        else
            context.SelectedModules.Remove(moduleName);
    }

    private async Task Submit()
    {
        if (RequestContext.UserId is null)
        {
            return;
        }

        submitting = true;

        SetupTenantRequest request = new(
            OwnerName: context.OwnerName,
            TenantName: context.TenantName,
            AppId: context.AppId,
            IndicatorMethod: context.IndicatorMethod,
            IndicatorValue: context.IndicatorValue,
            IndicatorStatus: IndicatorStatus.Active,
            SelectedModules: [.. context.SelectedModules],
            UserId: RequestContext.UserId);

        TenantReadDto? tenant = await PartiesService.SetupTenantAsync(request);

        submitting = false;

        if (tenant is null)
        {
            Snackbar.Add(Localizer["Error"], Severity.Error);
            return;
        }

        NavigationManager.NavigateTo(AppRoutes.Home, forceLoad: true);
    }
}
