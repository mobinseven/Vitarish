using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Base.Infrastructure.Pipeline;

namespace Base.Tests.Infrastructure.Pipeline;

public class LoggingBehaviorTests
{
    public sealed record TestRequest(string Value) : IRequest<string>;

    private readonly LoggingBehavior<TestRequest, string> _behavior;

    public LoggingBehaviorTests()
    {
        var logger = NullLogger<LoggingBehavior<TestRequest, string>>.Instance;
        _behavior = new LoggingBehavior<TestRequest, string>(logger);
    }

    [Fact]
    public async Task Handle_ReturnsExpectedResponse()
    {
        // Arrange
        var request = new TestRequest("test");
        var response = "response";

        Task<string> Next() => Task.FromResult(response);

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_RethrowsException()
    {
        // Arrange
        var request = new TestRequest("test");
        var exception = new InvalidOperationException("Test exception");

        Task<string> Next() => throw exception;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.Handle(request, Next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CallsNextDelegate()
    {
        // Arrange
        var request = new TestRequest("test");
        var nextCalled = false;

        Task<string> Next()
        {
            nextCalled = true;
            return Task.FromResult("response");
        }

        // Act
        await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }
}
