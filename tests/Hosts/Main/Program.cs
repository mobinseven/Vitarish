using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.Clients.Mock.Identity.DependencyInjection;
using Vitarish.Main.Mock.Authentication;
using Vitarish.Main.Mock.Components;
using Vitarish.Main.Mock.Services;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Parties.Application.Abstractions;
using Duende.IdentityServer.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

_ = builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Pages";
});

_ = builder.Services.AddIdentityComponentsPreview();

_ = builder.Services.AddAuthorization();

builder.Services
    .AddIdentityCore<User>()
    .AddSignInManager()
    .AddUserStore<PreviewUserStore>()
    .AddRoles<IdentityRole>()
    .AddRoleStore<PreviewRoleStore>();

builder.AddAccountsInterface();

builder.Services.AddScoped<AuthenticationStateProvider, PreviewAuthenticationStateProvider>();
builder.Services.AddSingleton<Vitarish.Users.Application.Abstractions.IUserProfileService, PreviewUserProfileService>();
builder.Services.AddScoped<IRequestContext, MockRequestContext>();
builder.Services.AddScoped<IRequestContextWriter>(sp => (IRequestContextWriter)sp.GetRequiredService<IRequestContext>());
builder.Services.AddSingleton<IMessageQueue, MockMessageQueue>();
builder.Services.AddSingleton<IPersonalAccessTokenGenerator, MockPersonalAccessTokenGenerator>();
builder.Services.AddSingleton<IIdentityServerInteractionService, MockIdentityServerInteractionService>();
builder.Services.AddSingleton<IApiClientsService, MockApiClientsService>();
builder.Services.AddSingleton<IAuthenticationClientsService, MockAuthenticationClientsService>();
builder.Services.AddSingleton<IRequestIdentitiesService, MockRequestIdentitiesService>();
builder.Services.AddSingleton<IPartiesService, MockPartiesService>();
builder.Services.AddSingleton<IUsersService, MockUsersService>();

WebApplication app = builder.Build();

_ = app.UseRequestLocalization();

_ = app.UseStaticFiles();
_ = app.UseRouting();

_ = app.UseAuthentication();
_ = app.UseAuthorization();

_ = app.UseAntiforgery();

_ = app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
_ = app.MapRazorPages();

app.MapAccountsInterface();

app.Run();
