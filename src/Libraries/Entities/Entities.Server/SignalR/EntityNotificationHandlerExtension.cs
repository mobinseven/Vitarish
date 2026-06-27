using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Entities.Server.SignalR;

public static class EntityNotificationHandlerExtension
{
    public static void AddEntityNotificationHandler<TReadDto, TWriteDto, TDescriptorDto>(this IServiceCollection services)
      where TReadDto : EntityReadDto
      where TWriteDto : EntityWriteDto
      where TDescriptorDto : EntityDescriptorDto => _ = services.AddScoped<INotificationHandler<EntityNotification>, EntityServer<TReadDto, TWriteDto, TDescriptorDto, IEntityService<TReadDto, TWriteDto, TDescriptorDto>>.EntityNotificationHandler>();
}
