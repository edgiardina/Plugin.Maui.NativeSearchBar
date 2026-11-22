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

	public static readonly BindableProperty ActivatesOnAppearingProperty = BindableProperty.Create(
		nameof(ActivatesOnAppearing), typeof(bool), typeof(NativeSearchBar), false);

	public static readonly BindableProperty IosPlacementProperty = BindableProperty.Create(
		nameof(IosPlacement), typeof(IosSearchBarPlacement), typeof(NativeSearchBar), IosSearchBarPlacement.Bottom);

	public static readonly BindableProperty AndroidPlacementProperty = BindableProperty.Create(
		nameof(AndroidPlacement), typeof(AndroidSearchBarPlacement), typeof(NativeSearchBar), AndroidSearchBarPlacement.AppBar);

	/// <summary>
	/// Where the field goes on Android. <see cref="AndroidSearchBarPlacement.AppBar"/> replaces the
	/// page title in the top app bar. Ignored on iOS.
	/// </summary>
	public AndroidSearchBarPlacement AndroidPlacement
	{
		get => (AndroidSearchBarPlacement)GetValue(AndroidPlacementProperty);
		set => SetValue(AndroidPlacementProperty, value);
	}

	// The copy of this bar that sits in the app bar as the page's TitleView, and the flag that marks
	// a bar as being that copy. The original stays in the page with zero size and forwards to it.
	internal NativeSearchBar? AppBarCopy { get; private set; }

	internal bool IsAppBarCopy { get; private init; }

	Page? _titleViewPage;

	/// <summary>
	/// Puts a bound copy of this bar into the page's title view. Returns false when the page isn't in
	/// a Shell or NavigationPage, or already has a title view of its own.
	/// </summary>
	internal bool TryMoveToAppBar()
	{
		if (IsAppBarCopy || AppBarCopy is not null)
		{
			return AppBarCopy is not null;
		}

		if (AndroidPlacement != AndroidSearchBarPlacement.AppBar)
		{
			return false;
		}

		var page = FindPage();
		if (page is null)
		{
			return false;
		}

		var inShell = false;
		var inNavigationPage = false;
		for (Element? element = page.Parent; element is not null; element = element.Parent)
		{
			inShell |= element is Shell;
			inNavigationPage |= element is NavigationPage;
		}

		if (inShell && Shell.GetTitleView(page) is null)
		{
			Shell.SetTitleView(page, CreateAppBarCopy());
		}
		else if (!inShell && inNavigationPage && NavigationPage.GetTitleView(page) is null)
		{
			NavigationPage.SetTitleView(page, CreateAppBarCopy());
		}
		else
		{
			return false;
		}

		_titleViewPage = page;
		InvalidateMeasure();
		return true;
	}

	NativeSearchBar CreateAppBarCopy()
	{
		// The toolbar gives the title view its full height and runs it to the edge, so inset it
		var copy = new NativeSearchBar { IsAppBarCopy = true, Margin = new Thickness(0, 6, 12, 6) };
		copy.SetBinding(TextProperty, new Binding(nameof(Text), BindingMode.TwoWay, source: this));
		copy.SetBinding(PlaceholderProperty, new Binding(nameof(Placeholder), source: this));
		copy.SetBinding(IsTextPredictionEnabledProperty, new Binding(nameof(IsTextPredictionEnabled), source: this));
		copy.SetBinding(BackgroundProperty, new Binding(nameof(Background), source: this));

		// The command and events stay on this bar, so callers only ever deal with the one they declared
		copy.SearchButtonPressed += (_, _) => SendSearchButtonPressed();

		AppBarCopy = copy;
		return copy;
	}

	internal void RemoveFromAppBar()
	{
		if (_titleViewPage is not null && AppBarCopy is not null)
		{
			if (Shell.GetTitleView(_titleViewPage) == AppBarCopy)
			{
				Shell.SetTitleView(_titleViewPage, null);
			}

			if (NavigationPage.GetTitleView(_titleViewPage) == AppBarCopy)
			{
				NavigationPage.SetTitleView(_titleViewPage, null);
			}
		}

		AppBarCopy = null;
		_titleViewPage = null;
	}

	Page? FindPage()
	{
		Element? element = Parent;
		while (element is not null && element is not Page)
		{
			element = element.Parent;
		}

		return element as Page;
	}

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
	/// Where the field goes on iOS. See <see cref="IosSearchBarPlacement"/>. Ignored on Android.
	/// </summary>
	public IosSearchBarPlacement IosPlacement
	{
		get => (IosSearchBarPlacement)GetValue(IosPlacementProperty);
		set => SetValue(IosPlacementProperty, value);
	}

	/// <summary>
	/// Starts the search and shows the keyboard the first time the page is navigated to,
	/// like a search tab does. Use it on pages whose only job is search.
	/// </summary>
	public bool ActivatesOnAppearing
	{
		get => (bool)GetValue(ActivatesOnAppearingProperty);
		set => SetValue(ActivatesOnAppearingProperty, value);
	}

	Page? _page;
	bool _hasActivated;

	public NativeSearchBar()
	{
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	void OnLoaded(object? sender, EventArgs e)
	{
		if (FindPage() is Page page && page != _page)
		{
			OnUnloaded(sender, e);
			_page = page;
			_page.NavigatedTo += OnPageNavigatedTo;
		}
	}

	void OnUnloaded(object? sender, EventArgs e)
	{
		if (_page is not null)
		{
			_page.NavigatedTo -= OnPageNavigatedTo;
			_page = null;
		}
	}

	// NavigatedTo fires once the push animation is done, which is when iOS will accept
	// activating a search controller. Only the first time: coming back from a result
	// shouldn't pop the keyboard up again.
	void OnPageNavigatedTo(object? sender, NavigatedToEventArgs e)
	{
		if (ActivatesOnAppearing && !_hasActivated)
		{
			_hasActivated = true;
			Focus();
		}
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

public enum AndroidSearchBarPlacement
{
	/// <summary>
	/// In the top app bar in place of the title (Material 3's search bar as the app bar).
	/// Falls back to <see cref="Inline"/> when the page has no app bar or its own title view.
	/// </summary>
	AppBar,

	/// <summary>Where the view is in the page layout.</summary>
	Inline,
}

public enum IosSearchBarPlacement
{
	/// <summary>
	/// Bottom toolbar on iOS 26+ iPhone, docked to the keyboard while editing. Where the toolbar isn't
	/// available (a visible tab bar, iPad, NavigationPage) it falls back to <see cref="NavigationBarButton"/>.
	/// </summary>
	Bottom,

	/// <summary>A field under the navigation bar title, always visible.</summary>
	Top,

	/// <summary>
	/// On iOS 26+, a search button in the navigation bar that expands into a field in the same bar.
	/// Earlier versions use <see cref="Top"/>.
	/// </summary>
	NavigationBarButton,
}
