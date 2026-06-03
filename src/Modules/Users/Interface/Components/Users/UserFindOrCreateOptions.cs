namespace Vitarish.Users.Interface.Components.Users;

public sealed class UserFindOrCreateOptions
{
    public bool AllowCreate { get; set; } = true;
    public bool AllowUnlink { get; set; } = true;
    public bool ShowSearchScopeSelector { get; set; } = true;
    public UserSearchScope DefaultSearchScope { get; set; } = UserSearchScope.TenantUsers;
    public bool AutoLinkExistingUser { get; set; } = true;
    public bool AllowAutoLinkToggle { get; set; } = true;
    public bool EnsureTenantUserRoleOnLink { get; set; } = true;
}
