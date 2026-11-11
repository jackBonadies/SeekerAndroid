using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.CheckBox;
using System.Collections.Generic;

namespace Seeker.Helpers
{
    public sealed class SortSheetOption
    {
        public int Key { get; }
        public int LabelRes { get; }
        // false for keys with only one reasonable order (i.e. availability)
        public bool HasDirection { get; }
        // keys have a default direction (i.e. A->Z, newest->oldest)
        public bool DefaultDescending { get; }

        public SortSheetOption(int key, int labelRes, bool hasDirection, bool defaultDescending)
        {
            Key = key;
            LabelRes = labelRes;
            HasDirection = hasDirection;
            DefaultDescending = defaultDescending;
        }
    }

    // implemented by the fragment (shown with ChildFragmentManager) or activity that shows the sheet,
    // so the sheet still finds its host after a recreate
    public interface ISortSheetHost
    {
        IReadOnlyList<SortSheetOption> SortOptions { get; }
        int SortKey { get; }
        bool SortDescending { get; }
        void ApplySort(int key, bool descending);
    }

    // adds a "Set as default" checkbox
    public interface ISortSheetDefaultHost : ISortSheetHost
    {
        bool IsSortDefault { get; }
        void SetSortAsDefault();
    }

    // tap a key to sort by it, tap the selected key again to flip its direction.
    // stays open so the list behind it re-sorts live
    public sealed class SortBottomSheet : BottomSheetDialogFragment
    {
        public const string TAG = "SortBottomSheet";

        private sealed class Row
        {
            public SortSheetOption Option;
            public TextView Label;
            public ImageView Direction;
        }

        private readonly List<Row> rows = new List<Row>();
        private ISortSheetHost host;
        private MaterialCheckBox setAsDefault;
        private Typeface normalTypeface;
        private Typeface boldTypeface;

        public static void Show(AndroidX.Fragment.App.FragmentManager fragmentManager)
        {
            if (fragmentManager.FindFragmentByTag(TAG) == null)
            {
                new SortBottomSheet().Show(fragmentManager, TAG);
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sort_sheet, container, false);
            host = (ParentFragment as ISortSheetHost) ?? (Activity as ISortSheetHost);
            if (host == null)
            {
                DismissAllowingStateLoss();
                return view;
            }

            var rowContainer = view.FindViewById<LinearLayout>(Resource.Id.sortSheetRows);
            rows.Clear();
            foreach (var option in host.SortOptions)
            {
                var rowView = inflater.Inflate(Resource.Layout.sort_sheet_row, rowContainer, false);
                var row = new Row
                {
                    Option = option,
                    Label = rowView.FindViewById<TextView>(Resource.Id.sortSheetLabel),
                    Direction = rowView.FindViewById<ImageView>(Resource.Id.sortSheetDirection),
                };
                row.Label.SetText(option.LabelRes);
                rowView.Click += (s, e) => OnRowTapped(option);
                rowContainer.AddView(rowView);
                rows.Add(row);
            }
            normalTypeface = rows.Count > 0 ? (rows[0].Label.Typeface ?? Typeface.Default) : Typeface.Default;
            boldTypeface = Typeface.Create(normalTypeface, TypefaceStyle.Bold);

            if (host is ISortSheetDefaultHost defaultHost)
            {
                view.FindViewById<View>(Resource.Id.sortSheetDefaultDivider).Visibility = ViewStates.Visible;
                setAsDefault = view.FindViewById<MaterialCheckBox>(Resource.Id.sortSheetSetAsDefault);
                setAsDefault.Visibility = ViewStates.Visible;
                setAsDefault.Click += (s, e) =>
                {
                    // checked means the current sort is the default, there is nothing to uncheck to
                    if (!defaultHost.IsSortDefault)
                    {
                        defaultHost.SetSortAsDefault();
                    }
                    Render();
                };
            }

            Render();
            return view;
        }

        private void OnRowTapped(SortSheetOption option)
        {
            if (host.SortKey == option.Key)
            {
                if (!option.HasDirection)
                {
                    return;
                }
                host.ApplySort(option.Key, !host.SortDescending);
            }
            else
            {
                host.ApplySort(option.Key, option.DefaultDescending);
            }
            Render();
        }

        private void Render()
        {
            Android.Graphics.Color normalColor = UiHelpers.GetColorFromAttribute(Context, Resource.Attribute.normalTextColorNonTinted);
            Android.Graphics.Color selectedColor = UiHelpers.GetColorFromAttribute(Context, Resource.Attribute.mainTextColor);
            foreach (var row in rows)
            {
                bool selected = row.Option.Key == host.SortKey;
                row.Label.Typeface = selected ? boldTypeface : normalTypeface;
                row.Label.SetTextColor(selected ? selectedColor : normalColor);
                if (selected && row.Option.HasDirection)
                {
                    row.Direction.Visibility = ViewStates.Visible;
                    row.Direction.SetImageResource(host.SortDescending ? Resource.Drawable.arrow_down : Resource.Drawable.arrow_up);
                    row.Direction.ContentDescription = GetString(host.SortDescending ? Resource.String.sort_descending : Resource.String.sort_ascending);
                }
                else
                {
                    row.Direction.Visibility = ViewStates.Gone;
                }
            }
            if (setAsDefault != null)
            {
                setAsDefault.Checked = ((ISortSheetDefaultHost)host).IsSortDefault;
            }
        }
    }
}
