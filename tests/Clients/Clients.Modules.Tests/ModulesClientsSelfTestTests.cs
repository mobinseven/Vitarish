using Microsoft.Extensions.Logging;

namespace Vitarish.Clients.Modules.Tests;

public class ModulesClientsSelfTestTests
{
    [Fact]
    public void AddModulesClients_RegistersSelfTestHostedService()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IIdentityClientSignalRFactory>(Mock.Of<IIdentityClientSignalRFactory>());
        services.AddSingleton<IHubConnectionStarter>(Mock.Of<IHubConnectionStarter>());

        // Act
        services.AddModulesClients();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IModulesClientsSelfTest>().Should().NotBeNull();
        provider.GetServices<IHostedService>()
            .OfType<ModulesClientsSelfTest>()
            .Should()
            .HaveCount(1);
    }

    [Fact]
    public async Task RunAsync_WhenAllProbesSucceed_StartsEachConnection()
    {
        // Arrange
        Mock<IIdentityClientSignalRFactory> factory = new();
        Mock<IHubConnectionStarter> starter = new();
        starter.Setup(s => s.StartAsync(It.IsAny<HubConnection>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        starter.Setup(s => s.DisposeAsync(It.IsAny<HubConnection>()))
            .Returns(Task.CompletedTask);

        factory.Setup(f => f.CreateConnection<MessageClientDefinition>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(static () => CreateConnection());
        factory.Setup(f => f.CreateConnection<FilesModuleClientDefinition>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(static () => CreateConnection());
        factory.Setup(f => f.CreateConnection<ResponderClientDefinition>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(static () => CreateConnection());
        factory.Setup(f => f.CreateConnection<TranscriberClientDefinition>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(static () => CreateConnection());

        ModulesClientsSelfTest selfTest = new(factory.Object, starter.Object, NullLogger<ModulesClientsSelfTest>.Instance);

        // Act
        await selfTest.RunAsync();

        // Assert
        starter.Verify(s => s.StartAsync(It.IsAny<HubConnection>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
        starter.Verify(s => s.DisposeAsync(It.IsAny<HubConnection>()), Times.Exactly(4));
    }

    [Fact]
    public async Task RunAsync_WhenProbeFails_ThrowsMeaningfulException()
    {
        // Arrange
        Mock<IIdentityClientSignalRFactory> factory = new();
        Mock<IHubConnectionStarter> starter = new();
        starter.Setup(s => s.StartAsync(It.IsAny<HubConnection>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("probe failed"));
        starter.Setup(s => s.DisposeAsync(It.IsAny<HubConnection>()))
            .Returns(Task.CompletedTask);

        factory.Setup(f => f.CreateConnection<MessageClientDefinition>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(static () => CreateConnection());
        factory.Setup(f => f.CreateConnection<FilesModuleClientDefinition>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(static () => CreateConnection());

        ModulesClientsSelfTest selfTest = new(factory.Object, starter.Object, NullLogger<ModulesClientsSelfTest>.Instance);

        // Act
        Func<Task> act = async () => await selfTest.RunAsync();

        // Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("Communications");
    }

    private static HubConnection CreateConnection() =>
        new HubConnectionBuilder().WithUrl("http://localhost").Build();
}
