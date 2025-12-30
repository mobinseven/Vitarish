using Vitarish.Accounts.Domain.Constants;

namespace Vitarish.Accounts.Domain.Tests.Constants;

public class AaasConstantsTests
{
    [Fact]
    public void AaasConstants_KnownValues_AreStable()
    {
        AaasConstants.Admin.Should().Be("Admin");
        AaasConstants.AccessClient.Should().Be("AccessClient");
        AaasConstants.AccessClaim.Should().Be("access");
        AaasConstants.AccessScope.Should().Be("access");
        AaasConstants.HubScope.Should().Be("hub");
    }
}
