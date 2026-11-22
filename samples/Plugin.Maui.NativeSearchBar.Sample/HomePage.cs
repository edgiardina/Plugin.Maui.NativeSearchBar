using Plugin.Maui.NativeSearchBar;

namespace NativeSearchBarSample;

public class HomePage : ContentPage
{
	public HomePage()
	{
		Title = "Native Search";

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 12,
			Children =
			{
				Button("Search (bottom, tab bar hidden)", () => Shell.Current.GoToAsync("search?placement=Bottom&tabbar=false")),
				Button("Search (bottom, tab bar visible)", () => Shell.Current.GoToAsync("search?placement=Bottom&tabbar=true")),
				Button("Search (top)", () => Shell.Current.GoToAsync("search?placement=Top&tabbar=true")),
				Button("Search (modal NavigationPage)", () => Navigation.PushModalAsync(new NavigationPage(new SearchPage(IosSearchBarPlacement.Bottom, showClose: false, closeToolbarItem: true)))),
				Button("Search (modal, no navigation)", () => Navigation.PushModalAsync(new SearchPage(IosSearchBarPlacement.Bottom, showClose: true))),
			},
		};
	}

	static Button Button(string text, Func<Task> action)
	{
		var button = new Button { Text = text, AutomationId = text };
		button.Clicked += async (_, _) => await action();
		return button;
	}
}
