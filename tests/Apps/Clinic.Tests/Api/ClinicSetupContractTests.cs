using FluentAssertions;
using Vitarish.Clinic.Contracts.Responses;
using ApplicationGoal = Vitarish.Clinic.Application.Setup.ClinicSetupGoal;

namespace Clinic.Tests.Api;

public sealed class ClinicSetupContractTests
{
    [Fact]
    public void The_contract_goals_match_the_application_goals_by_name_and_order()
    {
        Enum.GetNames<ClinicSetupGoal>().Should().Equal(Enum.GetNames<ApplicationGoal>());
        Enum.GetValues<ClinicSetupGoal>().Select(goal => (int)goal)
            .Should().Equal(Enum.GetValues<ApplicationGoal>().Select(goal => (int)goal));
    }
}
