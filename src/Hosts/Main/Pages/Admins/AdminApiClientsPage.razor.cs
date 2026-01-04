using Vitarish.ApiClients.Application.Commands;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminApiClientsPage
{

    private List<ApiClient> Clients { get; set; } = [];
    private string newClientId = string.Empty, newClientSecret = string.Empty, newClientHostUri = string.Empty, scopes = string.Empty;
    private ApiClient? editingClient;

    protected override async Task OnInitializedAsync() => Clients = [.. await ApiClientService.GetApiClientsAsync()];

    private void RowClicked(DataGridRowClickEventArgs<ApiClient> args)
    {
        editingClient = args.Item;
        newClientId = editingClient.Id;
        newClientSecret = editingClient.Secret;
        newClientHostUri = editingClient.HostUri;
        scopes = string.Join(',', editingClient.Scopes);
    }

    private async Task Save()
    {
        if (editingClient is null)
        {
            CreateApiClient request = new(newClientId,
                newClientSecret,
                scopes,
                newClientHostUri);
            ApiClient? client = await ApiClientService.CreateApiClientAsync(request);
            if (client is not null)
            {
                Clients.Add(client);
            }
        }
        else
        {
            UpdateApiClient request = new(newClientId,
                newClientSecret,
                scopes,
                newClientHostUri);
            ApiClient? updated = await ApiClientService.UpdateApiClientAsync(request);
            if (updated is not null)
            {
                int idx = Clients.FindIndex(c => c.Id == updated.Id);
                if (idx >= 0)
                {
                    Clients[idx] = updated;
                }
            }
            editingClient = null;
        }

        newClientId = string.Empty;
        newClientSecret = string.Empty;
        newClientHostUri = string.Empty;
        scopes = string.Empty;
        StateHasChanged();
    }

    private async Task Remove(ApiClient client)
    {
        await ApiClientService.DeleteApiClientAsync(client.Id);
        _ = Clients.Remove(client);
        if (editingClient?.Id == client.Id)
        {
            CancelEdit();
        }
        StateHasChanged();
    }

    private void CancelEdit()
    {
        editingClient = null;
        newClientId = string.Empty;
        newClientSecret = string.Empty;
        newClientHostUri = string.Empty;
        scopes = string.Empty;
    }

}
