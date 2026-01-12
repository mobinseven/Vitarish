namespace Vitarish.Main.Pages.Admins;

public partial class AdminAuthenticationClientPage
{
    [Parameter] public string? ClientId { get; set; }

    private AuthenticationClient? Client;

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            NavigationManager.NavigateTo("/Admin/AuthenticationClients");
            return;
        }

        Client = await AuthenticationClientsService.GetAuthenticationClientAsync(ClientId);
    }

    private async Task Save()
    {
        if (Client is null)
        {
            return;
        }

        var update = new AuthenticationClients.Application.Commands.UpdateAuthenticationClient(Client.Id,
                                                           Client.Url,
                                                           Client.Title,
                                                           Client.Description,
                                                           Client.LogoUrl,
                                                           Client.AutoAccess);
        await AuthenticationClientsService.UpdateAuthenticationClientAsync(update);

        _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
    }

    private async Task Delete()
    {
        if (Client is null)
        {
            return;
        }

        await AuthenticationClientsService.DeleteAuthenticationClientAsync(new AuthenticationClients.Application.Commands.DeleteAuthenticationClient(Client.Id));
        NavigationManager.NavigateTo("/Admin/AuthenticationClients");
    }
}
