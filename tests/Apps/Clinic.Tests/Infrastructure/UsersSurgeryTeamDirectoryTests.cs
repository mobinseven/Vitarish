using FluentAssertions;
using MediatR;
using Moq;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class UsersSurgeryTeamDirectoryTests
{
    [Fact]
    public async Task A_team_is_looked_up_with_one_query_and_returned_in_the_callers_order()
    {
        Mock<ISender> sender = new();
        sender.Setup(value => value.Send(It.IsAny<ListTenantMembersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListTenantMembersQuery query, CancellationToken _) => query.UserIds!
                .Select(id => new TenantMemberSummary(id, id, "First " + id, null, id + "@example.test", null, null, "TenantUser"))
                .Reverse()
                .ToArray());
        UsersSurgeryTeamDirectory directory = new(sender.Object);

        IReadOnlyList<SurgeryTeamContact> members = await directory.FindMembersAsync(["u3", "u1", "u2", "u1"], CancellationToken.None);

        members.Select(member => member.UserId).Should().Equal("u3", "u1", "u2", "u1");
        sender.Verify(value => value.Send(
            It.Is<ListTenantMembersQuery>(query => query.Search == null && query.MaxResults == 3 && query.UserIds!.SequenceEqual(new[] { "u3", "u1", "u2" })),
            It.IsAny<CancellationToken>()), Times.Once);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_member_outside_the_clinic_is_left_out()
    {
        Mock<ISender> sender = new();
        sender.Setup(value => value.Send(It.IsAny<ListTenantMembersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TenantMemberSummary("u1", "u1", null, null, null, null, null, "TenantUser")]);
        UsersSurgeryTeamDirectory directory = new(sender.Object);

        IReadOnlyList<SurgeryTeamContact> members = await directory.FindMembersAsync(["u1", "outsider"], CancellationToken.None);

        members.Select(member => member.UserId).Should().Equal("u1");
    }
}
