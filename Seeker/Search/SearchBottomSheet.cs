using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using Common;
using Seeker.Search;

namespace Seeker
{
    public partial class SearchFragment
    {
        private class BottomSheetDialogFragmentMenu : BottomSheetDialogFragment
        {
            public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            {

                //return base.OnCreateView(inflater, container, savedInstanceState);
                View rootView = inflater.Inflate(Resource.Layout.search_results_expandablexml, container);
                RadioGroup resultStyleRadioGroup = rootView.FindViewById<RadioGroup>(Resource.Id.radioGroup);



                switch (PreferencesState.SearchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandableLegacy:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonExpandable);
                        break;
                    case SearchResultStyleEnum.ExpandableModern:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonExpandableModern);
                        break;
                    case SearchResultStyleEnum.MediumLegacy:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMedium);
                        break;
                    case SearchResultStyleEnum.MinimalLegacy:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMinimal);
                        break;
                    case SearchResultStyleEnum.MediumModernBitrateBottom:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonModernBitrateBottom);
                        break;
                    case SearchResultStyleEnum.MediumModernBitrateTop:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonModernBitrateTop);
                        break;
                }
                resultStyleRadioGroup.CheckedChange += ResultStyleRadioGroup_CheckedChange;
                return rootView;
            }

            private void ResultStyleRadioGroup_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
            {
                //RadioButton checkedRadioButton = (RadioButton)(sender as View).FindViewById(e.CheckedId);
                var prev = PreferencesState.SearchResultStyle;
                switch (e.CheckedId)
                {
                    case Resource.Id.radioButtonExpandable:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.ExpandableLegacy;
                        break;
                    case Resource.Id.radioButtonExpandableModern:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.ExpandableModern;
                        break;
                    case Resource.Id.radioButtonMedium:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.MediumLegacy;
                        break;
                    case Resource.Id.radioButtonMinimal:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.MinimalLegacy;
                        break;
                    case Resource.Id.radioButtonModernBitrateBottom:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.MediumModernBitrateBottom;
                        break;
                    case Resource.Id.radioButtonModernBitrateTop:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.MediumModernBitrateTop;
                        break;
                }
                if (prev != PreferencesState.SearchResultStyle)
                {
                    SearchFragment.Instance.SearchResultStyleChanged();
                }
                this.Dismiss();
            }

            //public override int Theme => Resource.Style.MyCustomTheme; //for rounded corners...
        }
    }
}
