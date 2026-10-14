using MediatR;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class UsersSurgeryTeamDirectory(ISender sender) : ISurgeryTeamDirectory
{
    public async Task<IReadOnlyList<SurgeryTeamContact>> FindMembersAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return [];

        Dictionary<string, SurgeryTeamContact> members = new(StringComparer.Ordinal);
        foreach (string userId in userIds)
        {
            if (string.IsNullOrWhiteSpace(userId) || members.ContainsKey(userId)) continue;
            // Search per id because the shared member list caps its result count and could otherwise drop a requested member.
            IReadOnlyList<TenantMemberSummary> found = await sender.Send(
                new ListTenantMembersQuery(userId, TenantMemberQueryDefaults.MaxResults),
                cancellationToken);
            foreach (TenantMemberSummary summary in found)
            {
                if (!string.Equals(summary.Id, userId, StringComparison.Ordinal)) continue;
                members[userId] = new SurgeryTeamContact(summary.Id, DisplayName(summary), summary.Email, summary.PhoneNumber);
                break;
            }
        }

        return userIds.Where(members.ContainsKey).Select(userId => members[userId]).ToArray();
    }

    private static string DisplayName(TenantMemberSummary member)
    {
        string name = string.Join(' ', new[] { member.FirstName, member.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim()));
        return name.Length > 0
            ? name
            : string.IsNullOrWhiteSpace(member.UserName) ? member.Id : member.UserName.Trim();
    }
}
