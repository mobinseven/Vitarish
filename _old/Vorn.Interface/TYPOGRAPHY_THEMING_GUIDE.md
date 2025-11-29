# Typography Theming Guide

This guide explains how to control MudBlazor component font sizes through theme settings in Vorn.Interface.

## Overview

The typography theme system allows you to control font sizes across all MudBlazor components from a single centralized location. This makes it easy to:

- Apply different UI scales (compact, default, comfortable, large)
- Meet accessibility requirements (WCAG text size guidelines)
- Keep typography consistent across all components
- Switch between typography presets without custom CSS

## Using ThemeForm Component

The easiest way to adjust typography settings is through the **ThemeForm** component, which provides:

- **Typography Preset Selector**: Quick selection of predefined scales (Compact, Default, Comfortable, Large)
- **Font Size Slider**: Fine-tune font size from 0.5rem to 2rem
- **Font Weight Slider**: Adjust font weight from 100 to 1000
- **Live Preview**: See changes immediately in the preview section
- **Current Settings Display**: Shows active font size and weight

```razor
@* Add ThemeForm to your page *@
<ThemeForm />
```

The ThemeForm component automatically updates all MudBlazor components when you change typography settings, with changes visible instantly in the preview section.

## How It Works

The system uses CSS custom properties (CSS variables) to propagate theme typography settings to MudBlazor components:

1. **ThemeService** manages typography settings (font size, weight, line height)
2. **TypographyVariables** class converts these into CSS custom properties
3. **MainLayout** injects CSS variables into the page's `:root` element
4. **CSS overrides** ensure all MudBlazor components use these CSS variables

## Quick Start

### Setting Typography Theme

```csharp
@inject IThemeService ThemeService

// In your component or service
ThemeService.UpdateTypography(
    fontSize: "1rem",      // Base font size
    fontWeight: "500"      // Base font weight
);
```

### Predefined Typography Scales

#### Compact UI (Dense layouts)
```csharp
ThemeService.UpdateTypography(fontSize: "0.875rem", fontWeight: "400");
```
- Font size: 14px
- Best for: Data-heavy applications, dashboards with many components

#### Default UI (Standard)
```csharp
ThemeService.UpdateTypography(fontSize: "1rem", fontWeight: "500");
```
- Font size: 16px (browser default)
- Best for: General-purpose applications

#### Comfortable UI (Spacious)
```csharp
ThemeService.UpdateTypography(fontSize: "1.125rem", fontWeight: "500");
```
- Font size: 18px
- Best for: Content-focused applications, longer reading sessions

#### Large UI (Accessibility)
```csharp
ThemeService.UpdateTypography(fontSize: "1.25rem", fontWeight: "500");
```
- Font size: 20px
- Best for: Accessibility requirements, vision-impaired users, public kiosks

### Advanced: Custom Scales

You can define custom typography scales for specific use cases:

```csharp
public static class TypographyPresets
{
    public static void ApplyCompact(IThemeService theme)
        => theme.UpdateTypography(fontSize: "0.875rem", fontWeight: "400");
    
    public static void ApplyDefault(IThemeService theme)
        => theme.UpdateTypography(fontSize: "1rem", fontWeight: "500");
    
    public static void ApplyComfortable(IThemeService theme)
        => theme.UpdateTypography(fontSize: "1.125rem", fontWeight: "500");
    
    public static void ApplyLarge(IThemeService theme)
        => theme.UpdateTypography(fontSize: "1.25rem", fontWeight: "500");
    
    public static void ApplyExtraLarge(IThemeService theme)
        => theme.UpdateTypography(fontSize: "1.5rem", fontWeight: "600");
}

// Usage
TypographyPresets.ApplyComfortable(ThemeService);
```

## CSS Variables Reference

The following CSS custom properties are set by the theme system:

| Variable | Description | Default Value |
|----------|-------------|---------------|
| `--mud-typography-default-size` | Base font size for components | `1rem` |
| `--mud-typography-default-weight` | Base font weight | `500` |
| `--mud-typography-default-lineheight` | Base line height | `1.5` |

### Using CSS Variables Directly

You can reference these variables in your own custom CSS:

```css
.my-custom-component {
    font-size: var(--mud-typography-default-size);
    font-weight: var(--mud-typography-default-weight);
    line-height: var(--mud-typography-default-lineheight);
}

.my-large-text {
    font-size: calc(var(--mud-typography-default-size) * 1.5);
}

.my-small-text {
    font-size: calc(var(--mud-typography-default-size) * 0.875);
}
```

## Supported Components

The following MudBlazor components automatically respond to typography theme changes:

### Form & Input Components
- MudTextField
- MudSelect
- MudAutocomplete
- MudNumericField
- MudTextArea
- Input labels and helper text

### Action Components
- MudButton (all variants)
- MudIconButton
- MudFab

### Data Display Components
- MudTable
- MudDataGrid
- MudList
- MudListItem

### Navigation Components
- MudMenu
- MudMenuItem
- MudNavLink
- MudTabs
- MudBreadcrumbs
- MudDrawer

### Feedback Components
- MudAlert
- MudSnackbar
- MudTooltip
- MudDialog

### Other Components
- MudChip
- MudBadge
- MudExpansionPanel
- MudTimeline
- MudPagination
- MudRating
- MudAvatar
- MudAppBar

## Theme Persistence

To save and load typography settings:

```csharp
// Save theme settings including typography
var json = ThemeService.GetThemeSettingsJson();
// Store json in localStorage, database, etc.

// Load theme settings including typography
ThemeService.LoadThemeSettingsJson(json);
```

The JSON includes typography settings:

```json
{
  "typography": {
    "fontSize": "1rem",
    "fontWeight": "500"
  }
}
```

## Best Practices

### 1. Choose Appropriate Base Sizes

- **Never go below 0.75rem** (12px) - this hurts readability
- **Standard web default is 1rem** (16px) - widely tested and accessible
- **Consider WCAG Level AA** - requires text to be resizable up to 200%

### 2. Test Across Breakpoints

Different screen sizes may benefit from different scales:

```csharp
@code {
    private void UpdateTypographyForBreakpoint(Breakpoint breakpoint)
    {
        switch (breakpoint)
        {
            case Breakpoint.Xs:
            case Breakpoint.Sm:
                ThemeService.UpdateTypography(fontSize: "0.875rem");
                break;
            case Breakpoint.Md:
            case Breakpoint.Lg:
                ThemeService.UpdateTypography(fontSize: "1rem");
                break;
            case Breakpoint.Xl:
            case Breakpoint.Xxl:
                ThemeService.UpdateTypography(fontSize: "1.125rem");
                break;
        }
    }
}
```

### 3. Consider User Preferences

Allow users to choose their preferred typography scale:

```razor
@using Microsoft.JSInterop

<MudSelect T="string" 
           Label="Text Size" 
           Value="@selectedScale"
           ValueChanged="@OnScaleChanged">
    <MudSelectItem Value="@("compact")">Compact</MudSelectItem>
    <MudSelectItem Value="@("default")">Default</MudSelectItem>
    <MudSelectItem Value="@("comfortable")">Comfortable</MudSelectItem>
    <MudSelectItem Value="@("large")">Large</MudSelectItem>
</MudSelect>

@code {
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    
    private string selectedScale = "default";
    
    private async Task OnScaleChanged(string scale)
    {
        selectedScale = scale;
        
        switch (scale)
        {
            case "compact":
                ThemeService.UpdateTypography(fontSize: "0.875rem");
                break;
            case "comfortable":
                ThemeService.UpdateTypography(fontSize: "1.125rem");
                break;
            case "large":
                ThemeService.UpdateTypography(fontSize: "1.25rem");
                break;
            default:
                ThemeService.UpdateTypography(fontSize: "1rem");
                break;
        }
        
        // Optionally save preference using JSInterop
        await JSRuntime.InvokeVoidAsync("localStorage.setItem", "preferredTextScale", scale);
    }
}
```

## Troubleshooting

### Components Not Responding to Theme Changes

1. **Verify CSS is loaded**: Check browser DevTools to ensure MainLayout.razor.css is loaded
2. **Check CSS specificity**: Ensure no other styles are overriding with `!important`
3. **Verify theme is applied**: Check that `:root` element has CSS variables set
4. **Force refresh**: Sometimes browser cache needs to be cleared

### Font Sizes Look Wrong

1. **Check base font size**: Default browser font size affects `rem` calculations
2. **Review cascade**: Other CSS might be affecting specific components
3. **Test in isolation**: Create minimal reproduction to identify conflicts

### Typography Not Persisting

1. **Verify save/load**: Ensure `GetThemeSettingsJson()` and `LoadThemeSettingsJson()` are called
2. **Check JSON structure**: Verify typography object is in saved JSON
3. **Initialize on startup**: Load saved theme settings during app initialization

## Migration from Fixed Font Sizes

If you have existing CSS with fixed font sizes, you can migrate gradually:

### Before:
```css
.my-component {
    font-size: 14px;
}
```

### After:
```css
.my-component {
    font-size: calc(var(--mud-typography-default-size) * 0.875);
}
```

## Additional Resources

- [MudBlazor Typography Documentation](https://mudblazor.com/features/typography)
- [WCAG 2.1 Text Size Guidelines](https://www.w3.org/WAI/WCAG21/Understanding/resize-text.html)
- [CSS Custom Properties (MDN)](https://developer.mozilla.org/en-US/docs/Web/CSS/--*)

## Summary

The typography theming system provides:

✅ **Centralized control** - Change one setting, affect all components  
✅ **Easy scaling** - Predefined scales for common scenarios  
✅ **CSS variables** - Modern, performant, and extensible  
✅ **Comprehensive coverage** - Supports all major MudBlazor components  
✅ **Theme persistence** - Save and restore user preferences  
✅ **Accessibility ready** - Meet WCAG requirements easily  
✅ **No breaking changes** - Works with existing MudBlazor code  

For questions or issues, please refer to the project repository.
