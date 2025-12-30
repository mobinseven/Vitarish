using System.Text.Json;
using Vitarish.Interface.Infrastructure.Extensions;

namespace Vitarish.Accounts.Infrastructure.Tests.Extensions;

public class IdentityJsonContextTests
{
    [Fact]
    public void IdentityJsonContext_SerializesExpectedTypes()
    {
        Dictionary<string, string> payload = new()
        {
            ["theme"] = "dark"
        };

        string json = JsonSerializer.Serialize(payload, IdentityJsonContext.Default.DictionaryStringString);

        json.Should().Contain("theme");
        json.Should().Contain("dark");
    }

    [Fact]
    public void IdentityJsonContext_DeserializesExpectedTypes()
    {
        string json = "{\"language\":\"en-US\"}";

        Dictionary<string, string>? result = JsonSerializer.Deserialize(
            json,
            IdentityJsonContext.Default.DictionaryStringString);

        result.Should().NotBeNull();
        result!["language"].Should().Be("en-US");
    }
}
