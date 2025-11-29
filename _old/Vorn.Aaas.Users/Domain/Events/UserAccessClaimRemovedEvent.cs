using MediatR;
namespace Vorn.Aaas.Users.Domain.Events;

public record UserAccessClaimRemovedEvent(string UserId, string ClientId) : INotification;
