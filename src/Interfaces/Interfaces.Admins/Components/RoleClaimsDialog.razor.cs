using Microsoft.AspNetCore.Components;
using MudBlazor;
using ClaimDto = Vitarish.Users.Domain.Models.ClaimDto;

namespace Vitarish.Interfaces.Admins.Components;

public partial class RoleClaimsDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string RoleName { get; set; } = string.Empty;

    [Parameter] public List<ClaimDto> Claims { get; set; } = [];

    private List<ClaimDto> claims = [];

    protected override void OnParametersSet()
    {
        claims = Claims.Select(claim => new ClaimDto(claim.Type, claim.Value)).ToList();
    }

    private void UpdateClaims(List<ClaimDto> updatedClaims)
    {
        claims = updatedClaims ?? [];
    }

    private void Cancel() => MudDialog.Cancel();

    private void Save() => MudDialog.Close(DialogResult.Ok(claims));
}
