namespace Vitarish.Clinic.Application.Abstractions;

/// <summary>A current-clinic member who can join a surgery team, with the contacts reminders are sent to.</summary>
public sealed record SurgeryTeamContact(string UserId, string DisplayName, string? Email, string? PhoneNumber);

public interface ISurgeryTeamDirectory
{
    /// <summary>Returns the members of the current clinic among <paramref name="userIds"/>; ids of non-members are omitted.</summary>
    Task<IReadOnlyList<SurgeryTeamContact>> FindMembersAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);
}
