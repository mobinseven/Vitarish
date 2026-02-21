using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Modules.Base;

public class TenantCommonBase : CommonBase, IDisposable
{
    [Inject] protected ITenantService TenantService { get; set; } = default!;
    [CascadingParameter] public bool IsAdmin { get; set; } = false;
    protected bool AdminMode => IsAdmin;
    protected IEnumerable<UserDto> GetUsers() => TenantService.Users ?? [];
    protected string? GetUserFullNameById(Guid id) => TenantService.Users?.FirstOrDefault(x => x.Id == id.ToString())?.FullName ?? id.ToString();
    protected string? GetUserFullNameById(string id) => TenantService.Users?.FirstOrDefault(x => x.Id == id)?.FullName ?? id;
}
