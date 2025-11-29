
namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

public partial class AdminAuthenticationClientsPage
{
    List<AuthenticationClient> Clients { get; set; } = [];
    string newClientId = string.Empty,
           newClientUrl = string.Empty,
           newClientTitle = string.Empty,
           newClientDescription = string.Empty,
           newClientLogoUri = string.Empty;
    bool newClientAutoAccess = false;

    protected override async Task OnInitializedAsync()
        => Clients = [.. await AuthenticationClientsService.GetAuthenticationClientsAsync()];

    void RowClicked(DataGridRowClickEventArgs<AuthenticationClient> args)
        => NavigationManager.NavigateTo($"/Admin/AuthenticationClients/{args.Item.Id}");

    async Task Save()
    {
        var request = new CreateAuthenticationClientRequest(
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

    async Task Remove(AuthenticationClient client)
    {
        var request = new DeleteAuthenticationClientRequest(client.Id);
        await AuthenticationClientsService.DeleteAuthenticationClientAsync(request);
        Clients.Remove(client);
        StateHasChanged();
    }

    void ClearInput()
    {
        newClientId = string.Empty;
        newClientUrl = string.Empty;
        newClientTitle = string.Empty;
        newClientDescription = string.Empty;
        newClientLogoUri = string.Empty;
        newClientAutoAccess = false;
    }
}
