using MediatR;
namespace Vorn.Aaas.Users.Domain.Events;

public record UserAccessClaimSetEvent(string UserId, string ClientId, string AccessValue) : INotification;
