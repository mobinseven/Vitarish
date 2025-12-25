using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vitarish.Clients.Mock.Identity.DependencyInjection;
using Vitarish.Main.Mock.Authentication;
using Vitarish.Main.Mock.Components;
using Vitarish.Users.Domain.Entities;

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
    .AddUserStore<PreviewUserStore>();

builder.AddAccountsInterface();

builder.Services.AddScoped<AuthenticationStateProvider, PreviewAuthenticationStateProvider>();
builder.Services.AddSingleton<Vitarish.Users.Application.Abstractions.IUserProfileService, PreviewUserProfileService>();

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
