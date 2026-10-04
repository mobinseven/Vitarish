using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Vitarish.Users.Contracts.Responses;

namespace Users.Tests.Contracts;

public sealed class UsersCommandErrorKindTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Owned_codes_serialize_with_a_typed_failure_code()
    {
        UsersCommandError error = UsersCommandErrorKinds.ForCode(
            UsersCommandErrorCodes.ContactIdentifierInUse,
            UsersCommandErrorCodes.ContactIdentifierInUseDescription);

        string json = JsonSerializer.Serialize(UsersCommandResponse.Failed([error]), JsonOptions);

        json.Should().Contain("\"code\":\"ContactIdentifierInUse\"");
        json.Should().Contain("\"failureCode\":\"ContactIdentifierInUse\"");
    }

    [Fact]
    public void Passthrough_identity_codes_omit_the_failure_code()
    {
        UsersCommandError error = UsersCommandErrorKinds.ForCode("DuplicateUserName", "User name is already taken.");

        string json = JsonSerializer.Serialize(UsersCommandResponse.Failed([error]), JsonOptions);

        error.FailureCode.Should().BeNull();
        json.Should().NotContain("failureCode");
    }

    [Fact]
    public void The_serialized_failure_code_round_trips_to_the_enum()
    {
        UsersCommandError error = UsersCommandErrorKinds.ForCode(
            UsersCommandErrorCodes.ContactIdentifierInUse,
            UsersCommandErrorCodes.ContactIdentifierInUseDescription);
        string json = JsonSerializer.Serialize(UsersCommandResponse.Failed([error]), JsonOptions);

        UsersCommandResponse? deserialized = JsonSerializer.Deserialize<UsersCommandResponse>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Errors.Should().ContainSingle()
            .Which.FailureCode.Should().Be(UsersCommandErrorKind.ContactIdentifierInUse);
    }

    [Fact]
    public void The_kind_enum_mirrors_every_owned_error_code_constant()
    {
        FieldInfo[] ownedCodes = typeof(UsersCommandErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.Name.EndsWith("Description", StringComparison.Ordinal))
            .ToArray();

        Enum.GetNames<UsersCommandErrorKind>().Should().BeEquivalentTo(ownedCodes.Select(field => field.Name));
        foreach (FieldInfo ownedCode in ownedCodes)
        {
            ((string)ownedCode.GetRawConstantValue()!).Should().Be(
                ownedCode.Name, "the wire code value must equal the enum member name");
        }
    }
}
