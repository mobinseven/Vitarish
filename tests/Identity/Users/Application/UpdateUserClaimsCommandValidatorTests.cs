using FluentValidation.TestHelper;
using System.Security.Claims;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Tests.Application;

public class UpdateUserClaimsCommandValidatorTests
{
    private readonly UpdateUserClaimsCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        User user = new() { Id = "user-1" };
        UpdateUserClaimsCommand command = new(user, new[] { new Claim("scope", "read") });

        TestValidationResult<UpdateUserClaimsCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullUser_Fails()
    {
        UpdateUserClaimsCommand command = new(null!, new[] { new Claim("scope", "read") });

        TestValidationResult<UpdateUserClaimsCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.User);
    }

    [Fact]
    public void Validate_WithNullClaims_Fails()
    {
        User user = new() { Id = "user-1" };
        UpdateUserClaimsCommand command = new(user, null!);

        TestValidationResult<UpdateUserClaimsCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Claims);
    }
}
