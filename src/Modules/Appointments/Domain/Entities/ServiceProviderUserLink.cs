namespace Vitarish.Appointments.Domain.Entities;

public class ServiceProviderUserLink
{
    public Guid ServiceProviderId { get; set; }
    public string UserId { get; set; } = string.Empty;
}
