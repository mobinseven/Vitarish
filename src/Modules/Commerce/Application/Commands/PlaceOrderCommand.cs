using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Application.Commands;

public record PlaceOrderCommand(Guid CartId) : IRequest<EntityResult<OrderReadDto>>;
