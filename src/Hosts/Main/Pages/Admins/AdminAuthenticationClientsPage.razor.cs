using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Services;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminAuthenticationClientsPage
{
    private List<AuthenticationClient> Clients { get; set; } = [];
    private string newClientId = string.Empty,
           newClientUrl = string.Empty,
           newClientTitle = string.Empty,
           newClientDescription = string.Empty,
           newClientLogoUri = string.Empty;
    private bool newClientAutoAccess = false;

    protected override async Task OnInitializedAsync()
        => Clients = [.. await AuthenticationClientsService.GetAuthenticationClientsAsync()];

    private void RowClicked(DataGridRowClickEventArgs<AuthenticationClient> args)
        => NavigationManager.NavigateTo($"/Admin/AuthenticationClients/{args.Item.Id}");

    private async Task Save()
    {
        CreateAuthenticationClient request = new(
            newClientId,
            newClientUrl,
            newClientTitle,
            newClientDescription,
            newClientLogoUri,
            newClientAutoAccess);
        AuthenticationClient? client = await AuthenticationClientsService.CreateAuthenticationClientAsync(request);
        if (client is not null)
        {
            Clients.Add(client);
            ClearInput();
            StateHasChanged();
        }
    }

    private async Task Remove(AuthenticationClient client)
    {
        DeleteAuthenticationClient request = new(client.Id);
        await AuthenticationClientsService.DeleteAuthenticationClientAsync(request);
        _ = Clients.Remove(client);
        StateHasChanged();
    }

    private void ClearInput()
    {
        newClientId = string.Empty;
        newClientUrl = string.Empty;
        newClientTitle = string.Empty;
        newClientDescription = string.Empty;
        newClientLogoUri = string.Empty;
        newClientAutoAccess = false;
    }
}
