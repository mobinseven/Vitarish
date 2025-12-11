using Vitarish.Interface.Identity.Contracts;

namespace Vitarish.Interface.Presentation.Components.Admin;

public partial class AccessUsersMatrix : CommonBase
{
    [Parameter] public string ClientId { get; set; } = string.Empty;
    [Parameter] public IEnumerable<User>? Users { get; set; }
    [Parameter] public List<UserAccess>? AccessClaims { get; set; }
    [Parameter] public EventCallback<List<UserAccess>> AccessClaimsChanged { get; set; }

    private List<AccessUserModel>? rows;
    private List<string> roles = [];

    protected override void OnParametersSet()
    {
        if (Users is not null)
        {
            if (rows?.Count != Users.Count())
            {
                rows = Users.Select(u => new AccessUserModel(u, ClientId, Sync)).ToList();
                AccessUserModel? test = rows.FirstOrDefault();
            }
        }

        SyncFromClaims();
        UpdateRoles();
    }

    private void SyncFromClaims()
    {
        if (rows is null || AccessClaims is null)
        {
            return;
        }

        foreach (UserAccess accessClaim in AccessClaims)
        {
            AccessUserModel row = rows.Single(r => r.User.Id == accessClaim.UserId);
            row.LoadClaim(accessClaim?.Access);
        }
    }

    private void Sync()
    {
        if (rows is null)
        {
            return;
        }

        IReadOnlyList<UserAccess> newClaims = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Access.Read) ||
                        !string.IsNullOrWhiteSpace(r.Access.Write) ||
                        r.Access.Admin)
            .Select(r => new UserAccess(r.User.Id, r.Access))
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

    private static bool ClaimListsEqual(IReadOnlyList<UserAccess>? a, IReadOnlyList<UserAccess>? b)
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
            if (a[i].UserId != b[i].UserId || a[i].Access.GetClaimAccessValue() != b[i].Access.GetClaimAccessValue())
            {
                return false;
            }
        }

        return true;
    }

    public sealed record UserAccess(string UserId, Access Access);

    private sealed class AccessUserModel
    {
        private readonly Action sync;
        private readonly string clientId;
        public User User { get; }
        public Access Access { get; set; } = new();

        public AccessUserModel(User user, string clientId, Action sync)
        {
            User = user;
            this.clientId = clientId;
            this.sync = sync;
            Access.PropertyChanged += AccessPropertyChanged;
        }

        public void LoadClaim(Access? access)
        {
            if (access is null)
            {
                return;
            }

            Access.Entrance = access.Entrance;
            Access.Read = access.Read;
            Access.Write = access.Write;
            Access.Admin = access.Admin;

            UpdateClaim(false);
        }

        private void AccessPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => UpdateClaim();

        private void UpdateClaim(bool callSync = true)
        {
            if (callSync)
            {
                sync();
            }
        }
    }
}
