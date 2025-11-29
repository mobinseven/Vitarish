namespace Vorn.Aaas.Interface.Presentation.Components.Admin;

public partial class ClaimsEdit : CommonBase
{
    [Parameter] public List<ClaimDto>? UserClaims { get; set; }
    [Parameter] public EventCallback<List<ClaimDto>> UserClaimsChanged { get; set; }

    HashSet<ClaimDto> selectedClaims = new();
    void Delete()
    {
        UserClaims = UserClaims?.Except(selectedClaims).ToList();
        selectedClaims.Clear();
        UserClaimsChanged.InvokeAsync(UserClaims);
    }
    void Add() => UserClaims?.Add(new ClaimDto { Type = "NewClaim", Value = "NewValue" });

}
