using Plugin.Maui.NativeSearchBar;

namespace NativeSearchBarSample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseNativeSearchBar();

		return builder.Build();
	}
}

public class App : Application
{
	protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell());
}

public class AppShell : Shell
{
	public AppShell()
	{
		Items.Add(new TabBar
		{
			Items =
			{
				new ShellContent { Title = "Home", Route = "home", ContentTemplate = new DataTemplate(typeof(HomePage)) },
				new ShellContent { Title = "Settings", Route = "settings", ContentTemplate = new DataTemplate(() => new ContentPage { Title = "Settings" }) },
			},
		});

		Routing.RegisterRoute("search", typeof(SearchPage));
	}
}
