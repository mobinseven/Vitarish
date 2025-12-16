# Vorn.Entities.Client

The client package focuses on Blazor and typed SignalR consumption. It wraps an `IEntityClient<TReadDto, TWriteDto, TDescriptorDto>` with a
component base that manages hub connections, busy state, notifications, and error handling so Razor components remain mostly
markup.

## Install

```bash
dotnet add package Vorn.Entities.SignalR.Client
dotnet add package Vorn.Entities.Client
```

Bring in `Vorn.Entities` and `Vorn.Entities.Server` for the backing service/hub if they are not already referenced.

## Configure SignalR access

1. **Connection factory** – centralize HubConnection creation.
   ```csharp
   public sealed class DocumentConnectionFactory(Uri hubUri)
       : IEntityConnectionFactory<DocumentReadDto>
   {
       public Task<HubConnection> CreateConnection()
           => Task.FromResult(new HubConnectionBuilder()
               .WithUrl(hubUri)
               .WithAutomaticReconnect()
               .Build());
   }
   builder.Services.AddScoped<IEntityConnectionFactory<DocumentReadDto>, DocumentConnectionFactory>();
   ```
2. **Ambient interception** – forward user/time context to the server.
   ```csharp
   builder.Services.AddScoped<IEntityInterceptionProvider, AmbientEntityInterceptionProvider>();
   ```
3. **Typed client** – inherit from `EntityClient<TReadDto, TWriteDto, TDescriptorDto>` and register it with DI.
   ```csharp
   public sealed class DocumentClient(
       IEntityConnectionFactory<DocumentReadDto> connections,
       IEntityInterceptionProvider interception)
       : EntityClient<DocumentReadDto, DocumentWriteDto, DocumentDescriptorDto>(connections, interception);

   builder.Services.AddScoped<DocumentClient>();
   ```

## Build a component

`EntityClientComponentBase<TClient, TReadDto, TWriteDto, TDescriptorDto>` supplies the CRUD helpers, busy flag, and notification pipeline. Derive
a component base class and render your UI around the exposed state.

```csharp
public abstract class DocumentListBase
    : EntityClientComponentBase<DocumentClient, DocumentReadDto, DocumentWriteDto, DocumentDescriptorDto>
{
    protected List<DocumentReadDto> Items { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Items.AddRange(await Client.GetListAsync(Descriptor));
    }

    protected override async Task OnNotificationAsync(EntityNotification notification)
    {
        Items.Clear();
        Items.AddRange(await Client.GetListAsync(Descriptor));
    }
}
```

Razor usage:

```razor
@inherits DocumentListBase

@if (IsBusy)
{
    <p>Loading documents...</p>
}
else if (!string.IsNullOrEmpty(ErrorMessage))
{
    <p class="text-danger">@ErrorMessage</p>
}
else
{
    <ul>
        @foreach (var document in Items)
        {
            <li>@document.Title</li>
        }
    </ul>
}
```

## Component options

- `AutoConnect` (default `true`) controls whether the hub starts during `OnInitializedAsync`.
- `StopClientOnDispose` disposes the hub when the component leaves the render tree.
- `EntityInterceptionConfig` scopes user/time metadata across multiple calls.
- `Error` callback surfaces exceptions so pages can show toasts or log failures.

## Tips

- Use `SafeRunAsync` for read-only operations that should toggle `IsBusy` and capture errors without throwing.
- Override `DisposeAsync` when you allocate additional resources—remember to call `await base.DisposeAsync()`.
- Pair with generator-produced DTOs/descriptors to keep client/server contracts in sync.
