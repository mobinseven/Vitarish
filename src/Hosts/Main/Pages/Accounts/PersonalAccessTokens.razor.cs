namespace Vitarish.Main.Pages.Accounts;

public partial class PersonalAccessTokens
{
    private readonly PersonalAccessTokenModel model = new();
    private readonly List<string> scopes = [];
    private HashSet<string> selectedScopes = new(StringComparer.Ordinal);
    private MudForm? form;
    private string? token;
    private bool isGenerating;

    private IEnumerable<string?> SelectedScopeValues => selectedScopes;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        scopes.Clear();
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
            _ = Snackbar.Add(Localizer["SelectScopesFirst"], Severity.Warning);
            return;
        }
        _ = Snackbar.Add(Localizer["TokenCreationFailed"], Severity.Error);

        string? subjectId = RequestContext.UserId;
        if (string.IsNullOrEmpty(subjectId))
        {
            _ = Snackbar.Add(Localizer["TokenCreationFailed"], Severity.Error);
            return;
        }

        isGenerating = true;
        token = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            token = await PersonalAccessTokenService.CreateTokenAsync(subjectId, model.LifetimeDays, selectedScopes, model.IsReferenceToken);
            _ = Snackbar.Add(Localizer["TokenCreated"], Severity.Success);
        }
        catch
        {
            _ = Snackbar.Add(Localizer["TokenCreationFailed"], Severity.Error);
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
