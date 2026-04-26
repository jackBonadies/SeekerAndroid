using Android.OS;
using Android.Views;
using Android.Widget;
using Common;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Seeker.Search;

namespace Seeker
{
    public partial class SearchFragment
    {
        private class BottomSheetDialogFragmentMenu : BottomSheetDialogFragment
        {
            private MaterialButtonToggleGroup styleToggleGroup;
            private MaterialButtonToggleGroup bitrateToggleGroup;
            private MaterialButtonToggleGroup expandableToggleGroup;
            private TextView bitrateToggleLabel;
            private TextView expandableToggleLabel;

            public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            {
                View rootView = inflater.Inflate(Resource.Layout.search_result_style_selection, container);

                styleToggleGroup = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.styleToggleGroup);
                bitrateToggleGroup = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.bitrateToggleGroup);
                expandableToggleGroup = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.expandableToggleGroup);
                bitrateToggleLabel = rootView.FindViewById<TextView>(Resource.Id.bitrateToggleLabel);
                expandableToggleLabel = rootView.FindViewById<TextView>(Resource.Id.expandableToggleLabel);

                // Match the look of searches.xml's format/bitrate toggles: purple-when-checked,
                // dialog_background-when-unchecked, white text on checked. The outlined-button
                // style alone doesn't get there; same approach as SearchFragment.SetupFilterControls.
                var bgTint = GetSegmentedButtonBgTint(rootView.Context);
                var textTint = GetSegmentedButtonTextTint(rootView.Context);
                ApplyToggleGroupTint(styleToggleGroup, bgTint, textTint);
                ApplyToggleGroupTint(bitrateToggleGroup, bgTint, textTint);
                ApplyToggleGroupTint(expandableToggleGroup, bgTint, textTint);

                var current = PreferencesState.SearchResultStyle;
                int styleId;
                if (current.HasFlag(SearchResultStyleEnum.Compact))
                {
                    styleId = Resource.Id.btnStyleCompact;
                }
                else if (current.HasFlag(SearchResultStyleEnum.Modern))
                {
                    styleId = Resource.Id.btnStyleModern;
                }
                else
                {
                    styleId = Resource.Id.btnStyleSimple;
                }
                styleToggleGroup.Check(styleId);
                bitrateToggleGroup.Check(current.HasFlag(SearchResultStyleEnum.BitrateTop)
                    ? Resource.Id.btnBitrateTop
                    : Resource.Id.btnBitrateBottom);
                expandableToggleGroup.Check(current.HasFlag(SearchResultStyleEnum.Expandable)
                    ? Resource.Id.btnExpandableOn
                    : Resource.Id.btnExpandableOff);
                ApplyCompactVisibility(current.HasFlag(SearchResultStyleEnum.Compact));

                var listener = new ToggleListener(OnStyleChanged);
                styleToggleGroup.AddOnButtonCheckedListener(listener);
                bitrateToggleGroup.AddOnButtonCheckedListener(listener);
                expandableToggleGroup.AddOnButtonCheckedListener(listener);

                return rootView;
            }

            private void OnStyleChanged()
            {
                var prev = PreferencesState.SearchResultStyle;
                bool compact = styleToggleGroup.CheckedButtonId == Resource.Id.btnStyleCompact;
                ApplyCompactVisibility(compact);
                SearchResultStyleEnum next;
                if (compact)
                {
                    next = SearchResultStyleEnum.Compact;
                }
                else
                {
                    next = SearchResultStyleEnum.Simple;
                    if (styleToggleGroup.CheckedButtonId == Resource.Id.btnStyleModern)
                    {
                        next |= SearchResultStyleEnum.Modern;
                    }
                    if (bitrateToggleGroup.CheckedButtonId == Resource.Id.btnBitrateTop)
                    {
                        next |= SearchResultStyleEnum.BitrateTop;
                    }
                    if (expandableToggleGroup.CheckedButtonId == Resource.Id.btnExpandableOn)
                    {
                        next |= SearchResultStyleEnum.Expandable;
                    }
                }
                if (next != prev)
                {
                    PreferencesState.SearchResultStyle = next;
                    SearchFragment.Instance.SearchResultStyleChanged();
                }
            }

            private void ApplyCompactVisibility(bool compact)
            {
                var v = compact ? ViewStates.Gone : ViewStates.Visible;
                bitrateToggleLabel.Visibility = v;
                bitrateToggleGroup.Visibility = v;
                expandableToggleLabel.Visibility = v;
                expandableToggleGroup.Visibility = v;
            }

            // singleSelection + selectionRequired means we get one "checked=false" on the
            // outgoing button before the "checked=true" on the incoming one. Recompute on
            // the checked-true edge so we don't react to a transient empty state.
            private class ToggleListener : Java.Lang.Object, MaterialButtonToggleGroup.IOnButtonCheckedListener
            {
                private readonly System.Action onChanged;
                public ToggleListener(System.Action onChanged) { this.onChanged = onChanged; }
                public void OnButtonChecked(MaterialButtonToggleGroup group, int checkedId, bool isChecked)
                {
                    if (isChecked)
                    {
                        onChanged();
                    }
                }
            }
        }
    }
}
