using Android.OS;
using Android.Views;
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

            public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            {
                View rootView = inflater.Inflate(Resource.Layout.search_result_style_selection, container);

                styleToggleGroup = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.styleToggleGroup);
                bitrateToggleGroup = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.bitrateToggleGroup);
                expandableToggleGroup = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.expandableToggleGroup);

                var current = PreferencesState.SearchResultStyle;
                styleToggleGroup.Check(current.HasFlag(SearchResultStyleEnum.Modern)
                    ? Resource.Id.btnStyleModern
                    : Resource.Id.btnStyleSimple);
                bitrateToggleGroup.Check(current.HasFlag(SearchResultStyleEnum.BitrateTop)
                    ? Resource.Id.btnBitrateTop
                    : Resource.Id.btnBitrateBottom);
                expandableToggleGroup.Check(current.HasFlag(SearchResultStyleEnum.Expandable)
                    ? Resource.Id.btnExpandableOn
                    : Resource.Id.btnExpandableOff);

                var listener = new ToggleListener(OnStyleChanged);
                styleToggleGroup.AddOnButtonCheckedListener(listener);
                bitrateToggleGroup.AddOnButtonCheckedListener(listener);
                expandableToggleGroup.AddOnButtonCheckedListener(listener);

                return rootView;
            }

            private void OnStyleChanged()
            {
                var prev = PreferencesState.SearchResultStyle;
                var next = SearchResultStyleEnum.Simple;
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
                if (next != prev)
                {
                    PreferencesState.SearchResultStyle = next;
                    SearchFragment.Instance.SearchResultStyleChanged();
                }
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
