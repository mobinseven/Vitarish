using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Vitarish.Accounts.Interface.Components.Admin;
using ClaimDto = Vitarish.Users.Domain.Models.ClaimDto;

namespace Accounts.Interface.Tests.Admin;

public class ClaimsEditTests
{
    [Fact]
    public async Task ClaimsEdit_AddClaim_UpdatesList()
    {
        await using InterfaceTestContext context = new();
        List<ClaimDto> claims = [new() { Type = "role", Value = "admin" }];

        IRenderedComponent<ClaimsEdit> cut = context.Render<ClaimsEdit>(parameters => parameters
            .Add(p => p.UserClaims, claims));

        ComponentTestHelper.Invoke(cut.Instance, "Add", typeof(ClaimsEdit));

        Assert.Equal(2, claims.Count);
    }

    [Fact]
    public async Task ClaimsEdit_RemoveClaim_UpdatesList()
    {
        await using InterfaceTestContext context = new();
        List<ClaimDto> claims =
        [
            new() { Type = "role", Value = "admin" },
            new() { Type = "role", Value = "user" }
        ];
        List<ClaimDto>? updated = null;

        IRenderedComponent<ClaimsEdit> cut = context.Render<ClaimsEdit>(parameters => parameters
            .Add(p => p.UserClaims, claims)
            .Add(p => p.UserClaimsChanged, EventCallback.Factory.Create<List<ClaimDto>>(this, list => updated = list)));

        HashSet<ClaimDto> selected = [claims[0]];
        ComponentTestHelper.SetField(cut.Instance, "selectedClaims", selected, typeof(ClaimsEdit));

        ComponentTestHelper.Invoke(cut.Instance, "Delete", typeof(ClaimsEdit));

        Assert.Single(cut.Instance.UserClaims ?? []);
        Assert.Single(updated ?? []);
        Assert.Equal("user", updated?.Single().Value);
    }
}
