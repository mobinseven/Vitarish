using Microsoft.AspNetCore.Components;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class TenantWidget
{
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Parameter] public EventCallback<string> OnTenantNavigate { get; set; }

    protected TenantReadDto? Tenant;
    private bool navigationRequested;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (RequestContext.TenantId is not null && RequestContext.IsTenantUser())
        {
            Tenant = await PartiesService.GetTenantAsync(Guid.Parse(RequestContext.TenantId));
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (Tenant is not null)
        {
            if (navigationRequested)
            {
                return;
            }

            navigationRequested = true;
            await OnTenantNavigate.InvokeAsync(Tenant.AppId);
        }
    }
}
