namespace Vitarish.Entities.Client.SignalR;

using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Entities.Client.Models;

public interface IEntityConnectionFactory<TReadDto>
        where TReadDto : EntityReadDto
{
    Task<HubConnection> CreateConnection();
}
