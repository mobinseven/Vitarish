using MediatR;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Clinic.Infrastructure.Setup;

/// <summary>
/// Gathers the clinic's setup facts one dependency at a time (the dependencies share scoped database
/// contexts, so they are never awaited concurrently) and lets <see cref="ClinicSetupGoals"/> judge them.
/// </summary>
internal sealed class GetClinicSetupProgressQueryHandler(
    IServiceManager services,
    IServiceProviderManager providers,
    IAccessControlledShiftManager shifts,
    IShiftBookingPolicyManager bookingPolicies,
    ISender sender,
    IRequestContext requestContext)
    : IRequestHandler<GetClinicSetupProgressQuery, ClinicSetupProgress>
{
    private const string DependencyFailureMessage = "The setup progress dependency could not be queried.";

    public async Task<ClinicSetupProgress> Handle(GetClinicSetupProgressQuery request, CancellationToken cancellationToken)
    {
        if (!requestContext.IsPlatformAdmin() && !requestContext.IsTenantOwner())
            return ClinicSetupGoals.NotApplicable;

        int serviceCount = EnsureSuccess(await services.CountAsync(new ServiceDescriptorDto(), cancellationToken));
        int providerCount = EnsureSuccess(await providers.CountAsync(new ServiceProviderDescriptorDto(), cancellationToken));
        int bookingPolicyCount = EnsureSuccess(await bookingPolicies.CountAsync(new ShiftBookingPolicyDescriptorDto(), cancellationToken));
        // Counting shifts loads every row, so ask for the first active one instead.
        int activeShiftCount = EnsureSuccess(await shifts.GetListAsync(new ShiftDescriptorDto { Status = ShiftStatus.Active, Take = 1 }, cancellationToken)).Count;

        IReadOnlyList<TenantMemberSummary> members = await sender.Send(new ListTenantMembersQuery(null, MemberListMaximum), cancellationToken);
        int nonOwnerMembers = members.Count(member => !string.Equals(member.RoleSuffix, IdentityDefinitions.TenantOwner, StringComparison.Ordinal));

        TenantBusinessProfileView? profile = await sender.Send(new GetTenantBusinessProfileQuery(), cancellationToken);

        return ClinicSetupGoals.Evaluate(
            new ClinicSetupFacts(
                serviceCount,
                providerCount,
                activeShiftCount,
                bookingPolicyCount,
                nonOwnerMembers,
                profile?.Details?.Address is not null,
                !string.IsNullOrEmpty(profile?.LogoUrl),
                profile?.SetupGuideDismissed == true),
            callerCanManageSetup: true);
    }

    private const int MemberListMaximum = 200;

    private static T EnsureSuccess<T>(EntityResult<T> result) =>
        result.IsFailure || result.Value is null ? throw new InvalidOperationException(DependencyFailureMessage) : result.Value;
}

/// <summary>Hands the clinic's "later" choice to Parties, which owns the flag and decides who may set it.</summary>
internal sealed class DismissClinicSetupGuideCommandHandler(ISender sender)
    : IRequestHandler<DismissClinicSetupGuideCommand, ClinicSetupGuideDismissal>
{
    public async Task<ClinicSetupGuideDismissal> Handle(DismissClinicSetupGuideCommand request, CancellationToken cancellationToken) =>
        await sender.Send(new DismissTenantSetupGuideCommand(), cancellationToken) switch
        {
            TenantSetupGuideDismissalStatus.Updated => ClinicSetupGuideDismissal.Updated,
            TenantSetupGuideDismissalStatus.Forbidden => ClinicSetupGuideDismissal.Forbidden,
            _ => ClinicSetupGuideDismissal.NotFound
        };
}
