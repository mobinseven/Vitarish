using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class UsersTenantCreatorDirectoryTests
{
    private const string UserId = "user-1";

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public async Task Counts_a_confirmed_phone_or_a_confirmed_email_as_a_verified_contact(bool phoneConfirmed, bool emailConfirmed, bool expected)
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.Is<GetUserQuery>(query => query.Id == UserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = UserId, PhoneNumberConfirmed = phoneConfirmed, EmailConfirmed = emailConfirmed });
        sender.Setup(item => item.Send(It.Is<GetUserRolesQuery>(query => query.UserId == UserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<IdentityRole>());

        TenantCreator? creator = await new UsersTenantCreatorDirectory(sender.Object).FindAsync(UserId);

        creator.Should().NotBeNull();
        creator!.HasVerifiedContact.Should().Be(expected);
    }

    [Fact]
    public async Task Finds_no_creator_for_an_unknown_user()
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        (await new UsersTenantCreatorDirectory(sender.Object).FindAsync(UserId)).Should().BeNull();
    }
}
