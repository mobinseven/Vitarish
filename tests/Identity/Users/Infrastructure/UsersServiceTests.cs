using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Infrastructure;

public class UsersServiceTests
{
    private readonly Mock<ISender> sender = new();
    private readonly UsersService service;

    public UsersServiceTests()
    {
        service = new UsersService(sender.Object);
    }

    [Fact]
    public async Task GetUsersAsync_SendsQuery()
    {
        sender.Setup(s => s.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        await service.GetUsersAsync(CancellationToken.None);

        sender.Verify(s => s.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserAsync_SendsQueryWithId()
    {
        sender.Setup(s => s.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = "user-1" });

        await service.GetUserAsync("user-1", CancellationToken.None);

        sender.Verify(s => s.Send(It.Is<GetUserQuery>(q => q.Id == "user-1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserWithEmailAsync_SendsCommand()
    {
        CreateUserWithEmailCommand command = new("user@example.com", "First", "Last");
        sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(new User());

        await service.CreateUserWithEmailAsync(command, CancellationToken.None);

        sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_SendsCommand()
    {
        UpdateUserCommand command = new(new User { Id = "user-1" });
        sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);

        await service.UpdateUserAsync(command, CancellationToken.None);

        sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_SendsCommand()
    {
        DeleteUserCommand command = new("user-1");
        sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);

        await service.DeleteUserAsync(command, CancellationToken.None);

        sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserClaimsAsync_MapsClaimDtos()
    {
        sender.Setup(s => s.Send(It.IsAny<GetUserClaimsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ClaimDto("scope", "read") });

        IList<Claim> claims = await service.GetUserClaimsAsync("user-1", CancellationToken.None);

        claims.Should().ContainSingle(c => c.Type == "scope" && c.Value == "read");
    }

    [Fact]
    public async Task CheckUserEmailInUseAsync_SendsQueryWithExclude()
    {
        sender.Setup(s => s.Send(It.IsAny<CheckUserEmailInUseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await service.CheckUserEmailInUseAsync("user@example.com", "exclude", CancellationToken.None);

        sender.Verify(s => s.Send(
            It.Is<CheckUserEmailInUseQuery>(q => q.Email == "user@example.com" && q.ExcludeUserId == "exclude"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetUserEmailAsync_SendsCommand()
    {
        sender.Setup(s => s.Send(It.IsAny<SetUserEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        await service.SetUserEmailAsync("user-1", "user@example.com", CancellationToken.None);

        sender.Verify(s => s.Send(
            It.Is<SetUserEmailCommand>(c => c.Id == "user-1" && c.Email == "user@example.com"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
