using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Main.Mock.Authentication;
using Vitarish.Main.Mock.Components;
using Vitarish.Main.Mock.Services;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

_ = builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Pages";
});

_ = builder.Services.AddAuthorization();

builder.Services
    .AddIdentityCore<User>()
    .AddSignInManager()
    .AddUserStore<PreviewUserRoleStore>()
    .AddRoles<IdentityRole>()
    .AddRoleStore<PreviewRoleStore>();

builder.AddAccountsInterface();
builder.Services.AddVitarishInterfacesBase();
builder.Services.AddScoped<AuthenticationStateProvider, PreviewAuthenticationStateProvider>();
builder.Services.AddSingleton<Vitarish.Users.Application.Abstractions.IUserProfileService, PreviewUserProfileService>();
builder.Services.AddScoped<IRequestContext, MockRequestContext>();
builder.Services.AddScoped<IRequestContextWriter>(sp => (IRequestContextWriter)sp.GetRequiredService<IRequestContext>());
builder.Services.AddSingleton<IMessageQueue, MockMessageQueue>();
builder.Services.AddSingleton<IPersonalAccessTokenGenerator, MockPersonalAccessTokenGenerator>();
builder.Services.AddSingleton<IIdentityServerInteractionService, MockIdentityServerInteractionService>();
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
