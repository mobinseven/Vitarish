using System.Diagnostics;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Options;
using Microsoft.AspNetCore.Components.Routing;
using NavigationContext = Microsoft.AspNetCore.Components.Routing.NavigationContext;

namespace Vorn.Aaas.Interface.Presentation.Components
{
    public partial class Routes
    {
        [Inject] private ILogger<Routes> Logger { get; set; } = default!;
        [Inject] private IRequestIdentityContextWriter Writer { get; set; } = default!;
        [Inject] private IOptions<VornOptions> Options { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

        private async Task OnNavigating(NavigationContext context)
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
        }
    }
}
