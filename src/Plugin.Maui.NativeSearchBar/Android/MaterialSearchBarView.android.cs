using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AColor = Android.Graphics.Color;
using AImageButton = Android.Widget.ImageButton;

namespace Plugin.Maui.NativeSearchBar;

/// <summary>
/// A Material 3 search bar: a 56dp pill with a leading search icon, an inline text field and a
/// trailing clear button. Edits happen in place instead of expanding into a full-screen SearchView,
/// so the page's own content can show the results.
/// </summary>
public class MaterialSearchBarView : LinearLayout
{
	readonly GradientDrawable _pill;
	readonly AColor _defaultPillColor;

	public MaterialSearchBarView(Context context) : base(context)
	{
		Orientation = global::Android.Widget.Orientation.Horizontal;
		SetGravity(GravityFlags.CenterVertical);
		SetPadding(Dp(16), 0, Dp(4), 0);

		var night = (context.Resources?.Configuration?.UiMode & UiMode.NightMask) == UiMode.NightYes;

		// MAUI's default theme is MaterialComponents (M2), which doesn't define the M3 surface roles,
		// so fall back to the M3 neutral tones when the app theme doesn't have them.
		_defaultPillColor = ResolveColor("colorSurfaceContainerHigh", night ? 0xFF2B2B2B : 0xFFECECEC);
		var onSurface = ResolveColor("colorOnSurface", night ? 0xFFE5E5E5 : 0xFF1C1C1C);
		var onSurfaceVariant = ResolveColor("colorOnSurfaceVariant", night ? 0xFFC6C6C6 : 0xFF5E5E5E);

		_pill = new GradientDrawable();
		_pill.SetShape(ShapeType.Rectangle);
		_pill.SetColor(_defaultPillColor);
		Background = _pill;

		SearchIcon = new ImageView(context);
		SearchIcon.SetImageDrawable(LoadDrawable("abc_ic_search_api_material", global::Android.Resource.Drawable.IcMenuSearch));
		SearchIcon.ImageTintList = ColorStateList.ValueOf(onSurfaceVariant);
		SearchIcon.ImportantForAccessibility = ImportantForAccessibility.No;
		AddView(SearchIcon, new LayoutParams(Dp(24), Dp(24)));

		EditText = new AppCompatEditText(context)
		{
			Background = null,
			ImeOptions = ImeAction.Search | (ImeAction)ImeFlags.NoExtractUi,
		};
		EditText.SetSingleLine(true);
		EditText.SetTextSize(ComplexUnitType.Sp, 16);
		EditText.SetTextColor(onSurface);
		EditText.SetHintTextColor(onSurfaceVariant);
		EditText.SetPadding(Dp(16), 0, Dp(8), 0);
		AddView(EditText, new LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));

		ClearButton = new AImageButton(context)
		{
			Visibility = ViewStates.Gone,
			ContentDescription = "Clear",
		};
		ClearButton.SetImageDrawable(LoadDrawable("abc_ic_clear_material", global::Android.Resource.Drawable.IcMenuCloseClearCancel));
		ClearButton.ImageTintList = ColorStateList.ValueOf(onSurfaceVariant);
		ClearButton.Background = LoadSelectableBackground();
		AddView(ClearButton, new LayoutParams(Dp(48), Dp(48)));

		// Tapping anywhere on the pill should start editing, not just on the text itself
		Click += (_, _) => FocusAndShowKeyboard();
	}

	public ImageView SearchIcon { get; }

	public AppCompatEditText EditText { get; }

	public AImageButton ClearButton { get; }

	public void SetPillColor(AColor? color) => _pill.SetColor(color ?? _defaultPillColor);

	public void SetInputType(bool textPrediction)
	{
		EditText.InputType = textPrediction
			? InputTypes.ClassText | InputTypes.TextFlagAutoCorrect
			: InputTypes.ClassText | InputTypes.TextFlagNoSuggestions;

		// Changing the input type can drop single-line mode
		EditText.SetSingleLine(true);
	}

	public void FocusAndShowKeyboard()
	{
		EditText.RequestFocus();
		EditText.Post(() => InputMethodManager?.ShowSoftInput(EditText, ShowFlags.Implicit));
	}

	public void HideKeyboard()
	{
		InputMethodManager?.HideSoftInputFromWindow(EditText.WindowToken, HideSoftInputFlags.None);
		EditText.ClearFocus();
	}

	protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
	{
		// A search bar fills the width it's offered. With an AtMost spec LinearLayout wraps its
		// children instead, and the weighted text field collapses to its text.
		if (MeasureSpec.GetMode(widthMeasureSpec) == MeasureSpecMode.AtMost)
		{
			widthMeasureSpec = MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(widthMeasureSpec), MeasureSpecMode.Exactly);
		}

		// M3 search bars are 56dp tall. Not SetMinimumHeight: MAUI's MinimumHeightRequest mapping resets it.
		var height = Dp(56);
		if (MeasureSpec.GetMode(heightMeasureSpec) != MeasureSpecMode.Exactly)
		{
			heightMeasureSpec = MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly);
		}

		base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
	}

	protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
	{
		base.OnSizeChanged(w, h, oldw, oldh);

		// Fully rounded ends at whatever height MAUI gives us
		_pill.SetCornerRadius(h / 2f);
	}

	InputMethodManager? InputMethodManager =>
		Context?.GetSystemService(Context.InputMethodService) as InputMethodManager;

	int Dp(int value) =>
		(int)TypedValue.ApplyDimension(ComplexUnitType.Dip, value, Context!.Resources!.DisplayMetrics);

	AColor ResolveColor(string attrName, uint fallback)
	{
		var context = Context!;
		var attrId = context.Resources!.GetIdentifier(attrName, "attr", context.PackageName);
		var value = new TypedValue();

		if (attrId != 0 && context.Theme!.ResolveAttribute(attrId, value, true))
		{
			if (value.ResourceId != 0)
			{
				return new AColor(ContextCompat.GetColor(context, value.ResourceId));
			}

			if (value.Type >= DataType.FirstColorInt && value.Type <= DataType.LastColorInt)
			{
				return new AColor(value.Data);
			}
		}

		return new AColor(unchecked((int)fallback));
	}

	// AppCompat's Material icons are merged into the app's resources, so look them up by name
	Drawable? LoadDrawable(string name, int fallbackId)
	{
		var context = Context!;
		var id = context.Resources!.GetIdentifier(name, "drawable", context.PackageName);
		return ContextCompat.GetDrawable(context, id != 0 ? id : fallbackId);
	}

	Drawable? LoadSelectableBackground()
	{
		var value = new TypedValue();
		return Context!.Theme!.ResolveAttribute(global::Android.Resource.Attribute.SelectableItemBackgroundBorderless, value, true)
			? ContextCompat.GetDrawable(Context, value.ResourceId)
			: null;
	}
}
