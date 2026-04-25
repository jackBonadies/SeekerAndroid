using Android.OS;
using Android.Views;
using Common;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Google.Android.Material.CheckBox;
using Seeker.Search;

namespace Seeker
{
    public partial class SearchFragment
    {
        private class BottomSheetDialogFragmentMenu : BottomSheetDialogFragment
        {
            private MaterialButtonToggleGroup styleToggleGroup;
            private MaterialButtonToggleGroup bitrateToggleGroup;
            private MaterialCheckBox expandableCheckBox;

            public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            {
                View rootView = inflater.Inflate(Resource.Layout.search_results_expandablexml, container);

                styleToggleGroup = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.styleToggleGroup);
                bitrateToggleGroup = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.bitrateToggleGroup);
                expandableCheckBox = rootView.FindViewById<MaterialCheckBox>(Resource.Id.checkExpandable);

                var current = PreferencesState.SearchResultStyle;
                styleToggleGroup.Check(current.HasFlag(SearchResultStyleEnum.Modern)
                    ? Resource.Id.btnStyleModern
                    : Resource.Id.btnStyleSimple);
                bitrateToggleGroup.Check(current.HasFlag(SearchResultStyleEnum.BitrateTop)
                    ? Resource.Id.btnBitrateTop
                    : Resource.Id.btnBitrateBottom);
                expandableCheckBox.Checked = current.HasFlag(SearchResultStyleEnum.Expandable);

                styleToggleGroup.AddOnButtonCheckedListener(new ToggleListener(OnStyleChanged));
                bitrateToggleGroup.AddOnButtonCheckedListener(new ToggleListener(OnStyleChanged));
                expandableCheckBox.CheckedChange += (s, e) => OnStyleChanged();

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
                if (expandableCheckBox.Checked)
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
