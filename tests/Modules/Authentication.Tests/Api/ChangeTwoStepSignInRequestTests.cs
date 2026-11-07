using System.Text.Json;
using FluentAssertions;
using Vitarish.Authentication.Contracts.Requests;

namespace Authentication.Tests.Api;

public sealed class ChangeTwoStepSignInRequestTests
{
    [Fact]
    public void Deserialization_rejects_missing_enabled_member()
    {
        Action deserialize = () => JsonSerializer.Deserialize<ChangeTwoStepSignInRequest>("{}");

        deserialize.Should().Throw<JsonException>();
    }
}
