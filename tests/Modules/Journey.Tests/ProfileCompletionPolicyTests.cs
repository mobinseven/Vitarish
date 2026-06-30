using Moq;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.ProfileCompletion;
using Vitarish.Users.Domain.Entities;

namespace Journey.Tests;

public class ProfileCompletionPolicyTests
{
    private readonly Mock<IUsersService> _usersServiceMock = new();
    private readonly ProfileCompletionPolicy _sut;

    public ProfileCompletionPolicyTests()
    {
        _sut = new ProfileCompletionPolicy(_usersServiceMock.Object);
    }

    [Fact]
    public async Task EvaluateAsync_CompleteProfile_ReturnsComplete()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            FirstName = "John",
            LastName = "Doe",
            EmailConfirmed = true
        };

        _usersServiceMock.Setup(x => x.GetUserAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.EvaluateAsync("user1");

        // Assert
        result.IsComplete.Should().BeTrue();
        result.MissingRequirements.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_MissingFirstName_ReturnsIncomplete()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            FirstName = null,
            LastName = "Doe",
            EmailConfirmed = true
        };

        _usersServiceMock.Setup(x => x.GetUserAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.EvaluateAsync("user1");

        // Assert
        result.IsComplete.Should().BeFalse();
        result.MissingRequirements.Should().Contain(r => r.FieldName == "FirstName");
    }

    [Fact]
    public async Task EvaluateAsync_MissingLastName_ReturnsIncomplete()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            FirstName = "John",
            LastName = null,
            EmailConfirmed = true
        };

        _usersServiceMock.Setup(x => x.GetUserAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.EvaluateAsync("user1");

        // Assert
        result.IsComplete.Should().BeFalse();
        result.MissingRequirements.Should().Contain(r => r.FieldName == "LastName");
    }

    [Fact]
    public async Task EvaluateAsync_NoConfirmedContact_ReturnsIncomplete()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            FirstName = "John",
            LastName = "Doe",
            EmailConfirmed = false,
            PhoneNumberConfirmed = false
        };

        _usersServiceMock.Setup(x => x.GetUserAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.EvaluateAsync("user1");

        // Assert
        result.IsComplete.Should().BeFalse();
        result.MissingRequirements.Should().Contain(r => r.FieldName == "ConfirmedContact");
    }

    [Fact]
    public async Task EvaluateAsync_EmailConfirmed_ReturnsComplete()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            FirstName = "John",
            LastName = "Doe",
            EmailConfirmed = true,
            PhoneNumberConfirmed = false
        };

        _usersServiceMock.Setup(x => x.GetUserAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.EvaluateAsync("user1");

        // Assert
        result.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_PhoneConfirmed_ReturnsComplete()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            FirstName = "John",
            LastName = "Doe",
            EmailConfirmed = false,
            PhoneNumberConfirmed = true
        };

        _usersServiceMock.Setup(x => x.GetUserAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.EvaluateAsync("user1");

        // Assert
        result.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_UserNotFound_ReturnsIncomplete()
    {
        // Arrange
        _usersServiceMock.Setup(x => x.GetUserAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.EvaluateAsync("user1");

        // Assert
        result.IsComplete.Should().BeFalse();
        result.MissingRequirements.Should().Contain(r => r.FieldName == "User");
    }

    [Fact]
    public async Task EvaluateAsync_MultipleMissingFields_ReturnsAllMissing()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            FirstName = null,
            LastName = null,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false
        };

        _usersServiceMock.Setup(x => x.GetUserAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.EvaluateAsync("user1");

        // Assert
        result.IsComplete.Should().BeFalse();
        result.MissingRequirements.Should().HaveCount(3);
        result.MissingRequirements.Should().Contain(r => r.FieldName == "FirstName");
        result.MissingRequirements.Should().Contain(r => r.FieldName == "LastName");
        result.MissingRequirements.Should().Contain(r => r.FieldName == "ConfirmedContact");
    }
}
