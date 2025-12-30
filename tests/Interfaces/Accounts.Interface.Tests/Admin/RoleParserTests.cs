using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Accounts.Interface.Components.Admin;
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

        List<ParsedTenantRole> parsed = RoleParser.ParseTenantRoles(
            [$"{tenantId}:Admin"],
            new Dictionary<Guid, Tenant> { [tenantId] = tenant },
            new Dictionary<Guid, Owner?> { [owner.Id] = owner },
            new Dictionary<string, ApiClient?> { [client.Id] = client });

        ParsedTenantRole result = Assert.Single(parsed);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("Tenant", result.TenantName);
        Assert.Equal("Owner", result.OwnerName);
        Assert.Equal("client-1", result.ClientId);
        Assert.Equal("Admin", result.RoleName);
    }

    [Fact]
    public void RoleParser_RejectsInvalidRoleString()
    {
        List<ParsedTenantRole> parsed = RoleParser.ParseTenantRoles(
            ["invalid", "missing:role", "not-a-guid:Admin"],
            new Dictionary<Guid, Tenant>(),
            new Dictionary<Guid, Owner?>(),
            new Dictionary<string, ApiClient?>());

        Assert.Empty(parsed);
    }
}
