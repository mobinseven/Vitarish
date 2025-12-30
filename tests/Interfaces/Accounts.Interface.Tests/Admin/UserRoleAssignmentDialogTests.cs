using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Bunit.Rendering;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using System.Collections;
using System.Linq;
using Vitarish.Accounts.Interface.Components.Admin;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Parties.Domain.Entities;

namespace Accounts.Interface.Tests.Admin;

public class UserRoleAssignmentDialogTests
{
    [Fact]
    public async Task UserRoleAssignmentDialog_SubmitValid_Saves()
    {
        await using InterfaceTestContext context = new();
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
        Mock<IMudDialogInstance> dialog = new();
        DialogResult? result = null;
        dialog.Setup(instance => instance.Close(It.IsAny<DialogResult>()))
            .Callback<DialogResult>(data => result = data);

        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([tenant]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetOwnerAsync(owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        _ = context.Mocks.ApiClients
            .Setup(service => service.GetApiClientAsync(client.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        IRenderedComponent<UserRoleAssignmentDialog> cut = RenderWithProviders(
            context,
            dialog.Object,
            "alice",
            new List<string> { $"{tenantId}:Admin" },
            new List<string>());

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "LoadTenantRolesAsync", typeof(UserRoleAssignmentDialog)));

        object? rolesField = ComponentTestHelper.GetFieldValue<object>(cut.Instance, "tenantRoles", typeof(UserRoleAssignmentDialog));
        List<object> roles = ((IEnumerable)rolesField!).Cast<object>().ToList();
        object selectedRole = Assert.Single(roles);

        ComponentTestHelper.SetField(cut.Instance, "selectedRole", selectedRole, typeof(UserRoleAssignmentDialog));

        ComponentTestHelper.Invoke(cut.Instance, "Assign", typeof(UserRoleAssignmentDialog));

        Assert.NotNull(result);
        Assert.Equal($"{tenantId}:Admin", result?.Data);
    }

    [Fact]
    public async Task UserRoleAssignmentDialog_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();
        Mock<IMudDialogInstance> dialog = new();

        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        IRenderedComponent<UserRoleAssignmentDialog> cut = RenderWithProviders(
            context,
            dialog.Object,
            "alice",
            new List<string>(),
            new List<string>());

        ComponentTestHelper.Invoke(cut.Instance, "Assign", typeof(UserRoleAssignmentDialog));

        dialog.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Never);
    }

    private static IRenderedComponent<UserRoleAssignmentDialog> RenderWithProviders(
        InterfaceTestContext context,
        IMudDialogInstance dialogInstance,
        string userName,
        List<string> availableRoles,
        List<string> assignedRoles)
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
                childBuilder.OpenComponent<UserRoleAssignmentDialog>(5);
                childBuilder.AddAttribute(6, nameof(UserRoleAssignmentDialog.UserName), userName);
                childBuilder.AddAttribute(7, nameof(UserRoleAssignmentDialog.AvailableRoles), availableRoles);
                childBuilder.AddAttribute(8, nameof(UserRoleAssignmentDialog.AssignedRoles), assignedRoles);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        return renderResult.FindComponent<UserRoleAssignmentDialog>();
    }
}
