namespace Vorn.Aaas.Interface.Presentation.Components.Admin;

using Vorn.Aaas.Users;
using Vorn.Aaas.Client.Extensions;
using Vorn.Aaas.Client.Contracts;

public partial class AccessClaimsEdit : CommonBase
{
    MudDataGrid<AccessClaimModel> dataGrid;

    [Parameter] public List<ClaimDto>? AccessClaims { get; set; }
    [Parameter] public EventCallback<List<ClaimDto>> AccessClaimsChanged { get; set; }

    List<AccessClaimModel> accessClaims = [];
    HashSet<AccessClaimModel> selectedClaims = [];

    protected override void OnParametersSet()
    {
        if(AccessClaims is not null)
        {
            accessClaims = AccessClaims.Select(c => new AccessClaimModel(c)).ToList();
        }
    }

    void Delete()
    {
        accessClaims = accessClaims.Except(selectedClaims).ToList();
        selectedClaims.Clear();
        Sync();
    }

    void Add()
    {
        AccessClaimModel model = new();
        accessClaims.Add(model);
        Sync();
    }

    void Sync()
    {
        AccessClaims = accessClaims.Select(a => a.Claim).ToList();
        AccessClaimsChanged.InvokeAsync(AccessClaims);
    }

    class AccessClaimModel
    {
        public string Id = Guid.NewGuid().ToString();
        public ClaimDto Claim { get; set; } = new ClaimDto { Type = AaasConstants.AccessClaim };
        private string clientId = string.Empty;
        public Access Access { get; } = new();

        public string ClientId { get => clientId; set { clientId = value; UpdateClaim(); } }
        public string Read { get => Access.Read ?? string.Empty; set { Access.Read = value; UpdateClaim(); } }
        public string Write { get => Access.Write ?? string.Empty; set { Access.Write = value; UpdateClaim(); } }
        public bool Admin { get => Access.Admin; set { Access.Admin = value; UpdateClaim(); } }
        public AccessClaimModel() { }
        public AccessClaimModel(ClaimDto claim)
        {
            Claim = claim;
            if(!string.IsNullOrWhiteSpace(claim.Value))
            {
                string[] parts = claim.Value.Split(':');
                ClientId = parts[0];
                var access = AccessExtensions.GenerateAccess(claim.Value);
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
