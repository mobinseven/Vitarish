using Vitarish.Parties.Domain.Entities;
using Vitarish.Requests.Application.Models;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Modules.Base;

public class TenantService : ITenantService
{
    public Tenant? Tenant { get; private set; }
    public Metadata? Metadata { get; set; }
    public IEnumerable<UserDto> Users { get; set; } = [];

}

public interface ITenantService
{
    Metadata? Metadata { get; set; }
    IEnumerable<UserDto> Users { get; set; }
    Tenant? Tenant { get; }
}
