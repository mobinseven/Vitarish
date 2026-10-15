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
        string[] requested = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToArray();
        if (requested.Length == 0) return [];

        Dictionary<string, SurgeryTeamContact> members = new(StringComparer.Ordinal);
        // One query per chunk of IDs; the member list caps its result count, so a chunk never exceeds that cap.
        foreach (string[] chunk in requested.Chunk(TenantMemberQueryDefaults.MaxResults))
        {
            IReadOnlyList<TenantMemberSummary> found = await sender.Send(
                new ListTenantMembersQuery(null, chunk.Length, chunk),
                cancellationToken);
            foreach (TenantMemberSummary summary in found)
            {
                if (chunk.Contains(summary.Id, StringComparer.Ordinal))
                    members[summary.Id] = new SurgeryTeamContact(summary.Id, DisplayName(summary), summary.Email, summary.PhoneNumber);
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
