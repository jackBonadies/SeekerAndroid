using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
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



                switch (SearchFragment.SearchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandedAll:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonExpanded);
                        break;
                    case SearchResultStyleEnum.CollapsedAll:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonCollapsed);
                        break;
                    case SearchResultStyleEnum.Medium:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMedium);
                        break;
                    case SearchResultStyleEnum.Minimal:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMinimal);
                        break;
                    case SearchResultStyleEnum.MediumChipRow:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonChipRow);
                        break;
                    case SearchResultStyleEnum.MediumTwoLine:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonTwoLine);
                        break;
                    case SearchResultStyleEnum.MediumCard:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonCard);
                        break;
                    case SearchResultStyleEnum.MediumStatusBar:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonStatusBar);
                        break;
                    case SearchResultStyleEnum.MediumChipGrid:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonChipGrid);
                        break;
                    case SearchResultStyleEnum.MediumBadgeLine:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonBadgeLine);
                        break;
                }
                resultStyleRadioGroup.CheckedChange += ResultStyleRadioGroup_CheckedChange;
                return rootView;
            }

            private void ResultStyleRadioGroup_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
            {
                //RadioButton checkedRadioButton = (RadioButton)(sender as View).FindViewById(e.CheckedId);
                var prev = SearchFragment.SearchResultStyle;
                switch (e.CheckedId)
                {
                    case Resource.Id.radioButtonExpanded:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.ExpandedAll;
                        break;
                    case Resource.Id.radioButtonCollapsed:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.CollapsedAll;
                        break;
                    case Resource.Id.radioButtonMedium:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.Medium;
                        break;
                    case Resource.Id.radioButtonMinimal:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.Minimal;
                        break;
                    case Resource.Id.radioButtonChipRow:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.MediumChipRow;
                        break;
                    case Resource.Id.radioButtonTwoLine:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.MediumTwoLine;
                        break;
                    case Resource.Id.radioButtonCard:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.MediumCard;
                        break;
                    case Resource.Id.radioButtonStatusBar:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.MediumStatusBar;
                        break;
                    case Resource.Id.radioButtonChipGrid:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.MediumChipGrid;
                        break;
                    case Resource.Id.radioButtonBadgeLine:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.MediumBadgeLine;
                        break;
                }
                if (prev != SearchFragment.SearchResultStyle)
                {
                    SearchFragment.Instance.SearchResultStyleChanged();
                }
                this.Dismiss();
            }

            //public override int Theme => Resource.Style.MyCustomTheme; //for rounded corners...
        }
    }
}
