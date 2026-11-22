using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace Plugin.Maui.NativeSearchBar;

public partial class NativeSearchBarHandler
{
	UISearchController? _searchController;
	UISearchBar? _inlineBar;
	SearchBarDelegate? _delegate;
	SearchResultsUpdater? _resultsUpdater;
	UIViewController? _host;
	Page? _page;
	bool _toolbarShownByUs;
	bool _updatingText;

	UISearchBar? SearchBar => _searchController?.SearchBar ?? _inlineBar;

	// Toolbar integration is an iPhone-only iOS 26 feature. iPad and Mac keep the field in the
	// navigation bar, so showing a toolbar there would just add an empty bar.
	// A visible tab bar floats over the toolbar, so the field goes to the top instead.
	bool UseBottomToolbar =>
		VirtualView?.IosPlacement == IosSearchBarPlacement.Bottom
		&& OperatingSystem.IsIOSVersionAtLeast(26)
		&& UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone
		&& !IsTabBarVisible
		&& !IsMauiNavigationPage;

	// NavigationPage's renderer copies the page controller's toolbarItems into its own
	// SecondaryToolbar, which crashes on any item without a CustomView. Shell doesn't do that.
	bool IsMauiNavigationPage =>
		_host?.NavigationController is Microsoft.Maui.Controls.Handlers.Compatibility.NavigationRenderer;

	bool IsTabBarVisible
	{
		get
		{
			var tabBarController = _host?.TabBarController;
			return tabBarController is not null && !tabBarController.TabBar.Hidden && !_host!.HidesBottomBarWhenPushed;
		}
	}

	protected override SearchBarContainer CreatePlatformView() => new();

	protected override void ConnectHandler(SearchBarContainer platformView)
	{
		base.ConnectHandler(platformView);

		platformView.Handler = new WeakReference<NativeSearchBarHandler>(this);
		_delegate = new SearchBarDelegate(this);
		_page = FindPage(VirtualView);

		if (_page is not null && IsInNavigationStack(_page))
		{
			_resultsUpdater = new SearchResultsUpdater(this);

			// No results controller: the page's own content shows the results
			_searchController = new UISearchController((UIViewController?)null)
			{
				ObscuresBackgroundDuringPresentation = false,
				SearchResultsUpdater = _resultsUpdater,
			};
			_searchController.SearchBar.Delegate = _delegate;

			_page.Appearing += OnPageAppearing;
			_page.Disappearing += OnPageDisappearing;
		}
		else
		{
			_inlineBar = new UISearchBar
			{
				SearchBarStyle = UISearchBarStyle.Minimal,
				Delegate = _delegate,
			};
			platformView.InlineBar = _inlineBar;
		}
	}

	protected override void DisconnectHandler(SearchBarContainer platformView)
	{
		if (_page is not null)
		{
			_page.Appearing -= OnPageAppearing;
			_page.Disappearing -= OnPageDisappearing;
		}

		HideToolbarIfOurs();

		if (_host is not null && _host.NavigationItem.SearchController == _searchController)
		{
			_host.NavigationItem.SearchController = null;
		}

		if (_searchController is not null)
		{
			_searchController.SearchBar.Delegate = null!;
			_searchController.SearchResultsUpdater = null!;
			_searchController.Dispose();
		}

		if (_inlineBar is not null)
		{
			_inlineBar.Delegate = null!;
			platformView.InlineBar = null;
		}

		platformView.Handler = null;
		_searchController = null;
		_inlineBar = null;
		_delegate = null;
		_resultsUpdater = null;
		_host = null;
		_page = null;

		base.DisconnectHandler(platformView);
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint) =>
		// The navigation item owns the field, so the view takes no space in the page
		_searchController is not null ? Size.Zero : base.GetDesiredSize(widthConstraint, heightConstraint);

	internal void OnMovedToWindow() => Attach();

	void OnPageAppearing(object? sender, EventArgs e) => Attach();

	void OnPageDisappearing(object? sender, EventArgs e) => HideToolbarIfOurs();

	void Attach()
	{
		var pageController = (_page?.Handler as IPlatformViewHandler)?.ViewController;

		if (_searchController is null || pageController is null)
		{
			return;
		}

		var host = ResolveHost(pageController);

		if (_host is not null && _host != host && _host.NavigationItem.SearchController == _searchController)
		{
			_host.NavigationItem.SearchController = null;
		}

		_host = host;
		_host.DefinesPresentationContext = true;

		if (_host.NavigationItem.SearchController != _searchController)
		{
			_host.NavigationItem.SearchController = _searchController;
		}

		ApplyPlacement();

		// Shell applies Shell.TabBarIsVisible for the new page around the same time we attach,
		// so check the tab bar again once that's settled.
		NSOperationQueue.MainQueue.AddOperation(ApplyPlacement);
	}

	void ApplyPlacement()
	{
		if (_host is null)
		{
			return;
		}

		var item = _host.NavigationItem;
		item.HidesSearchBarWhenScrolling = false;

		if (UseBottomToolbar && OperatingSystem.IsIOSVersionAtLeast(26))
		{
			item.PreferredSearchBarPlacement = UINavigationItemSearchBarPlacement.Integrated;
			item.SearchBarPlacementAllowsToolbarIntegration = true;

			// UIKit only moves the field into a toolbar that has items, and this item marks its slot
			if (_host.ToolbarItems is null || _host.ToolbarItems.Length == 0)
			{
				_host.SetToolbarItems([item.SearchBarPlacementBarButtonItem], false);
			}

			ShowToolbar();
		}
		else if (VirtualView?.IosPlacement != IosSearchBarPlacement.Top && OperatingSystem.IsIOSVersionAtLeast(26))
		{
			// A magnifier button in the navigation bar that expands into a field in the same bar.
			// This is also where Bottom ends up when it can't use the toolbar, since the stacked
			// field reads as a separate band between the title and the content.
			item.SearchBarPlacementAllowsToolbarIntegration = false;
			item.PreferredSearchBarPlacement = UINavigationItemSearchBarPlacement.IntegratedButton;
			RemoveOurToolbarItem();
			HideToolbarIfOurs();
		}
		else
		{
			if (OperatingSystem.IsIOSVersionAtLeast(26) || OperatingSystem.IsMacCatalystVersionAtLeast(26))
			{
				item.SearchBarPlacementAllowsToolbarIntegration = false;
			}

			if (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16))
			{
				item.PreferredSearchBarPlacement = UINavigationItemSearchBarPlacement.Stacked;
			}

			RemoveOurToolbarItem();
			HideToolbarIfOurs();
		}
	}

	void RemoveOurToolbarItem()
	{
		if (_host is null || !OperatingSystem.IsIOSVersionAtLeast(26))
		{
			return;
		}

		// Only clear the toolbar if the search slot we added is the only thing in it
		var items = _host.ToolbarItems;
		var searchItem = _host.NavigationItem.SearchBarPlacementBarButtonItem;

		if (items is not null && items.Length == 1 && items[0] == searchItem)
		{
			_host.SetToolbarItems(null, false);
		}
	}

	// The field can only integrate into a toolbar the navigation controller is showing.
	// Shell and NavigationPage both leave it hidden, so show it while this page is up.
	void ShowToolbar()
	{
		if (_host?.NavigationController is { } nav && nav.ToolbarHidden)
		{
			nav.SetToolbarHidden(false, true);
			_toolbarShownByUs = true;
		}
	}

	void HideToolbarIfOurs()
	{
		if (_toolbarShownByUs && _host?.NavigationController is { } nav)
		{
			nav.SetToolbarHidden(true, true);
		}

		_toolbarShownByUs = false;
	}

	// Shell pushes the page's controller directly. NavigationPage wraps it in a parent controller,
	// and that wrapper's navigation item is the one the bar shows.
	static UIViewController ResolveHost(UIViewController pageController)
	{
		var current = pageController;

		while (current.ParentViewController is not null && current.ParentViewController is not UINavigationController)
		{
			current = current.ParentViewController;
		}

		return current.ParentViewController is UINavigationController ? current : pageController;
	}

	static Page? FindPage(Element? element)
	{
		while (element is not null and not Page)
		{
			element = element.Parent;
		}

		return element as Page;
	}

	static bool IsInNavigationStack(Page page)
	{
		for (Element? element = page.Parent; element is not null; element = element.Parent)
		{
			if (element is Shell or NavigationPage)
			{
				return true;
			}
		}

		return false;
	}

	void OnPlatformTextChanged(string? text)
	{
		if (_updatingText || VirtualView is null)
		{
			return;
		}

		_updatingText = true;
		try
		{
			VirtualView.Text = text ?? string.Empty;
		}
		finally
		{
			_updatingText = false;
		}
	}

	public static void MapText(NativeSearchBarHandler handler, NativeSearchBar view)
	{
		if (handler._updatingText || handler.SearchBar is not { } bar)
		{
			return;
		}

		if (bar.Text != view.Text)
		{
			bar.Text = view.Text;
		}
	}

	public static void MapPlaceholder(NativeSearchBarHandler handler, NativeSearchBar view)
	{
		if (handler.SearchBar is { } bar)
		{
			bar.Placeholder = view.Placeholder;
		}
	}

	public static void MapIsTextPredictionEnabled(NativeSearchBarHandler handler, NativeSearchBar view)
	{
		if (handler.SearchBar is not { } bar)
		{
			return;
		}

		bar.AutocorrectionType = view.IsTextPredictionEnabled ? UITextAutocorrectionType.Default : UITextAutocorrectionType.No;
		bar.SpellCheckingType = view.IsTextPredictionEnabled ? UITextSpellCheckingType.Default : UITextSpellCheckingType.No;
	}

	public static void MapIosPlacement(NativeSearchBarHandler handler, NativeSearchBar view) =>
		handler.ApplyPlacement();

	static void MapFocus(NativeSearchBarHandler handler, NativeSearchBar view, object? args)
	{
		if (handler._searchController is { } controller)
		{
			controller.Active = true;

			// Activating and focusing in the same pass is ignored while the controller is still presenting
			NSOperationQueue.MainQueue.AddOperation(() => controller.SearchBar.BecomeFirstResponder());
			CompleteFocusRequest(args, true);
		}
		else
		{
			CompleteFocusRequest(args, handler._inlineBar?.BecomeFirstResponder() == true);
		}
	}

	static void MapUnfocus(NativeSearchBarHandler handler, NativeSearchBar view, object? args)
	{
		handler.SearchBar?.ResignFirstResponder();
	}

	sealed class SearchResultsUpdater : NSObject, IUISearchResultsUpdating
	{
		readonly WeakReference<NativeSearchBarHandler> _handler;

		public SearchResultsUpdater(NativeSearchBarHandler handler) => _handler = new(handler);

		// Runs on every edit and also when the controller is cancelled, which clears the text
		public void UpdateSearchResultsForSearchController(UISearchController searchController)
		{
			if (_handler.TryGetTarget(out var handler))
			{
				handler.OnPlatformTextChanged(searchController.SearchBar.Text);
			}
		}
	}

	sealed class SearchBarDelegate : UISearchBarDelegate
	{
		readonly WeakReference<NativeSearchBarHandler> _handler;

		public SearchBarDelegate(NativeSearchBarHandler handler) => _handler = new(handler);

		public override void TextChanged(UISearchBar searchBar, string searchText)
		{
			// The search controller reports edits through its results updater instead
			if (_handler.TryGetTarget(out var handler) && handler._inlineBar == searchBar)
			{
				handler.OnPlatformTextChanged(searchText);
			}
		}

		public override void SearchButtonClicked(UISearchBar searchBar)
		{
			// Drop the keyboard but leave the search active, like Mail
			searchBar.ResignFirstResponder();

			if (_handler.TryGetTarget(out var handler))
			{
				handler.VirtualView?.SendSearchButtonPressed();
			}
		}

		public override void OnEditingStarted(UISearchBar searchBar)
		{
			if (IsInline(searchBar))
			{
				searchBar.SetShowsCancelButton(true, true);
			}
		}

		public override void OnEditingStopped(UISearchBar searchBar)
		{
			if (IsInline(searchBar))
			{
				searchBar.SetShowsCancelButton(false, true);
			}
		}

		public override void CancelButtonClicked(UISearchBar searchBar)
		{
			if (!IsInline(searchBar) || !_handler.TryGetTarget(out var handler))
			{
				return;
			}

			searchBar.Text = string.Empty;
			handler.OnPlatformTextChanged(string.Empty);
			searchBar.ResignFirstResponder();
		}

		bool IsInline(UISearchBar searchBar) =>
			_handler.TryGetTarget(out var handler) && handler._inlineBar == searchBar;
	}
}
