
using Microsoft.AspNetCore.Components;
using Vorn.Aaas.Client.Constants;
using Vorn.Aaas.Client.Contracts;
using Vorn.Aaas.Client.Extensions;

namespace Vorn.Aaas.Client.Admin;

public partial class AccessClaimsMatrix : ComponentBase
{
    [Parameter] public IEnumerable<ClientModel>? Clients { get; set; }
    [Parameter] public List<ClaimDto>? AccessClaims { get; set; }
    [Parameter] public EventCallback<List<ClaimDto>> AccessClaimsChanged { get; set; }

    private List<AccessClientModel>? rows;
    private List<string> roles = [];

    protected override void OnParametersSet()
    {
        if (Clients is null)
        {
            return;
        }

        rows ??= Clients.Select(c => new AccessClientModel(c, Sync)).ToList();
        SyncFromClaims();
        UpdateRoles();
    }

    private void SyncFromClaims()
    {
        if (rows is null || AccessClaims is null)
        {
            return;
        }

        foreach (AccessClientModel row in rows)
        {
            ClaimDto? claim = AccessClaims.FirstOrDefault(c => c.Value?.StartsWith($"{row.Client.Id}:", StringComparison.InvariantCultureIgnoreCase) == true);
            row.LoadClaim(claim);
        }
    }

    private void Sync()
    {
        if (rows is null)
        {
            return;
        }

        IReadOnlyList<ClaimDto> newClaims = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Access.Read) ||
                        !string.IsNullOrWhiteSpace(r.Access.Write) ||
                        r.Access.Admin)
            .Select(r => r.Claim)
            .ToList();

        if (!ClaimListsEqual(AccessClaims, newClaims))
        {
            _ = AccessClaimsChanged.InvokeAsync(newClaims.ToList());
        }

        UpdateRoles();
    }

    private void UpdateRoles()
    {
        if (rows is null)
        {
            roles = [];
            return;
        }

        roles = rows
            .SelectMany(r => r.Access.Readers.Concat(r.Access.Writers))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();
    }

    private static bool ClaimListsEqual(IReadOnlyList<ClaimDto>? a, IReadOnlyList<ClaimDto>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].Type != b[i].Type || a[i].Value != b[i].Value)
            {
                return false;
            }
        }

        return true;
    }

    public sealed record ClientModel(string Id, string? Title);

    private sealed class AccessClientModel
    {
        private readonly Action sync;
        public ClientModel Client { get; }
        public ClaimDto Claim { get; private set; }
        public Access Access { get; } = new();

        public string? Read
        {
            get => Access.Read;
            set
            {
                if (Access.Read != value)
                {
                    Access.Read = value;
                    UpdateClaim();
                }
            }
        }

        public string? Write
        {
            get => Access.Write;
            set
            {
                if (Access.Write != value)
                {
                    Access.Write = value;
                    UpdateClaim();
                }
            }
        }

        public bool Admin
        {
            get => Access.Admin;
            set
            {
                if (Access.Admin != value)
                {
                    Access.Admin = value;
                    UpdateClaim();
                }
            }
        }

        public AccessClientModel(ClientModel client, Action sync)
        {
            Client = client;
            this.sync = sync;
            Claim = new ClaimDto { Type = AaasConstants.AccessClaim };
        }

        public void LoadClaim(ClaimDto? claim)
        {
            if (claim is null)
            {
                Access.Read = null;
                Access.Write = null;
                Access.Admin = false;
                UpdateClaim(false);
                return;
            }

            Claim = claim;
            Access access = AccessExtensions.GenerateAccess(claim.Value!);
            Access.Read = access.Read;
            Access.Write = access.Write;
            Access.Admin = access.Admin;

            UpdateClaim(false);
        }

        private void UpdateClaim(bool callSync = true)
        {
            Claim.Type = AaasConstants.AccessClaim;
            Claim.Value = $"{Client.Id}:{Access.GetClaimAccessValue()}";

            if (callSync)
            {
                sync();
            }
        }
    }
}
