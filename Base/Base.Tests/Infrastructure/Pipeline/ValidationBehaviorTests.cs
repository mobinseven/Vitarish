using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Base.Infrastructure.Pipeline;

namespace Base.Tests.Infrastructure.Pipeline;

public class ValidationBehaviorTests
{
    public sealed record TestRequest(string Value) : IRequest<string>;

    public sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required");
        }
    }

    [Fact]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var logger = NullLogger<ValidationBehavior<TestRequest, string>>.Instance;
        var behavior = new ValidationBehavior<TestRequest, string>(validators, logger);
        var request = new TestRequest(string.Empty);

        // Act
        var result = await behavior.Handle(
            request,
            _ => Task.FromResult("response"),
            CancellationToken.None);

        // Assert
        result.Should().Be("response");
    }

    [Fact]
    public async Task Handle_WithValidRequest_CallsNext()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var logger = NullLogger<ValidationBehavior<TestRequest, string>>.Instance;
        var behavior = new ValidationBehavior<TestRequest, string>(validators, logger);
        var request = new TestRequest("valid value");

        // Act
        var result = await behavior.Handle(
            request,
            _ => Task.FromResult("response"),
            CancellationToken.None);

        // Assert
        result.Should().Be("response");
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var logger = NullLogger<ValidationBehavior<TestRequest, string>>.Instance;
        var behavior = new ValidationBehavior<TestRequest, string>(validators, logger);
        var request = new TestRequest(string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                request,
                _ => Task.FromResult("response"),
                CancellationToken.None));

        exception.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ValidatesAll()
    {
        // Arrange
        var validator1 = new InlineValidator<TestRequest>();
        var validator2 = new InlineValidator<TestRequest>();

        var validators = new List<IValidator<TestRequest>> { validator1, validator2 };
        var logger = NullLogger<ValidationBehavior<TestRequest, string>>.Instance;
        var behavior = new ValidationBehavior<TestRequest, string>(validators, logger);
        var request = new TestRequest("value");

        // Act
        var result = await behavior.Handle(
            request,
            _ => Task.FromResult("response"),
            CancellationToken.None);

        // Assert
        result.Should().Be("response");
    }

    [Fact]
    public async Task Handle_WithValidationFailures_ThrowsValidationException()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var logger = NullLogger<ValidationBehavior<TestRequest, string>>.Instance;
        var behavior = new ValidationBehavior<TestRequest, string>(validators, logger);
        var request = new TestRequest(string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                request,
                _ => Task.FromResult("response"),
                CancellationToken.None));

        exception.Errors.Should().NotBeEmpty();
    }
}
