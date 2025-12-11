using Microsoft.AspNetCore.Components;

namespace Accounts.Interface.Tests.TestInfrastructure;

internal sealed class RecordingNavigationManager : NavigationManager
{
    private readonly List<(string Uri, bool ForceLoad)> history = [];

    public IReadOnlyList<(string Uri, bool ForceLoad)> History => history;

    public RecordingNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Uri absoluteUri = ToAbsoluteUri(uri);
        history.Add((absoluteUri.ToString(), forceLoad));
        Uri = absoluteUri.ToString();
    }
}
