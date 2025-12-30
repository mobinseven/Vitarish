using MediatR;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Infrastructure;

public class UserProfileServiceTests
{
    private readonly Mock<ISender> sender = new();
    private readonly UserProfileService service;

    public UserProfileServiceTests()
    {
        service = new UserProfileService(sender.Object);
    }

    [Fact]
    public async Task GetProfileAsync_SendsQueryWithUserId()
    {
        // Arrange
        Dictionary<string, string> expected = new() { ["pref"] = "value" };
        sender.Setup(s => s.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(expected));

        // Act
        Dictionary<string, string> result = await service.GetProfileAsync("user-123", CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        sender.Verify(s => s.Send(It.Is<GetUserProfileQuery>(q => q.UserId == "user-123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_SendsCommandWithUserIdAndPreferences()
    {
        // Arrange
        Dictionary<string, string> preferences = new() { ["theme"] = "dark" };

        sender.Setup(s => s.Send(It.IsAny<UpdateUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await service.UpdateProfileAsync("user-456", preferences, CancellationToken.None);

        // Assert
        sender.Verify(s => s.Send(
            It.Is<UpdateUserProfileCommand>(cmd => cmd.UserId == "user-456" && cmd.Preferences == preferences),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_WhenSenderThrows_PropagatesException()
    {
        sender.Setup(s => s.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("invalid user"));

        Func<Task> act = () => service.GetProfileAsync("missing", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenSenderThrows_PropagatesException()
    {
        Dictionary<string, string> preferences = new() { ["theme"] = "dark" };
        sender.Setup(s => s.Send(It.IsAny<UpdateUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("invalid user"));

        Func<Task> act = () => service.UpdateProfileAsync("missing", preferences, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDataException>();
    }
}
