using Microsoft.Extensions.Options;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Main.Pages.Common;

public partial class SetupPage
{
    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Inject] private IOptions<FeaturesOptions> FeaturesOptions { get; set; } = default!;
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
        if (!RequireFields(context.OwnerName, context.TenantName, context.AppId))
        {
            return Task.CompletedTask;
        }

        context.CurrentStep = SetupStep.Indicator;
        return Task.CompletedTask;
    }

    private Task GoToModules()
    {
        if (!RequireFields(context.IndicatorValue))
        {
            return Task.CompletedTask;
        }

        context.CurrentStep = SetupStep.Modules;
        return Task.CompletedTask;
    }

    private bool RequireFields(params string[] fields)
    {
        if (fields.Any(string.IsNullOrWhiteSpace))
        {
            Snackbar.Add(Localizer["RequiredFields"], Severity.Warning);
            return false;
        }
        return true;
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

    private async Task Submit()
    {
        if (RequestContext.UserId is null)
        {
            return;
        }

        submitting = true;
        try
        {
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

            if (tenant is null)
            {
                Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            NavigationManager.NavigateTo(AppRoutes.Home, forceLoad: true);
        }
        finally
        {
            submitting = false;
        }
    }
}
