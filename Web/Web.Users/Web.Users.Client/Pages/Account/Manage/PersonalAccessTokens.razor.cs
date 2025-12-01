namespace Vitarish.Web.Users.Client.Pages.Account.Manage;

public partial class PersonalAccessTokens
{
    private readonly PersonalAccessTokenModel model = new();
    private readonly List<string> scopes = new();
    private HashSet<string> selectedScopes = new(StringComparer.Ordinal);
    private MudForm? form;
    private string? token;
    private bool isGenerating;

    private IEnumerable<string?> SelectedScopeValues => selectedScopes;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        IEnumerable<ApiClient> clients = await ApiClientService.GetApiClientsAsync();
        scopes.Clear();
        scopes.AddRange(clients
            .Select(static c => c.Scopes)
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static s => s));
    }

    private async Task GenerateTokenAsync()
    {
        if (form is not null)
        {
            await form.Validate();
            if (!form.IsValid)
            {
                return;
            }
        }

        if (!selectedScopes.Any())
        {
            Snackbar.Add(Localizer["SelectScopesFirst"], Severity.Warning);
            return;
        }
            Snackbar.Add(Localizer["TokenCreationFailed"], Severity.Error);

        string? subjectId = RequestIdentityContext.UserId;
        if (string.IsNullOrEmpty(subjectId))
        {
            Snackbar.Add(Localizer["TokenCreationFailed"], Severity.Error);
            return;
        }

        isGenerating = true;
        token = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            token = await PersonalAccessTokenService.CreateTokenAsync(subjectId, model.LifetimeDays, selectedScopes, model.IsReferenceToken);
            Snackbar.Add(Localizer["TokenCreated"], Severity.Success);
        }
        catch
        {
            Snackbar.Add(Localizer["TokenCreationFailed"], Severity.Error);
        }
        finally
        {
            isGenerating = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task OnSelectedScopesChanged(IEnumerable<string?>? values)
    {
        selectedScopes = values?
            .Where(static scope => !string.IsNullOrWhiteSpace(scope))
            .Select(static scope => scope!)
            .ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);
        return Task.CompletedTask;
    }
}
