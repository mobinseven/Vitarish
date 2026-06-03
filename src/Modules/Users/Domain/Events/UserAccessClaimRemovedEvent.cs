using MediatR;
namespace Vitarish.Users.Domain.Events;

public record UserAccessClaimRemovedEvent(string UserId, string ClientId) : INotification;
