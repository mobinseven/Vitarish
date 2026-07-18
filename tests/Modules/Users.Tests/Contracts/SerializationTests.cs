using System.Text.Json;
using FluentAssertions;
using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Contracts.Routes;

namespace Users.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void SearchUsersQueryParameters_round_trip()
    {
        var original = new SearchUsersQueryParameters("test", 10, true);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<SearchUsersQueryParameters>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Query.Should().Be("test");
        deserialized.MaxResults.Should().Be(10);
        deserialized.IncludeTenantUsers.Should().BeTrue();
    }

    [Fact]
    public void CreateUserWithPhoneRequest_round_trip()
    {
        var original = new CreateUserWithPhoneRequest("+1234567890", "John", "Doe", "ID123", new DateOnly(1990, 1, 1), "US", "NYC");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateUserWithPhoneRequest>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.PhoneNumber.Should().Be("+1234567890");
        deserialized.FirstName.Should().Be("John");
        deserialized.LastName.Should().Be("Doe");
    }

    [Fact]
    public void CreateUserWithEmailRequest_round_trip()
    {
        var original = new CreateUserWithEmailRequest("test@example.com", "Jane", "Smith");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateUserWithEmailRequest>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Email.Should().Be("test@example.com");
        deserialized.FirstName.Should().Be("Jane");
    }

    [Fact]
    public void SetUserEmailRequest_round_trip()
    {
        var original = new SetUserEmailRequest("new@example.com");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<SetUserEmailRequest>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Email.Should().Be("new@example.com");
    }

    [Fact]
    public void AddUserToRoleRequest_round_trip()
    {
        var original = new AddUserToRoleRequest("Admin");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AddUserToRoleRequest>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.RoleName.Should().Be("Admin");
    }

    [Fact]
    public void UserSummaryResponse_round_trip()
    {
        var original = new UserSummaryResponse("id-1", "johndoe", "John", "Doe", "john@test.com", "+1234567890", "ID1", new DateOnly(1990, 1, 1), "US", "NYC", "John Doe");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UserSummaryResponse>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("id-1");
        deserialized.UserName.Should().Be("johndoe");
        deserialized.FirstName.Should().Be("John");
        deserialized.LastName.Should().Be("Doe");
        deserialized.Email.Should().Be("john@test.com");
        deserialized.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void UserProfileResponse_does_not_expose_theme_setting()
    {
        var response = new UserProfileResponse(
            "id-1", "johndoe", "John", "Doe", "john@test.com", "+1234567890", "en", "en");

        var json = JsonSerializer.Serialize(response, JsonOptions);

        json.Should().NotContain("\"theme\"");
    }

    [Fact]
    public void UserExistenceResponse_round_trip()
    {
        var original = new UserExistenceResponse(true);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UserExistenceResponse>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Exists.Should().BeTrue();
    }

    [Fact]
    public void UserRoleCheckResponse_round_trip()
    {
        var original = new UserRoleCheckResponse(true);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UserRoleCheckResponse>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.IsInRole.Should().BeTrue();
    }

    [Fact]
    public void UsersCommandResponse_round_trip_success()
    {
        var original = new UsersCommandResponse(true, []);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UsersCommandResponse>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeTrue();
        deserialized.Errors.Should().BeEmpty();
    }

    [Fact]
    public void UsersCommandResponse_round_trip_with_errors()
    {
        var errors = new List<UsersCommandError> { new("code", "description") };
        var original = new UsersCommandResponse(false, errors);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UsersCommandResponse>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeFalse();
        deserialized.Errors.Should().HaveCount(1);
        deserialized.Errors[0].Code.Should().Be("code");
    }

    [Fact]
    public void UsersApiRoutes_constants_are_correct()
    {
        UsersApiRoutes.Base.Should().Be("/api/modules/users");
        UsersApiRoutes.Search.Should().Be("/search");
        UsersApiRoutes.ById.Should().Be("/{userId}");
        UsersApiRoutes.CheckEmail.Should().Be("/email-in-use");
        UsersApiRoutes.CheckPhone.Should().Be("/phone-in-use");
        UsersApiRoutes.CheckRole.Should().Be("/role-membership");
        UsersApiRoutes.CreateWithPhone.Should().Be("/create/phone");
        UsersApiRoutes.CreateWithEmail.Should().Be("/create/email");
        UsersApiRoutes.SetEmail.Should().Be("/{userId}/email");
        UsersApiRoutes.AddRole.Should().Be("/{userId}/roles");
    }
}
