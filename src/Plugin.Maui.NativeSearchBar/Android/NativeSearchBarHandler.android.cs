using Android.Views;
using Android.Views.InputMethods;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;

namespace Plugin.Maui.NativeSearchBar;

public partial class NativeSearchBarHandler
{
	bool _updatingText;

	protected override MaterialSearchBarView CreatePlatformView() => new(Context);

	protected override void ConnectHandler(MaterialSearchBarView platformView)
	{
		base.ConnectHandler(platformView);

		platformView.EditText.TextChanged += OnTextChanged;
		platformView.EditText.EditorAction += OnEditorAction;
		platformView.ClearButton.Click += OnClearClicked;
	}

	protected override void DisconnectHandler(MaterialSearchBarView platformView)
	{
		platformView.EditText.TextChanged -= OnTextChanged;
		platformView.EditText.EditorAction -= OnEditorAction;
		platformView.ClearButton.Click -= OnClearClicked;

		base.DisconnectHandler(platformView);
	}

	void OnTextChanged(object? sender, global::Android.Text.TextChangedEventArgs e)
	{
		var text = PlatformView.EditText.Text ?? string.Empty;
		PlatformView.ClearButton.Visibility = text.Length > 0 ? ViewStates.Visible : ViewStates.Gone;

		if (!_updatingText && VirtualView is not null)
		{
			_updatingText = true;
			try
			{
				VirtualView.Text = text;
			}
			finally
			{
				_updatingText = false;
			}
		}
	}

	void OnEditorAction(object? sender, global::Android.Widget.TextView.EditorActionEventArgs e)
	{
		var isSearch = e.ActionId == ImeAction.Search
			|| (e.Event is { KeyCode: Keycode.Enter, Action: KeyEventActions.Down });

		if (!isSearch)
		{
			e.Handled = false;
			return;
		}

		PlatformView.HideKeyboard();
		VirtualView?.SendSearchButtonPressed();
		e.Handled = true;
	}

	void OnClearClicked(object? sender, EventArgs e)
	{
		PlatformView.EditText.Text = string.Empty;
		PlatformView.FocusAndShowKeyboard();
	}

	public static void MapText(NativeSearchBarHandler handler, NativeSearchBar view)
	{
		if (handler._updatingText)
		{
			return;
		}

		var editText = handler.PlatformView.EditText;
		var text = view.Text ?? string.Empty;

		if (editText.Text == text)
		{
			return;
		}

		handler._updatingText = true;
		try
		{
			editText.Text = text;
			editText.SetSelection(text.Length);
		}
		finally
		{
			handler._updatingText = false;
		}
	}

	public static void MapPlaceholder(NativeSearchBarHandler handler, NativeSearchBar view) =>
		handler.PlatformView.EditText.Hint = view.Placeholder;

	public static void MapIsTextPredictionEnabled(NativeSearchBarHandler handler, NativeSearchBar view) =>
		handler.PlatformView.SetInputType(view.IsTextPredictionEnabled);

	public static void MapIosPlacement(NativeSearchBarHandler handler, NativeSearchBar view)
	{
	}

	public static void MapBackground(NativeSearchBarHandler handler, NativeSearchBar view) =>
		handler.PlatformView.SetPillColor((((IView)view).Background as SolidPaint)?.Color?.ToPlatform());

	static void MapFocus(NativeSearchBarHandler handler, NativeSearchBar view, object? args)
	{
		handler.PlatformView.FocusAndShowKeyboard();
		CompleteFocusRequest(args, true);
	}

	static void MapUnfocus(NativeSearchBarHandler handler, NativeSearchBar view, object? args) =>
		handler.PlatformView.HideKeyboard();
}
