using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Moq;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Clinic.Infrastructure.Setup;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;

namespace Clinic.Tests.Infrastructure;

public sealed class GetClinicSetupProgressQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task An_owner_of_an_empty_clinic_is_guided_from_the_first_goal()
    {
        Fixture fixture = new();

        ClinicSetupProgress progress = await fixture.Handler(owner: true).Handle(new GetClinicSetupProgressQuery(), CancellationToken.None);

        progress.Applies.Should().BeTrue();
        progress.NextGoal.Should().Be(ClinicSetupGoal.ServicesAndProviders);
    }

    [Fact]
    public async Task Facts_come_from_the_managers_members_and_profile_and_are_read_one_after_another()
    {
        Fixture fixture = new() { Services = 2, Providers = 1, ShiftRows = 1, Policies = 1 };
        fixture.Members.AddRange([Member("TenantOwner"), Member("TenantUser")]);
        fixture.Profile = new TenantBusinessProfileView(
            "Clinic", null, "/logo", null,
            TenantBusinessDetails.TryCreate(new TenantBusinessAddress("Tehran", null, null, null, null, null), null, null, null, null, null, out _),
            false);

        ClinicSetupProgress progress = await fixture.Handler(owner: true).Handle(new GetClinicSetupProgressQuery(), CancellationToken.None);

        progress.Applies.Should().BeFalse();
        progress.NextGoal.Should().BeNull();
        fixture.ShiftDescriptor.Should().NotBeNull();
        fixture.ShiftDescriptor!.Status.Should().Be(ShiftStatus.Active);
        fixture.ShiftDescriptor.Take.Should().Be(1);
        fixture.MaxConcurrentCalls.Should().Be(1);
    }

    [Fact]
    public async Task A_dismissed_clinic_reports_the_dismissal()
    {
        Fixture fixture = new() { Profile = new TenantBusinessProfileView("Clinic", null, null, null, null, false, SetupGuideDismissed: true) };

        ClinicSetupProgress progress = await fixture.Handler(owner: true).Handle(new GetClinicSetupProgressQuery(), CancellationToken.None);

        progress.Applies.Should().BeFalse();
        progress.Dismissed.Should().BeTrue();
    }

    [Fact]
    public async Task Staff_get_nothing_to_set_up_and_no_member_or_profile_query_is_sent()
    {
        Fixture fixture = new();

        ClinicSetupProgress progress = await fixture.Handler(owner: false).Handle(new GetClinicSetupProgressQuery(), CancellationToken.None);

        progress.Applies.Should().BeFalse();
        progress.Goals.Should().BeEmpty();
        fixture.Sender.Verify(item => item.Send(It.IsAny<ListTenantMembersQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Sender.Verify(item => item.Send(It.IsAny<GetTenantBusinessProfileQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_failing_dependency_surfaces_as_an_invalid_operation()
    {
        Fixture fixture = new() { ServicesFail = true };

        Func<Task> act = () => fixture.Handler(owner: true).Handle(new GetClinicSetupProgressQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static TenantMemberSummary Member(string role) => new(Guid.NewGuid().ToString(), null, null, null, null, null, null, role);

    private sealed class Fixture
    {
        private int _inFlight;

        public int Services { get; init; }
        public int Providers { get; init; }
        public int ShiftRows { get; init; }
        public int Policies { get; init; }
        public bool ServicesFail { get; init; }
        public List<TenantMemberSummary> Members { get; } = [];
        public TenantBusinessProfileView? Profile { get; set; } = new("Clinic", null, null, null, null, false);
        public ShiftDescriptorDto? ShiftDescriptor { get; private set; }
        public int MaxConcurrentCalls { get; private set; }
        public Mock<ISender> Sender { get; } = new();

        public GetClinicSetupProgressQueryHandler Handler(bool owner)
        {
            Mock<IServiceManager> services = new();
            services.Setup(item => item.CountAsync(It.IsAny<ServiceDescriptorDto>(), It.IsAny<CancellationToken>()))
                .Returns(() => Tracked(ServicesFail ? EntityResult<int>.Failure(EntityError.Unknown("failed")) : EntityResult<int>.Success(Services)));
            Mock<IServiceProviderManager> providers = new();
            providers.Setup(item => item.CountAsync(It.IsAny<ServiceProviderDescriptorDto>(), It.IsAny<CancellationToken>()))
                .Returns(() => Tracked(EntityResult<int>.Success(Providers)));
            Mock<IShiftBookingPolicyManager> policies = new();
            policies.Setup(item => item.CountAsync(It.IsAny<ShiftBookingPolicyDescriptorDto>(), It.IsAny<CancellationToken>()))
                .Returns(() => Tracked(EntityResult<int>.Success(Policies)));
            Mock<IAccessControlledShiftManager> shifts = new();
            shifts.Setup(item => item.GetListAsync(It.IsAny<ShiftDescriptorDto>(), It.IsAny<CancellationToken>()))
                .Returns((ShiftDescriptorDto descriptor, CancellationToken _) =>
                {
                    ShiftDescriptor = descriptor;
                    return Tracked(EntityResult<IReadOnlyList<ShiftReadDto>>.Success(Enumerable.Range(0, ShiftRows).Select(_ => new ShiftReadDto()).ToArray()));
                });
            Sender.Setup(item => item.Send(It.IsAny<ListTenantMembersQuery>(), It.IsAny<CancellationToken>()))
                .Returns(() => Tracked<IReadOnlyList<TenantMemberSummary>>(Members));
            Sender.Setup(item => item.Send(It.IsAny<GetTenantBusinessProfileQuery>(), It.IsAny<CancellationToken>()))
                .Returns(() => Tracked(Profile));

            Mock<IRequestContext> context = new();
            context.SetupGet(item => item.TenantId).Returns(TenantId.ToString());
            context.SetupGet(item => item.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
                owner ? [new Claim(ClaimTypes.Role, $"{TenantId}:{IdentityDefinitions.TenantOwner}")] : [],
                "test")));
            return new(services.Object, providers.Object, shifts.Object, policies.Object, Sender.Object, context.Object);
        }

        private async Task<T> Tracked<T>(T value)
        {
            MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, Interlocked.Increment(ref _inFlight));
            await Task.Yield();
            _ = Interlocked.Decrement(ref _inFlight);
            return value;
        }
    }
}
