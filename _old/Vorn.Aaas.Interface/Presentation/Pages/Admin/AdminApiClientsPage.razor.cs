using Vorn.Aaas.ApiClients.Presentation.Contracts;

namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

public partial class AdminApiClientsPage
{

    List<ApiClient> Clients { get; set; } = [];
    string newClientId = string.Empty, newClientSecret = string.Empty, newClientHostUri = string.Empty, scopes = string.Empty;
    ApiClient? editingClient;

    protected override async Task OnInitializedAsync() => Clients = [.. await ApiClientService.GetApiClientsAsync()];

    void RowClicked(DataGridRowClickEventArgs<ApiClient> args)
    {
        editingClient = args.Item;
        newClientId = editingClient.Id;
        newClientSecret = editingClient.Secret;
        newClientHostUri = editingClient.HostUri;
        scopes = string.Join(',', editingClient.Scopes);
    }

    async Task Save()
    {
        if(editingClient is null)
        {
            var request = new CreateApiClientRequest(newClientId,
                newClientSecret,
                scopes,
                newClientHostUri);
            ApiClient? client = await ApiClientService.CreateApiClientAsync(request);
            if(client is not null)
            {
                Clients.Add(client);
            }
        }
        else
        {
            var request = new UpdateApiClientRequest(newClientId,
                newClientSecret,
                scopes,
                newClientHostUri);
            ApiClient? updated = await ApiClientService.UpdateApiClientAsync(request);
            if(updated is not null)
            {
                int idx = Clients.FindIndex(c => c.Id == updated.Id);
                if(idx >= 0)
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

    async Task Remove(ApiClient client)
    {
        await ApiClientService.DeleteApiClientAsync(client.Id);
        Clients.Remove(client);
        if(editingClient?.Id == client.Id)
        {
            CancelEdit();
        }
        StateHasChanged();
    }

    void CancelEdit()
    {
        editingClient = null;
        newClientId = string.Empty;
        newClientSecret = string.Empty;
        newClientHostUri = string.Empty;
        scopes = string.Empty;
    }

}
