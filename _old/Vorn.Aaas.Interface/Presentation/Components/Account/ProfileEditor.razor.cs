using System.Diagnostics;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Options;
using Vorn.Aaas.Users.Application.Commands;

namespace Vorn.Aaas.Interface.Presentation.Components.Account;

public partial class ProfileEditor
{

    [Parameter] public string Username { get; set; } = default!;
    [Parameter] public string? ReturnUrl { get; set; }
    User User = default!;
    [Inject] private IRequestIdentityContextWriter Writer { get; set; } = default!;
    [Inject] private IOptions<VornOptions> Options { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

    protected override async Task OnInitializedAsync()
    {
        VornOptions v = Options.Value;

        string authority = v.Aaas.Authority ?? throw new InvalidOperationException("Aaas Authority missing.");
        string clientId = v.Aaas.ClientId ?? throw new InvalidOperationException("Aaas client id missing.");

        string ownerId = clientId;
        string tenantId = $"{clientId}.{ownerId}";

        string? userId = null;
        if (AuthStateTask is not null)
        {
            ClaimsPrincipal user = (await AuthStateTask).User;
            userId = user.FindFirstValue("sub");
        }

        string resourceId = Nav.Uri;

        string? traceId = Activity.Current?.TraceId.ToHexString();

        string requestId = Guid.NewGuid().ToString();

        RequestIdentityContextCommand command = new(
            authority,
            clientId,
            ownerId,
            tenantId,
            userId,
            resourceId,
            traceId,
            requestId,
            null);

        Writer.Write(command);
        User = (await UserService.GetUserAsync(userId))!;
    }
    async void UpdateUser()
    {
        await UserService.UpdateUserAsync(new UpdateUserCommand(User));
        if(ReturnUrl is null)
        {
            Snackbar.Add(Localizer["ProfileUpdated"]);
        }
        else
        {
            RedirectManager.RedirectTo(WebUtility.UrlDecode(ReturnUrl));
        }
    }

}
