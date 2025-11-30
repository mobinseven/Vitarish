using Vitarish.Base.Domain.Results;

namespace Base.Tests.Domain.Results;

public class ErrorTests
{
    [Fact]
    public void Error_None_HasEmptyValues()
    {
        // Assert
        Error.None.Code.Should().BeEmpty();
        Error.None.Description.Should().BeEmpty();
    }

    [Fact]
    public void Error_NullValue_HasCorrectValues()
    {
        // Assert
        Error.NullValue.Code.Should().Be("Error.NullValue");
        Error.NullValue.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Error_Validation_CreatesCorrectError()
    {
        // Act
        var error = Error.Validation("Validation.FieldRequired", "The field is required.");

        // Assert
        error.Code.Should().Be("Validation.FieldRequired");
        error.Description.Should().Be("The field is required.");
    }

    [Fact]
    public void Error_NotFound_CreatesCorrectError()
    {
        // Act
        var error = Error.NotFound("Resource.NotFound", "The resource was not found.");

        // Assert
        error.Code.Should().Be("Resource.NotFound");
        error.Description.Should().Be("The resource was not found.");
    }

    [Fact]
    public void Error_Conflict_CreatesCorrectError()
    {
        // Act
        var error = Error.Conflict("Resource.Conflict", "The resource already exists.");

        // Assert
        error.Code.Should().Be("Resource.Conflict");
        error.Description.Should().Be("The resource already exists.");
    }

    [Fact]
    public void Error_Unauthorized_CreatesCorrectError()
    {
        // Act
        var error = Error.Unauthorized("Auth.Unauthorized", "Access denied.");

        // Assert
        error.Code.Should().Be("Auth.Unauthorized");
        error.Description.Should().Be("Access denied.");
    }

    [Fact]
    public void Error_Failure_CreatesCorrectError()
    {
        // Act
        var error = Error.Failure("Operation.Failed", "The operation failed.");

        // Assert
        error.Code.Should().Be("Operation.Failed");
        error.Description.Should().Be("The operation failed.");
    }

    [Fact]
    public void Error_WithSameValues_AreEqual()
    {
        // Arrange
        var error1 = new Error("Code", "Description");
        var error2 = new Error("Code", "Description");

        // Assert
        error1.Should().Be(error2);
    }

    [Fact]
    public void Error_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var error1 = new Error("Code1", "Description1");
        var error2 = new Error("Code2", "Description2");

        // Assert
        error1.Should().NotBe(error2);
    }
}
