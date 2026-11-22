using System.Collections.ObjectModel;
using Plugin.Maui.NativeSearchBar;

namespace NativeSearchBarSample;

public class SearchPage : ContentPage, IQueryAttributable
{
	static readonly string[] AllNames =
	[
		"Addams Family", "Attack from Mars", "Black Knight", "Cactus Canyon", "Creature from the Black Lagoon",
		"Demolition Man", "Eight Ball Deluxe", "Fireball", "Funhouse", "Godzilla", "Indiana Jones",
		"Junk Yard", "Medieval Madness", "Monster Bash", "No Good Gofers", "Paragon", "Revenge from Mars",
		"Scared Stiff", "Star Trek: The Next Generation", "Swords of Fury", "Taxi", "Theatre of Magic",
		"Twilight Zone", "White Water", "World Cup Soccer", "Xenon",
	];

	readonly ObservableCollection<string> _results = new(AllNames);
	readonly NativeSearchBar _searchBar;
	readonly Label _status;

	public SearchPage() : this(IosSearchBarPlacement.Bottom, showClose: false)
	{
	}

	public SearchPage(IosSearchBarPlacement placement, bool showClose, bool closeToolbarItem = false)
	{
		Title = "Search";

		if (closeToolbarItem)
		{
			ToolbarItems.Add(new ToolbarItem("Done", null, async () => await Navigation.PopModalAsync()));
		}

		_searchBar = new NativeSearchBar
		{
			Placeholder = "Search machines",
			IsTextPredictionEnabled = false,
			IosPlacement = placement,
			Margin = DeviceInfo.Platform == DevicePlatform.Android ? new Thickness(16, 8) : 0,
			AutomationId = "SearchBar",
		};
		_searchBar.TextChanged += (_, e) => Filter(e.NewTextValue);
		_searchBar.SearchButtonPressed += (_, _) => _status!.Text = $"Searched for \"{_searchBar.Text}\"";

		_status = new Label { Margin = new Thickness(16, 4), FontSize = 12, TextColor = Colors.Gray, AutomationId = "Status" };

		var list = new CollectionView
		{
			ItemsSource = _results,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label { Padding = new Thickness(16, 12), FontSize = 17 };
				label.SetBinding(Label.TextProperty, ".");
				return label;
			}),
		};

		var grid = new Grid
		{
			RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star)],
		};
		grid.Add(_searchBar, 0, 0);
		grid.Add(_status, 0, 1);
		grid.Add(list, 0, 2);

		if (showClose)
		{
			var close = new Button { Text = "Close", HorizontalOptions = LayoutOptions.End, Margin = new Thickness(16, 48, 16, 0) };
			close.Clicked += async (_, _) => await Navigation.PopModalAsync();
			grid.RowDefinitions.Insert(0, new RowDefinition(GridLength.Auto));
			grid.Children.Clear();
			grid.Add(close, 0, 0);
			grid.Add(_searchBar, 0, 1);
			grid.Add(_status, 0, 2);
			grid.Add(list, 0, 3);
		}

		Content = grid;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("placement", out var placement) && Enum.TryParse<IosSearchBarPlacement>(placement?.ToString(), out var value))
		{
			_searchBar.IosPlacement = value;
		}

		if (query.TryGetValue("activate", out var activate))
		{
			_searchBar.ActivatesOnAppearing = activate?.ToString() == "true";
		}

		if (query.TryGetValue("tabbar", out var tabbar))
		{
			Shell.SetTabBarIsVisible(this, tabbar?.ToString() == "true");
		}
	}

	void Filter(string? text)
	{
		_results.Clear();

		foreach (var name in AllNames.Where(n => string.IsNullOrWhiteSpace(text) || n.Contains(text, StringComparison.OrdinalIgnoreCase)))
		{
			_results.Add(name);
		}
	}
}
