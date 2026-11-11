using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using Seeker.Helpers;
using System.Collections.Generic;

namespace Seeker.Transfers
{
    // tap a key to sort by it, tap again to flip the direction.
    // stays open so the list behind it will re-sorts live
    public sealed class TransferSortBottomSheet : BottomSheetDialogFragment
    {
        public const string TAG = "TransferSortBottomSheet";

        private static readonly (TransferSortKey key, int labelRes)[] Options =
        {
            (TransferSortKey.DateAdded, Resource.String.sort_date_added),
            (TransferSortKey.FolderName, Resource.String.sort_folder_name),
            (TransferSortKey.User, Resource.String.sort_user),
        };

        private sealed class Row
        {
            public TransferSortKey Key;
            public TextView Label;
            public ImageView Direction;
        }

        private readonly List<Row> rows = new List<Row>();
        private Typeface normalTypeface;
        private Typeface boldTypeface;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.transfer_sort_sheet, container, false);
            var rowContainer = view.FindViewById<LinearLayout>(Resource.Id.transferSortRows);
            rows.Clear();
            foreach (var (key, labelRes) in Options)
            {
                var rowView = inflater.Inflate(Resource.Layout.transfer_sort_sheet_row, rowContainer, false);
                var row = new Row
                {
                    Key = key,
                    Label = rowView.FindViewById<TextView>(Resource.Id.transferSortLabel),
                    Direction = rowView.FindViewById<ImageView>(Resource.Id.transferSortDirection),
                };
                row.Label.SetText(labelRes);
                rowView.Click += (s, e) => OnRowTapped(key);
                rowContainer.AddView(rowView);
                rows.Add(row);
            }
            normalTypeface = rows[0].Label.Typeface ?? Typeface.Default;
            boldTypeface = Typeface.Create(normalTypeface, TypefaceStyle.Bold);
            Render();
            return view;
        }

        // a newly picked date added starts newest first (so descending), names start A-Z (so ascending)
        private static bool DefaultDescending(TransferSortKey key)
        {
            return key == TransferSortKey.DateAdded;
        }

        private void OnRowTapped(TransferSortKey key)
        {
            var viewState = TransfersViewState.Instance;
            if (viewState.SortKey == key)
            {
                viewState.SortDescending = !viewState.SortDescending;
            }
            else
            {
                viewState.SortKey = key;
                viewState.SortDescending = DefaultDescending(key);
            }
            PreferencesManager.SaveTransferViewSort();
            (ParentFragment as TransfersFragment)?.OnSortChanged();
            Render();
        }

        private void Render()
        {
            var viewState = TransfersViewState.Instance;
            Color normalColor = UiHelpers.GetColorFromAttribute(Context, Resource.Attribute.normalTextColorNonTinted);
            Color selectedColor = UiHelpers.GetColorFromAttribute(Context, Resource.Attribute.mainTextColor);
            foreach (var row in rows)
            {
                bool selected = row.Key == viewState.SortKey;
                row.Label.Typeface = selected ? boldTypeface : normalTypeface;
                row.Label.SetTextColor(selected ? selectedColor : normalColor);
                if (selected)
                {
                    row.Direction.Visibility = ViewStates.Visible;
                    row.Direction.SetImageResource(viewState.SortDescending ? Resource.Drawable.arrow_down : Resource.Drawable.arrow_up);
                    row.Direction.ContentDescription = GetString(viewState.SortDescending ? Resource.String.sort_descending : Resource.String.sort_ascending);
                }
                else
                {
                    row.Direction.Visibility = ViewStates.Gone;
                }
            }
        }
    }
}
