using Vitarish.Base.Domain.Results;

namespace Base.Tests.Domain.Results;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        // Arrange
        var error = Error.Validation("Code", "Description");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Success_WithValue_CreatesSuccessfulResultWithValue()
    {
        // Act
        var result = Result.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_WithValue_CreatesFailedResultWithoutValue()
    {
        // Arrange
        var error = Error.NotFound("Code", "Description");

        // Act
        var result = Result.Failure<int>(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_AccessingValue_ThrowsException()
    {
        // Arrange
        var error = Error.Failure("Code", "Description");
        var result = Result.Failure<int>(error);

        // Act
        var act = () => result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void Create_WithNonNullValue_ReturnsSuccess()
    {
        // Act
        var result = Result.Create("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void Create_WithNullValue_ReturnsFailure()
    {
        // Act
        var result = Result.Create<string>(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ReturnsSuccess()
    {
        // Act
        Result<int> result = 42;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        // Arrange
        var result = Result.Success(10);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(20);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        // Arrange
        var error = Error.Validation("Code", "Description");
        var result = Result.Failure<int>(error);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_OnSuccess_ChainsResults()
    {
        // Arrange
        var result = Result.Success(10);

        // Act
        var bound = result.Bind(x => Result.Success(x.ToString()));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("10");
    }

    [Fact]
    public void Bind_OnFailure_PropagatesError()
    {
        // Arrange
        var error = Error.Conflict("Code", "Description");
        var result = Result.Failure<int>(error);

        // Act
        var bound = result.Bind(x => Result.Success(x.ToString()));

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public void Match_OnSuccess_CallsOnSuccess()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onFailure: e => $"Error: {e.Code}");

        // Assert
        output.Should().Be("Value: 42");
    }

    [Fact]
    public void Match_OnFailure_CallsOnFailure()
    {
        // Arrange
        var error = Error.Validation("ERR001", "Test error");
        var result = Result.Failure<int>(error);

        // Act
        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onFailure: e => $"Error: {e.Code}");

        // Assert
        output.Should().Be("Error: ERR001");
    }
}
