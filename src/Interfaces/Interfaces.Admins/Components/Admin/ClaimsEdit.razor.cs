using Vitarish.Interfaces.Admins.Components.Base;
using ClaimDto = Vitarish.Users.Domain.Models.ClaimDto;

namespace Vitarish.Interfaces.Admins.Components.Admin;

public partial class ClaimsEdit : AdminCommonBase
{
    [Parameter] public List<ClaimDto>? UserClaims { get; set; }
    [Parameter] public EventCallback<List<ClaimDto>> UserClaimsChanged { get; set; }

    private HashSet<ClaimDto> selectedClaims = [];
    private void Delete()
    {
        UserClaims = UserClaims?.Except(selectedClaims).ToList();
        selectedClaims.Clear();
        _ = UserClaimsChanged.InvokeAsync(UserClaims);
    }
    private void Add() => UserClaims?.Add(new ClaimDto { Type = "NewClaim", Value = "NewValue" });

}
