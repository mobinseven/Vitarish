using Vitarish.Entities.Server.Models;

namespace Vitarish.Commerce.Application.Commands;

public record PlaceOrderCommand(Guid CartId) : IRequest<EntityResult<OrderReadDto>>;
