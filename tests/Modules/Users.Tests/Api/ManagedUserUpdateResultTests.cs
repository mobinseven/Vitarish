using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Api;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Users.Tests.Api;

public sealed class ManagedUserUpdateResultTests
{
    [Theory]
    [InlineData(UsersCommandFailureCode.ContactIdentifierInUse, StatusCodes.Status409Conflict)]
    [InlineData(UsersCommandFailureCode.ConcurrencyFailure, StatusCodes.Status409Conflict)]
    [InlineData(UsersCommandFailureCode.UserNotFound, StatusCodes.Status404NotFound)]
    [InlineData(UsersCommandFailureCode.InvalidContact, StatusCodes.Status400BadRequest)]
    public void Update_failures_map_to_the_same_status_as_create(UsersCommandFailureCode failure, int expectedStatus)
    {
        IResult result = UsersApi.ToManagedUserUpdateResult(IdentityResult.Failed(new IdentityError { Code = failure.ToString() }));

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(expectedStatus);
    }
}
