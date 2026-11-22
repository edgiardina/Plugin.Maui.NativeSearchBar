using CoreGraphics;
using UIKit;

namespace Plugin.Maui.NativeSearchBar;

/// <summary>
/// The platform view for <see cref="NativeSearchBar"/> on iOS. It's empty when the search field
/// lives in the navigation item, and hosts an inline <see cref="UISearchBar"/> otherwise.
/// </summary>
public class SearchBarContainer : UIView
{
	UISearchBar? _inlineBar;

	internal WeakReference<NativeSearchBarHandler>? Handler { get; set; }

	internal UISearchBar? InlineBar
	{
		get => _inlineBar;
		set
		{
			_inlineBar?.RemoveFromSuperview();
			_inlineBar = value;

			if (_inlineBar is not null)
			{
				AddSubview(_inlineBar);
			}
		}
	}

	public override CGSize SizeThatFits(CGSize size) =>
		_inlineBar?.SizeThatFits(size) ?? CGSize.Empty;

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();

		if (_inlineBar is not null)
		{
			_inlineBar.Frame = Bounds;
		}
	}

	public override void MovedToWindow()
	{
		base.MovedToWindow();

		// By the time the page's view is in a window, its controller is in the navigation stack,
		// so this is the reliable point to find the controller that owns the navigation item.
		if (Window is not null && Handler?.TryGetTarget(out var handler) == true)
		{
			handler.OnMovedToWindow();
		}
	}
}
