
using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Entities.Client.Models;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Entities.Client.SignalR;

public interface IEntityConnectionFactory<TReadDto>
        where TReadDto : EntityReadDto
{
    Task<HubConnection> CreateConnection();
}
