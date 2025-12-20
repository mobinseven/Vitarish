using Vitarish.Accounts.Domain.Contracts;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure;

namespace Vitarish.Accounts.Interface.Components.Admin;
/// <summary>
/// Displays the access‑matrix for all authentication clients and emits a minimal list of
/// <see cref="ClaimDto"/> objects representing the user edits.
///
/// **Important:** To prevent the render‑loop that caused the stack‑overflow in the original
/// implementation, this component never mutates the incoming <see cref="AccessClaims"/>
/// parameter directly – it only raises <see cref="AccessClaimsChanged"/>.
/// </summary>
public partial class AccessClaimsMatrix : CommonBase
{
    // ────────────────────────────────────────────────────────────────────────────────
    // Parameters
    // ────────────────────────────────────────────────────────────────────────────────
    [Parameter] public List<ClaimDto>? AccessClaims { get; set; }

    /// <summary>
    /// Two‑way binding endpoint.  DO NOT change <see cref="AccessClaims"/> directly in this
    /// component – doing so would trigger an endless cascade of <see cref="OnParametersSet"/>
    /// calls and blow the stack.
    /// </summary>
    [Parameter] public EventCallback<List<ClaimDto>> AccessClaimsChanged { get; set; }

    // ────────────────────────────────────────────────────────────────────────────────
    // Private state
    // ────────────────────────────────────────────────────────────────────────────────
    private List<AccessClientModel>? rows;
    private List<string> roles = [];
    private IEnumerable<AuthenticationClient> clients = [];
    // ────────────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ────────────────────────────────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        clients = await AuthenticationClientsService.GetAuthenticationClientsAsync();

        rows = clients.Select(c => new AccessClientModel(c, Sync)).ToList();

        if (AccessClaims is null)
        {
            return;
        }

        foreach (AccessClientModel row in rows)
        {
            ClaimDto? claim = AccessClaims.FirstOrDefault(c => c.Value?.StartsWith($"{row.Client.Id}:", StringComparison.InvariantCultureIgnoreCase) == true);
            row.LoadClaim(claim);
        }

        UpdateRoles();
    }

    private void Sync()
    {
        if (rows is null)
        {
            return;
        }

        IReadOnlyList<ClaimDto> newClaims = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Read) ||
                        !string.IsNullOrWhiteSpace(r.Write) ||
                        r.Admin)
            .Select(r => r.Claim)
            .ToList();

        _ = AccessClaimsChanged.InvokeAsync(newClaims.ToList());

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

    // ────────────────────────────────────────────────────────────────────────────────
    // Row model
    // ────────────────────────────────────────────────────────────────────────────────
    private sealed class AccessClientModel
    {
        private readonly Action _sync;
        public AuthenticationClient Client { get; }
        public ClaimDto Claim { get; private set; }
        public Access Access { get; } = new();

        public string Read
        {
            get => Access.Read ?? string.Empty;
            set
            {
                if (Access.Read != value)
                {
                    Access.Read = value;
                    UpdateClaim();
                }
            }
        }

        public string Write
        {
            get => Access.Write ?? string.Empty;
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

        public AccessClientModel(AuthenticationClient client, Action sync)
        {
            Client = client;
            _sync = sync;
            Claim = new ClaimDto { Type = IdentityAccessConstants.AccessClaim };
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
            Claim.Type = IdentityAccessConstants.AccessClaim;
            Claim.Value = $"{Client.Id}:{Access.GetClaimAccessValue()}";

            if (callSync)
            {
                _sync();
            }
        }

        public override string ToString() => Claim.Value ?? string.Empty;
    }
}

