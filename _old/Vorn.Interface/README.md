# Vorn.Interface

This library provides common interface components and services for Blazor applications using MudBlazor.

## Features

### MainLayout

A responsive main layout component with:
- Theme support (light/dark mode)
- Breakpoint detection
- Render notifications with snackbar
- Integration with MudBlazor components

### SearchService

A service for handling search functionality across components:
- Event-based search notifications
- Last search query tracking
- Enable/disable state management

### ThemeService

Manages MudBlazor themes with:
- Pre-configured light and dark palettes
- Customizable colors and typography
- **Centralized font size control** via CSS custom properties
- Glassmorphism surface effects
- Dynamic theme switching

**Typography Features:**
- Control font sizes for all MudBlazor components from theme settings
- Built-in support for compact, default, comfortable, and large UI scales
- CSS variables enable consistent typography across all components
- See [Typography Theming Guide](TYPOGRAPHY_THEMING_GUIDE.md) for detailed usage

### Other Components

- `NavMenu`: Navigation menu component
- `FocusService`: Manages focus states
- `PwaInstall`: PWA installation prompts
- `StrictConfirmDialog`: Confirmation dialogs
- `ExpandableItem`: Expandable UI items
- `EndOfScroll`: Scroll detection utilities

## Usage

1. Add the package reference:

```xml
<PackageReference Include="Vorn.Common.Interface" Version="*" />
```

2. Configure services in `Program.cs`:

```csharp
builder.Services.AddMudServices();
builder.Services.AddScoped<ISearchService, SearchService>();
// Other services are registered via attributes
```

3. Use in your components:

```razor
@inherits MainLayout

<MudThemeProvider Theme="@Theme" />
<MudDialogProvider />
<MudSnackbarProvider />

@Body
```

4. Inject services as needed:

```csharp
@inject IThemeService ThemeService
@inject ISearchService SearchService
```

## Dependencies

- MudBlazor
- Microsoft.AspNetCore.Components.Web
- Injectio (for service registration)