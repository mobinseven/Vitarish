using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Users.Api;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Tests.Api;

public sealed class UserSecurityDetailsResponseMappingTests
{
    private const string UserId = "user-42";

    [Fact]
    public async Task Maps_authentication_methods_and_passkey_identifiers_to_a_200_response()
    {
        Guid credentialId = Guid.NewGuid();
        DateTime registeredAt = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        DateTime lastUsedAt = new(2026, 10, 20, 9, 30, 0, DateTimeKind.Utc);
        UserSecurityDetailsQueryResult details = Details(
            new UserAuthenticationMethodsResult(
                PasswordSet: true,
                EmailVerified: true,
                PhoneNumberVerified: false,
                Passkeys: [new UserPasskeyEntry(credentialId, "Office laptop", registeredAt, lastUsedAt)]),
            authenticationMethodsUnavailable: false);
        Mock<ISender> sender = Sender(details);

        IResult result = await UsersApi.GetUserSecurityDetailsAsync(UserId, 1, 25, null, sender.Object, CancellationToken.None);

        Ok<UserSecurityDetailsResponse> response = result.Should().BeAssignableTo<Ok<UserSecurityDetailsResponse>>().Subject;
        response.Value!.AuthenticationMethodsUnavailable.Should().BeFalse();
        UserAuthenticationMethodsResponse methods = response.Value.AuthenticationMethods!;
        methods.PasswordSet.Should().BeTrue();
        methods.EmailVerified.Should().BeTrue();
        methods.PhoneNumberVerified.Should().BeFalse();
        methods.Passkeys.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new UserPasskeyResponse(credentialId, "Office laptop", registeredAt, lastUsedAt));
    }

    [Fact]
    public async Task Maps_unavailable_authentication_methods_to_a_200_response_without_losing_history()
    {
        UserSecurityDetailsQueryResult details = Details(authenticationMethods: null, authenticationMethodsUnavailable: true);
        Mock<ISender> sender = Sender(details);

        IResult result = await UsersApi.GetUserSecurityDetailsAsync(UserId, 1, 25, null, sender.Object, CancellationToken.None);

        Ok<UserSecurityDetailsResponse> response = result.Should().BeAssignableTo<Ok<UserSecurityDetailsResponse>>().Subject;
        response.Value!.AuthenticationMethods.Should().BeNull();
        response.Value.AuthenticationMethodsUnavailable.Should().BeTrue();
        response.Value.SecurityHistoryAvailable.Should().BeTrue();
        response.Value.AuthenticationHistoryAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Keeps_authentication_methods_on_a_503_history_unavailable_response()
    {
        Guid credentialId = Guid.NewGuid();
        UserSecurityDetailsQueryResult details = Details(
            new UserAuthenticationMethodsResult(true, true, false, [new UserPasskeyEntry(credentialId, "Office laptop", new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc), null)]),
            authenticationMethodsUnavailable: false) with
        {
            SecurityHistoryAvailable = false,
            AuthenticationHistoryAvailable = false
        };
        Mock<ISender> sender = Sender(details);

        IResult result = await UsersApi.GetUserSecurityDetailsAsync(UserId, 1, 25, null, sender.Object, CancellationToken.None);

        JsonHttpResult<UserSecurityDetailsResponse> response = result.Should().BeAssignableTo<JsonHttpResult<UserSecurityDetailsResponse>>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        response.Value!.AuthenticationMethods!.Passkeys.Should().ContainSingle().Which.Id.Should().Be(credentialId);
    }

    private static UserSecurityDetailsQueryResult Details(
        UserAuthenticationMethodsResult? authenticationMethods,
        bool authenticationMethodsUnavailable) => new(
        IsAuthorized: true,
        User: new User
        {
            Id = UserId,
            UserName = "user@example.test",
            Email = "user@example.test",
            FirstName = "Test",
            LastName = "User",
            IsEnabled = true
        },
        AssignedRoles: [IdentityDefinitions.PlatformAdmin],
        SecurityHistory: new UserSecurityHistoryResult([], 1, 25, 0),
        SecurityHistoryAvailable: true,
        AuthenticationHistory: new UserSecurityHistoryResult([], 1, 25, 0),
        AuthenticationHistoryAvailable: true,
        AuthenticationMethods: authenticationMethods,
        AuthenticationMethodsUnavailable: authenticationMethodsUnavailable);

    private static Mock<ISender> Sender(UserSecurityDetailsQueryResult details)
    {
        Mock<ISender> sender = new();
        sender.Setup(value => value.Send(It.IsAny<GetUserSecurityDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);
        return sender;
    }
}
