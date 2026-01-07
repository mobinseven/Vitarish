# Bottom Navigation Service

## Overview

The **BottomNavService** provides centralized control over the bottom navigation bar in Blazor applications. It manages four navigation buttons: **Home**, **Search**, **Back**, and **Profile**, allowing pages to configure their behavior without directly manipulating UI components.

## Features

- ✅ Centralized state management for bottom navigation
- ✅ Per-page configuration support
- ✅ Configurable global defaults
- ✅ Automatic state reset on navigation
- ✅ Type-safe API with strong typing
- ✅ Dependency injection ready

## Quick Start

### 1. Configure Global Defaults (Optional)

If you want to customize the default URIs for navigation buttons, register `BottomNavDefaults` in your DI container:

```csharp
builder.Services.AddScoped(_ => new BottomNavDefaults(
    HomeUri: "/dashboard",
    ProfileUri: "/user/profile",
    SearchUri: "/search"
));
```

If not configured, the service uses built-in defaults:
- **Home**: `/`
- **Profile**: `/profile`
- **Search**: `/search`

### 2. Service Registration

The service is automatically registered via the `[RegisterScoped]` attribute using Injectio. No manual registration needed.

### 3. Basic Usage in Layout

Subscribe to state changes in your layout component:

```razor
@inject IBottomNavService BottomNav
@implements IDisposable

<div class="bottom-nav">
    @if (BottomNav.Current.Home.Visible)
    {
        <button @onclick="BottomNav.Current.Home.OnClick" disabled="@(!BottomNav.Current.Home.Enabled)">
            Home
        </button>
    }
    
    @if (BottomNav.Current.Search.Visible)
    {
        <button @onclick="BottomNav.Current.Search.OnClick" disabled="@(!BottomNav.Current.Search.Enabled)">
            Search
        </button>
    }
    
    @if (BottomNav.Current.Back.Visible)
    {
        <button @onclick="BottomNav.Current.Back.OnClick" disabled="@(!BottomNav.Current.Back.Enabled)">
            Back
        </button>
    }
    
    @if (BottomNav.Current.Profile.Visible)
    {
        <button @onclick="BottomNav.Current.Profile.OnClick" disabled="@(!BottomNav.Current.Profile.Enabled)">
            Profile
        </button>
    }
</div>

@code {
    protected override void OnInitialized()
    {
        BottomNav.Changed += OnNavChanged;
    }

    private async void OnNavChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        BottomNav.Changed -= OnNavChanged;
    }
}
```

## Per-Page Configuration

### Code-Based Configuration

Configure navigation in your page's `OnInitialized` or `OnParametersSet`:

```csharp
@page "/product/{id}"
@inject IBottomNavService BottomNav

<h1>Product Details</h1>

@code {
    [Parameter]
    public string Id { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        BottomNav.Apply(new BottomNavOptions(
            HomeUriOverride: "/products",  // Override home to go to products list
            Back: new BackTarget(
                Visible: true,
                Uri: "/products"  // Back button navigates to product list
            )
        ));
    }
}
```

## Button Configuration Examples

### Home Button

The **Home** button is always visible. You can override its destination per page:

```csharp
// Default: navigates to global HomeUri (default: "/")
BottomNav.Apply(null);

// Override for this page
BottomNav.Apply(new BottomNavOptions(
    HomeUriOverride: "/dashboard"
));
```

### Search Button

The **Search** button is always visible. Configure custom behavior or override the destination:

```csharp
// Default: navigates to global SearchUri (default: "/search")
BottomNav.Apply(null);

// Navigate to a custom search page
BottomNav.Apply(new BottomNavOptions(
    Search: new SearchAction(Uri: "/products/search")
));

// Execute custom action (e.g., open a search overlay)
BottomNav.Apply(new BottomNavOptions(
    Search: new SearchAction(
        Action: async () =>
        {
            await DialogService.ShowAsync<SearchDialog>();
        }
    )
));
```

### Back Button

The **Back** button is hidden by default. Configure it to appear with a target:

```csharp
// Hidden (default)
BottomNav.Apply(null);

// Show and navigate to a specific URI
BottomNav.Apply(new BottomNavOptions(
    Back: new BackTarget(
        Visible: true,
        Uri: "/previous-page"
    )
));

// Show and execute custom logic
BottomNav.Apply(new BottomNavOptions(
    Back: new BackTarget(
        Visible: true,
        Action: async () =>
        {
            await NavigationManager.NavigateBack();
        }
    )
));

// Explicitly hide (not needed, as it's hidden by default)
BottomNav.Apply(new BottomNavOptions(
    Back: new BackTarget(Visible: false)
));
```

### Profile Button

The **Profile** button is always visible and always navigates to the configured `ProfileUri`. It cannot be customized per page.

## Common Scenarios

### Detail Page with Back Button

```csharp
@page "/order/{orderId}"
@inject IBottomNavService BottomNav

@code {
    protected override void OnInitialized()
    {
        BottomNav.Apply(new BottomNavOptions(
            HomeUriOverride: "/orders",  // Home goes to orders list
            Back: new BackTarget(
                Visible: true,
                Uri: "/orders"  // Back also goes to orders list
            )
        ));
    }
}
```

### Modal-Like Search Behavior

```csharp
@page "/gallery"
@inject IBottomNavService BottomNav
@inject IDialogService DialogService

@code {
    protected override void OnInitialized()
    {
        BottomNav.Apply(new BottomNavOptions(
            Search: new SearchAction(
                Action: async () =>
                {
                    var dialog = await DialogService.ShowAsync<GallerySearchDialog>();
                    var result = await dialog.Result;
                    
                    if (!result.Canceled)
                    {
                        // Handle search result
                    }
                }
            )
        ));
    }
}
```

### Custom Back Action with Browser History

```csharp
@page "/wizard/step2"
@inject IBottomNavService BottomNav
@inject IJSRuntime JS

@code {
    protected override void OnInitialized()
    {
        BottomNav.Apply(new BottomNavOptions(
            Back: new BackTarget(
                Visible: true,
                Action: async () =>
                {
                    await JS.InvokeVoidAsync("history.back");
                }
            )
        ));
    }
}
```

### Restore Defaults

To restore the navigation bar to its global defaults:

```csharp
BottomNav.ResetToDefaults();
```

Note: The service automatically resets to defaults when navigation occurs (via `NavigationManager.LocationChanged`).

## API Reference

### IBottomNavService

```csharp
public interface IBottomNavService
{
    BottomNavState Current { get; }
    event Action? Changed;
    void Apply(BottomNavOptions? options);
    void ResetToDefaults();
}
```

### BottomNavState

```csharp
public sealed record BottomNavState(
    NavButton Home,
    NavButton Search,
    NavButton Back,
    NavButton Profile
);
```

### NavButton

```csharp
public sealed record NavButton(
    bool Visible,
    bool Enabled,
    Func<Task> OnClick
);
```

### BottomNavOptions

```csharp
public sealed record BottomNavOptions(
    string? HomeUriOverride = null,
    BackTarget? Back = null,
    SearchAction? Search = null
);
```

### BackTarget

```csharp
public sealed record BackTarget(
    bool Visible,
    string? Uri = null,
    Func<Task>? Action = null
);
```

### SearchAction

```csharp
public sealed record SearchAction(
    Func<Task>? Action = null,
    string? Uri = null
);
```

## Behavior Notes

1. **Navigation events**: The service automatically resets to defaults when the user navigates to a new page.
2. **Action precedence**: For Back and Search buttons, if both `Action` and `Uri` are provided, `Action` takes precedence.
3. **State equality**: The service compares options before updating state to avoid unnecessary change notifications.
4. **Thread safety**: The service is registered as scoped and should only be accessed from the Blazor synchronization context.

## Testing

The service includes comprehensive unit tests covering:
- Default state behaviors
- Per-page configuration overrides
- Profile button (always visible)
- Home button with configurable destination
- Back button visibility and action
- Search button with default fallback
- State updates on navigation
- Dispose behavior

See `tests/Interfaces/Interfaces.Base.Tests/Services/BottomNavServiceTests.cs` for examples.
