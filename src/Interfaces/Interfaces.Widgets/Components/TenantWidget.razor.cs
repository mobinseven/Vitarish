using Microsoft.AspNetCore.Components;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class TenantWidget
{
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;

    protected TenantReadDto? Tenant;
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if(RequestContext.TenantId is not null && RequestContext.IsTenantUser())
        {
            Tenant = await PartiesService.GetTenantAsync(Guid.Parse(RequestContext.TenantId));
        }
        StateHasChanged();
    }
}
