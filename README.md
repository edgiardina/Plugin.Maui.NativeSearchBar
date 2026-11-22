# Plugin.Maui.NativeSearchBar

A .NET MAUI search bar that uses the search UI of each platform.

- **iOS 26+ on iPhone**: The search field floats in the bottom toolbar, like Mail. When the user taps it, it moves up and docks above the keyboard.
- **Earlier iOS versions, iPad, and Mac Catalyst**: The search field is in the navigation bar, below the title.
- **Android**: A Material 3 search bar. It is a 56dp pill with a search icon and a clear button.

The view takes no space in the page layout on iOS, because the navigation item owns the field. Put the results in the page as usual, for example in a `CollectionView`.

## Install

```
dotnet add package Plugin.Maui.NativeSearchBar
```

Register the handler in `MauiProgram.cs`:

```csharp
builder
    .UseMauiApp<App>()
    .UseNativeSearchBar();
```

## Use

```xml
<ContentPage xmlns:nsb="clr-namespace:Plugin.Maui.NativeSearchBar;assembly=Plugin.Maui.NativeSearchBar"
             Shell.TabBarIsVisible="False">
    <Grid RowDefinitions="Auto,*">
        <nsb:NativeSearchBar Text="{Binding SearchText}"
                             Placeholder="Search players"
                             SearchCommand="{Binding SearchCommand}"
                             IsTextPredictionEnabled="False"
                             Margin="{OnPlatform Android='16,8', Default=0}" />
        <CollectionView Grid.Row="1" ItemsSource="{Binding Results}" />
    </Grid>
</ContentPage>
```

| Property | Description |
| --- | --- |
| `Text` | The search text. The default binding mode is two-way. |
| `Placeholder` | The hint text in the empty field. |
| `SearchCommand`, `SearchCommandParameter` | The command that runs when the user taps the search key on the keyboard. |
| `IsTextPredictionEnabled` | Set to `False` to turn off autocorrect and suggestions. Use this for names. |
| `IosPlacement` | `Bottom` (default) or `Top`. Android ignores this property. |

| Event | Description |
| --- | --- |
| `TextChanged` | The text changed. |
| `SearchButtonPressed` | The user tapped the search key on the keyboard. |

`Focus()` starts the search and shows the keyboard. `Unfocus()` closes the keyboard.

On Android, `BackgroundColor` sets the color of the pill.

## iOS placement rules

The plugin puts a `UISearchController` on the navigation item of the page. The placement follows these rules:

1. If the page is not in a `Shell` or a `NavigationPage`, the plugin shows an inline `UISearchBar` in the layout.
2. If `IosPlacement` is `Bottom`, the device is an iPhone with iOS 26 or later, and no tab bar shows, the field goes into the bottom toolbar. The plugin shows the toolbar of the navigation controller while the page is visible. It hides the toolbar again when the page disappears.
3. In all other conditions, the field goes below the title in the navigation bar.

A visible tab bar floats over the bottom toolbar. Thus, the plugin moves the field to the top when a tab bar shows. To get the bottom field on a page in a tab, set `Shell.TabBarIsVisible="False"` on the page.

Do not use a Shell `SearchHandler` on the same page. Both use the same navigation item.

## Android notes

Material 3 puts search at the top of the screen. The plugin does not open a full-screen `SearchView`. The user types in the pill, and the page shows the results.

MAUI apps use a MaterialComponents (M2) theme by default. That theme does not define the M3 surface colors. In that condition, the plugin uses neutral M3 tones. If the app theme is M3, the plugin uses the theme colors.

## Sample

The `samples` folder has a Shell app with four cases: bottom with the tab bar hidden, bottom with the tab bar visible, top, and a modal page with no navigation.

## License

MIT
