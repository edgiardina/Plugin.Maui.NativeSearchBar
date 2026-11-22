namespace Plugin.Maui.NativeSearchBar;

public static class MauiAppBuilderExtensions
{
	/// <summary>Registers the native handler for <see cref="NativeSearchBar"/>.</summary>
	public static MauiAppBuilder UseNativeSearchBar(this MauiAppBuilder builder)
	{
#if IOS || MACCATALYST || ANDROID
		builder.ConfigureMauiHandlers(handlers =>
			handlers.AddHandler<NativeSearchBar, NativeSearchBarHandler>());
#endif
		return builder;
	}
}
