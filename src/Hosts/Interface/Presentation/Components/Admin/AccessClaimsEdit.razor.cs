using Vitarish.Interface.Identity.Contracts;
using Vitarish.Users.Infrastructure;
using ClaimDto = Vitarish.Users.Domain.Models.ClaimDto;

namespace Vitarish.Interface.Presentation.Components.Admin;

public partial class AccessClaimsEdit : CommonBase
{
    private MudDataGrid<AccessClaimModel> dataGrid;

    [Parameter] public List<ClaimDto>? AccessClaims { get; set; }
    [Parameter] public EventCallback<List<ClaimDto>> AccessClaimsChanged { get; set; }

    private List<AccessClaimModel> accessClaims = [];
    private HashSet<AccessClaimModel> selectedClaims = [];

    protected override void OnParametersSet()
    {
        if (AccessClaims is not null)
        {
            accessClaims = AccessClaims.Select(c => new AccessClaimModel(c)).ToList();
        }
    }

    private void Delete()
    {
        accessClaims = accessClaims.Except(selectedClaims).ToList();
        selectedClaims.Clear();
        Sync();
    }

    private void Add()
    {
        AccessClaimModel model = new();
        accessClaims.Add(model);
        Sync();
    }

    private void Sync()
    {
        AccessClaims = accessClaims.Select(a => a.Claim).ToList();
        _ = AccessClaimsChanged.InvokeAsync(AccessClaims);
    }

    private class AccessClaimModel
    {
        public string Id = Guid.NewGuid().ToString();
        public ClaimDto Claim { get; set; } = new ClaimDto { Type = AaasConstants.AccessClaim };
        public Access Access { get; } = new();

        public string ClientId { get; set { field = value; UpdateClaim(); } } = string.Empty;
        public string Read { get => Access.Read ?? string.Empty; set { Access.Read = value; UpdateClaim(); } }
        public string Write { get => Access.Write ?? string.Empty; set { Access.Write = value; UpdateClaim(); } }
        public bool Admin { get => Access.Admin; set { Access.Admin = value; UpdateClaim(); } }
        public AccessClaimModel() { }
        public AccessClaimModel(ClaimDto claim)
        {
            Claim = claim;
            if (!string.IsNullOrWhiteSpace(claim.Value))
            {
                string[] parts = claim.Value.Split(':');
                ClientId = parts[0];
                Access access = AccessExtensions.GenerateAccess(claim.Value);
                Access.Read = access.Read;
                Access.Write = access.Write;
                Access.Admin = access.Admin;
            }
        }

        public void UpdateClaim()
        {
            Claim.Type = AaasConstants.AccessClaim;
            Claim.Value = $"{ClientId}:{Access.GetClaimAccessValue()}";
        }
    }
}
