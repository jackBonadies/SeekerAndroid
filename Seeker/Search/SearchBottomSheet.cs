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



                // TODO step N: rebuild this UI as 3 toggles (Modern/Simple, Top/Bottom, Expandable)
                // now that the enum is orthogonal. For now the existing radio buttons map only to
                // the 5 originally-supported combinations.
                switch (PreferencesState.SearchResultStyle)
                {
                    case SearchResultStyleEnum.SimpleBottomExpandable:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonExpandable);
                        break;
                    case SearchResultStyleEnum.ModernBottomExpandable:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonExpandableModern);
                        break;
                    case SearchResultStyleEnum.SimpleBottom:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMedium);
                        break;
                    case SearchResultStyleEnum.ModernBottom:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonModernBitrateBottom);
                        break;
                    case SearchResultStyleEnum.ModernTop:
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
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.SimpleBottomExpandable;
                        break;
                    case Resource.Id.radioButtonExpandableModern:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.ModernBottomExpandable;
                        break;
                    case Resource.Id.radioButtonMedium:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.SimpleBottom;
                        break;
                    case Resource.Id.radioButtonModernBitrateBottom:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.ModernBottom;
                        break;
                    case Resource.Id.radioButtonModernBitrateTop:
                        PreferencesState.SearchResultStyle = SearchResultStyleEnum.ModernTop;
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
