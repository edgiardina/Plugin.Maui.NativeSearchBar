#if IOS || MACCATALYST || ANDROID
using Microsoft.Maui.Handlers;

#if IOS || MACCATALYST
using PlatformView = Plugin.Maui.NativeSearchBar.SearchBarContainer;
#else
using PlatformView = Plugin.Maui.NativeSearchBar.MaterialSearchBarView;
#endif

namespace Plugin.Maui.NativeSearchBar;

public partial class NativeSearchBarHandler : ViewHandler<NativeSearchBar, PlatformView>
{
	public static readonly IPropertyMapper<NativeSearchBar, NativeSearchBarHandler> Mapper =
		new PropertyMapper<NativeSearchBar, NativeSearchBarHandler>(ViewMapper)
		{
			[nameof(NativeSearchBar.Text)] = MapText,
			[nameof(NativeSearchBar.Placeholder)] = MapPlaceholder,
			[nameof(NativeSearchBar.IsTextPredictionEnabled)] = MapIsTextPredictionEnabled,
			[nameof(NativeSearchBar.IosPlacement)] = MapIosPlacement,
#if ANDROID
			// The pill draws its own background, so don't let the base mapper replace it
			[nameof(IView.Background)] = MapBackground,
#endif
		};

	public static readonly CommandMapper<NativeSearchBar, NativeSearchBarHandler> CommandMapper =
		new(ViewCommandMapper)
		{
			[nameof(IView.Focus)] = MapFocus,
			[nameof(IView.Unfocus)] = MapUnfocus,
		};

	public NativeSearchBarHandler() : base(Mapper, CommandMapper)
	{
	}

	static void CompleteFocusRequest(object? args, bool result)
	{
		if (args is FocusRequest request)
		{
			request.TrySetResult(result);
		}
	}
}
#endif
