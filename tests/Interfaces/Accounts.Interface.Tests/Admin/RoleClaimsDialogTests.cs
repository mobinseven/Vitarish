using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Vitarish.Accounts.Interface.Components.Admin;
using ClaimDto = Vitarish.Users.Domain.Models.ClaimDto;

namespace Accounts.Interface.Tests.Admin;

public class RoleClaimsDialogTests
{
    [Fact]
    public async Task RoleClaimsDialog_RendersClaims()
    {
        await using InterfaceTestContext context = new();
        List<ClaimDto> claims = [new("type", "value")];
        Mock<IMudDialogInstance> dialog = new();

        IRenderedComponent<RoleClaimsDialog> cut = RenderWithProviders(context, dialog.Object, "Admin", claims);

        List<ClaimDto> dialogClaims = ComponentTestHelper.GetFieldValue<List<ClaimDto>>(cut.Instance, "claims", typeof(RoleClaimsDialog)) ?? [];
        ClaimDto claim = Assert.Single(dialogClaims);
        Assert.Equal("type", claim.Type);
        Assert.Equal("value", claim.Value);
    }

    [Fact]
    public async Task RoleClaimsDialog_Save_InvokesUpdate()
    {
        await using InterfaceTestContext context = new();
        List<ClaimDto> claims = [new("type", "value")];
        Mock<IMudDialogInstance> dialog = new();
        DialogResult? result = null;
        dialog.Setup(instance => instance.Close(It.IsAny<DialogResult>()))
            .Callback<DialogResult>(data => result = data);

        IRenderedComponent<RoleClaimsDialog> cut = RenderWithProviders(context, dialog.Object, "Admin", claims);

        ComponentTestHelper.Invoke(cut.Instance, "Save", typeof(RoleClaimsDialog));

        Assert.NotNull(result);
        List<ClaimDto> saved = Assert.IsType<List<ClaimDto>>(result?.Data);
        Assert.Single(saved);
        Assert.Equal("type", saved[0].Type);
    }

    private static IRenderedComponent<RoleClaimsDialog> RenderWithProviders(
        InterfaceTestContext context,
        IMudDialogInstance dialogInstance,
        string roleName,
        List<ClaimDto> claims)
    {
        IRenderedComponent<ContainerFragment> renderResult = context.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudDialogProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<CascadingValue<IMudDialogInstance>>(2);
            builder.AddAttribute(3, "Value", dialogInstance);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<RoleClaimsDialog>(5);
                childBuilder.AddAttribute(6, nameof(RoleClaimsDialog.RoleName), roleName);
                childBuilder.AddAttribute(7, nameof(RoleClaimsDialog.Claims), claims);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        return renderResult.FindComponent<RoleClaimsDialog>();
    }
}
