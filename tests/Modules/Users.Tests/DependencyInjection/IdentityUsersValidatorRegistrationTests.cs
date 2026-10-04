using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Module.DependencyInjection;

namespace Vitarish.Users.Tests.DependencyInjection;

public sealed class IdentityUsersValidatorRegistrationTests
{
    [Fact]
    public void Users_composition_registers_application_command_validators()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddUsers(_ => { });
        builder.Services.AddSingleton(Mock.Of<ISecurityAuditWriter>());
        builder.Services.AddSingleton(Mock.Of<IRequestContext>());

        using ServiceProvider provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        IValidator<CreateUserWithEmailCommand> validator =
            scope.ServiceProvider.GetRequiredService<IValidator<CreateUserWithEmailCommand>>();

        validator.Should().BeOfType<CreateUserWithEmailCommandValidator>();
    }
}
