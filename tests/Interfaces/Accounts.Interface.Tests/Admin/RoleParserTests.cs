using System.Collections;
using System.Reflection;
using System.Linq;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Parties.Domain.Entities;

namespace Accounts.Interface.Tests.Admin;

public class RoleParserTests
{
    [Fact]
    public void RoleParser_ParsesValidRoleString()
    {
        Guid tenantId = Guid.NewGuid();
        Owner owner = new() { Id = Guid.NewGuid(), Name = "Owner" };
        Tenant tenant = new() { Id = tenantId, Name = "Tenant", OwnerId = owner.Id, ClientId = "client-1" };
        ApiClient client = new()
        {
            Id = "client-1",
            Secret = "secret",
            Scopes = "scope.read",
            HostUri = "https://client.example.com"
        };

        IList parsed = ParseTenantRoles(
            [$"{tenantId}:Admin"],
            new Dictionary<Guid, Tenant> { [tenantId] = tenant },
            new Dictionary<Guid, Owner?> { [owner.Id] = owner },
            new Dictionary<string, ApiClient?> { [client.Id] = client });

        object result = Assert.Single(parsed.Cast<object>());
        Type resultType = result.GetType();
        Assert.Equal(tenantId, resultType.GetProperty("TenantId")?.GetValue(result));
        Assert.Equal("Tenant", resultType.GetProperty("TenantName")?.GetValue(result));
        Assert.Equal("Owner", resultType.GetProperty("OwnerName")?.GetValue(result));
        Assert.Equal("client-1", resultType.GetProperty("ClientId")?.GetValue(result));
        Assert.Equal("Admin", resultType.GetProperty("RoleName")?.GetValue(result));
    }

    [Fact]
    public void RoleParser_RejectsInvalidRoleString()
    {
        IList parsed = ParseTenantRoles(
            ["invalid", "missing:role", "not-a-guid:Admin"],
            new Dictionary<Guid, Tenant>(),
            new Dictionary<Guid, Owner?>(),
            new Dictionary<string, ApiClient?>());

        Assert.Empty(parsed);
    }

    private static IList ParseTenantRoles(
        IEnumerable<string> roles,
        Dictionary<Guid, Tenant> tenantDict,
        Dictionary<Guid, Owner?> owners,
        Dictionary<string, ApiClient?> clients)
    {
        Type roleParserType = Type.GetType("Vitarish.Accounts.Interface.Components.Admin.RoleParser, Vitarish.Accounts.Interface")
            ?? throw new InvalidOperationException("RoleParser type not found.");
        MethodInfo parseMethod = roleParserType.GetMethod("ParseTenantRoles", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseTenantRoles method not found.");

        object? result = parseMethod.Invoke(null, [roles, tenantDict, owners, clients]);
        return Assert.IsAssignableFrom<IList>(result);
    }
}
