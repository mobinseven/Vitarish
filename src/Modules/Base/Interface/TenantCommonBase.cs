using Microsoft.AspNetCore.Components;
using Vitarish.Modules.Base.Core;
using Vitarish.Modules.Base.Interface.Common;

namespace Vitarish.Modules.Base.Interface;

public class TenantCommonBase : CommonBase, IDisposable
{
    [Inject] protected ITenantService TenantService { get; set; } = default!;
    protected bool AdminMode => TenantService.IsTenantOwner;
    protected IEnumerable<TenantUserSummary> GetUsers() => TenantService.Users ?? [];
    protected string? GetUserFullNameById(Guid id) => TenantService.Users?.FirstOrDefault(x => x.Id == id.ToString())?.FullName ?? id.ToString();
    protected string? GetUserFullNameById(string? id) => TenantService.Users?.FirstOrDefault(x => x.Id == id)?.FullName ?? id;
}
