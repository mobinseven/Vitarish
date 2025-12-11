using MediatR;
namespace Vitarish.Users.Domain.Events;

public record UserAccessClaimSetEvent(string UserId, string ClientId, string AccessValue) : INotification;
