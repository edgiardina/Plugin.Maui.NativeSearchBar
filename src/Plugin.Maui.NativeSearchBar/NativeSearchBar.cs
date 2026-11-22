using System.Windows.Input;

namespace Plugin.Maui.NativeSearchBar;

/// <summary>
/// A search field that uses each platform's own search UI.
/// </summary>
/// <remarks>
/// On iOS and Mac Catalyst the field lives in the navigation bar through a <c>UISearchController</c>,
/// so the view itself takes no space in the layout. On iOS 26+ iPhone it moves to the bottom toolbar
/// and docks to the keyboard while editing, like Mail. Pages that aren't in a navigation stack fall
/// back to an inline <c>UISearchBar</c>.
/// On Android it renders a Material 3 search bar: a full-height pill with a leading search icon
/// and a trailing clear button.
/// </remarks>
public class NativeSearchBar : View
{
	public static readonly BindableProperty TextProperty = BindableProperty.Create(
		nameof(Text), typeof(string), typeof(NativeSearchBar), string.Empty, BindingMode.TwoWay,
		propertyChanged: (b, o, n) => ((NativeSearchBar)b).TextChanged?.Invoke(b, new TextChangedEventArgs((string)o, (string)n)));

	public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
		nameof(Placeholder), typeof(string), typeof(NativeSearchBar), string.Empty);

	public static readonly BindableProperty SearchCommandProperty = BindableProperty.Create(
		nameof(SearchCommand), typeof(ICommand), typeof(NativeSearchBar));

	public static readonly BindableProperty SearchCommandParameterProperty = BindableProperty.Create(
		nameof(SearchCommandParameter), typeof(object), typeof(NativeSearchBar));

	public static readonly BindableProperty IsTextPredictionEnabledProperty = BindableProperty.Create(
		nameof(IsTextPredictionEnabled), typeof(bool), typeof(NativeSearchBar), true);

	public static readonly BindableProperty IosPlacementProperty = BindableProperty.Create(
		nameof(IosPlacement), typeof(IosSearchBarPlacement), typeof(NativeSearchBar), IosSearchBarPlacement.Bottom);

	/// <summary>The search text. Two-way by default.</summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public string Placeholder
	{
		get => (string)GetValue(PlaceholderProperty);
		set => SetValue(PlaceholderProperty, value);
	}

	/// <summary>Runs when the user taps the keyboard's search key.</summary>
	public ICommand? SearchCommand
	{
		get => (ICommand?)GetValue(SearchCommandProperty);
		set => SetValue(SearchCommandProperty, value);
	}

	public object? SearchCommandParameter
	{
		get => GetValue(SearchCommandParameterProperty);
		set => SetValue(SearchCommandParameterProperty, value);
	}

	/// <summary>Turns autocorrect and keyboard suggestions on or off. Names usually want this off.</summary>
	public bool IsTextPredictionEnabled
	{
		get => (bool)GetValue(IsTextPredictionEnabledProperty);
		set => SetValue(IsTextPredictionEnabledProperty, value);
	}

	/// <summary>
	/// Where the field goes on iOS. <see cref="IosSearchBarPlacement.Bottom"/> uses the bottom toolbar
	/// on iOS 26+ iPhone and falls back to the top elsewhere. Ignored on Android.
	/// </summary>
	public IosSearchBarPlacement IosPlacement
	{
		get => (IosSearchBarPlacement)GetValue(IosPlacementProperty);
		set => SetValue(IosPlacementProperty, value);
	}

	public event EventHandler<TextChangedEventArgs>? TextChanged;

	public event EventHandler? SearchButtonPressed;

	/// <summary>Called by the handlers when the keyboard's search key is tapped.</summary>
	public void SendSearchButtonPressed()
	{
		SearchButtonPressed?.Invoke(this, EventArgs.Empty);

		if (SearchCommand?.CanExecute(SearchCommandParameter) == true)
		{
			SearchCommand.Execute(SearchCommandParameter);
		}
	}
}

public enum IosSearchBarPlacement
{
	/// <summary>Bottom toolbar on iOS 26+ iPhone, docked to the keyboard while editing. Top elsewhere.</summary>
	Bottom,

	/// <summary>Always under the navigation bar title.</summary>
	Top,
}
