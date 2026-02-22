using Seeker.Helpers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AndroidX.ViewPager.Widget;
using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using AndroidX.Activity;

namespace Seeker
{
    public class SwipeDisabledViewPager : ViewPager
    {

        public bool SwipeEnabled = false;

        public SwipeDisabledViewPager(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public override bool OnTouchEvent(MotionEvent motionEvent)
        {
            if (this.SwipeEnabled)
            {
                return base.OnTouchEvent(motionEvent);
            }

            return false;
        }

        public override bool OnInterceptTouchEvent(MotionEvent motionEvent)
        {
            if (this.SwipeEnabled)
            {
                return base.OnInterceptTouchEvent(motionEvent);
            }

            return false;
        }
    }



    [Activity(Label = "ImportWizardActivity", Theme = "@style/AppTheme.NoActionBar", Exported = false)]
    public class ImportWizardActivity : ThemeableActivity
    {
        private const int IMPORT_FILE_SELECTED = 2000;

        Button prevButton;
        Button nextButton;
        AndroidX.ViewPager.Widget.ViewPager pager;
        StepPagerStrip strip1;
        public static ImportedData? fullImportedData = null; //this has to be static.  otherwise someone can just rotate the screen on a later step and clear it.
        public static ImportedData? selectedImportedData = null; //this has to be static.  otherwise someone can just rotate the screen on a later step and clear it.
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var backPressedCallback = new GenericOnBackPressedCallback(true, onBackPressedAction);
            OnBackPressedDispatcher.AddCallback(backPressedCallback);

            SetContentView(Resource.Layout.wizard_activity_layout);

            prevButton = this.FindViewById<Button>(Resource.Id.prev_button);
            prevButton.Click += PrevButton_Click;
            nextButton = this.FindViewById<Button>(Resource.Id.next_button);
            nextButton.Click += NextButton_Click;

            pager = this.FindViewById<AndroidX.ViewPager.Widget.ViewPager>(Resource.Id.pager);
            pager.Adapter = new WizardPagerAdapter(this.SupportFragmentManager);
            pager.PageSelected += Pager_PageSelected;

            strip1 = this.FindViewById<StepPagerStrip>(Resource.Id.strip);
            strip1.setPageCount(pager.Adapter.Count);
            strip1.setCurrentPage(pager.CurrentItem);

            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.setting_toolbar);
            myToolbar.Title = SeekerApplication.GetString(Resource.String.ImportWizard);
            SetButtonText(pager.CurrentItem);
        }

        public void UpdatePagerReference(AndroidX.Fragment.App.Fragment frag, ImportListType importListType)
        {
            (pager.Adapter as WizardPagerAdapter).UpdatePagerReference(frag, importListType);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public bool IsCurrentStep(AndroidX.Fragment.App.Fragment f)
        {
            return f == (this.pager.Adapter as WizardPagerAdapter).GetItem(this.pager.CurrentItem);
        }

        private void onBackPressedAction(OnBackPressedCallback callback)
        {
            PrevButton_Click(null, new EventArgs());
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == IMPORT_FILE_SELECTED)
            {
                if (resultCode == Result.Ok)
                {

                    StartPageFragment.Instance.PreImportLoad();
                    string realName = string.Empty;
                    System.Threading.Tasks.Task.Run(() =>
                    {

                        if (data.Data.Scheme == "content")
                        {

                            Android.Database.ICursor cursor = this.ContentResolver.Query(data.Data, new string[] { Android.Provider.MediaStore.IMediaColumns.DisplayName }, null, null, null);
                            try
                            {
                                if (cursor != null && cursor.MoveToFirst())
                                {
                                    realName = cursor.GetString(0);
                                }
                            }
                            finally
                            {
                                cursor.Close();
                            }

                            var stream = this.ContentResolver.OpenInputStream(data.Data);
                            fullImportedData = ImportHelper.ImportFile(realName, stream);
                            selectedImportedData = new ImportedData();

                        }
                    }).ContinueWith(
                            (System.Threading.Tasks.Task t) =>
                            {
                                this.RunOnUiThread(() =>
                                {
                                    if (t.IsCompletedSuccessfully)
                                    {
                                        StartPageFragment.Instance.PostImportLoad();
                                        SetButtonText(this.pager.CurrentItem);
                                        Toast.MakeText(this, Resource.String.SuccessfullyParsed, ToastLength.Long).Show();
                                    }
                                    else
                                    {
                                        StartPageFragment.Instance.PostImportLoad();
                                        SetButtonText(this.pager.CurrentItem);
                                        if (t.Exception.InnerException is ImportHelper.NicotineParsingException npe)
                                        {
                                            Toast.MakeText(this, String.Format(SeekerApplication.GetString(Resource.String.FailedToParseReasonContactDev), npe.MessageToToast), ToastLength.Long).Show();
                                        }
                                        else
                                        {
                                            Toast.MakeText(this, Resource.String.FailedToParseContactDev, ToastLength.Long).Show();
                                        }
                                        Logger.Firebase("failed to parse: " + realName + " " + t.Exception.InnerException.Message + "---" + t.Exception.InnerException.StackTrace);
                                    }

                                });
                            });
                    //if(fullImportedData != null)
                    //{
                    //    //go to next step
                    //    pager.SetCurrentItem(1, true);
                    //}
                }
            }
            base.OnActivityResult(requestCode, resultCode, data);
        }

        public void LaunchImportIntent()
        {
            Intent intent = new Intent();
            intent.SetType("*/*");
            intent.SetAction(Intent.ActionOpenDocument);
            if (intent.ResolveActivity(this.PackageManager) != null)
            {
                //this will open default file browser and allow user to select anything.  This is preferable to Intent.ActionGetContent as ActionGetContent pulled up image gallery, contacts, etc.
                //however, if the default file browser is disabled then this fails.  So as backup do ActionGetContent.
                this.StartActivityForResult(intent, IMPORT_FILE_SELECTED);
            }
            else
            {
                Intent backUpIntent = new Intent();
                backUpIntent.SetType("*/*");
                backUpIntent.SetAction(Intent.ActionGetContent);
                try
                {
                    this.StartActivityForResult(backUpIntent, IMPORT_FILE_SELECTED);
                }
                catch (Android.Content.ActivityNotFoundException)
                {
                    //toast nothing can handle
                    Toast.MakeText(this, Resource.String.NoSuitableFileManager, ToastLength.Long).Show();
                }
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            switch (pager.CurrentItem)
            {
                case 0:
                    pager.SetCurrentItem(pager.CurrentItem + 1, true);
                    break;
                case 1:
                    //need to select which data
                    selectedImportedData = new ImportedData(((pager.Adapter as WizardPagerAdapter).GetItem(pager.CurrentItem) as ImportListFragment).GetSelectedItems(), selectedImportedData.Value.IgnoredBanned, selectedImportedData.Value.Wishlist, selectedImportedData.Value.UserNotes);
                    pager.SetCurrentItem(pager.CurrentItem + 1, true);
                    break;
                case 2:
                    //need to select which data
                    selectedImportedData = new ImportedData(selectedImportedData.Value.UserList, ((pager.Adapter as WizardPagerAdapter).GetItem(pager.CurrentItem) as ImportListFragment).GetSelectedItems(), selectedImportedData.Value.Wishlist, selectedImportedData.Value.UserNotes);
                    pager.SetCurrentItem(pager.CurrentItem + 1, true);
                    break;
                case 3:
                    //need to select which data
                    var userNotesUsernames = ((pager.Adapter as WizardPagerAdapter).GetItem(pager.CurrentItem) as ImportListFragment).GetSelectedItems();
                    List<Tuple<string, string>> userNotes = new List<Tuple<string, string>>();
                    var lookupNotes = fullImportedData.Value.UserNotes.ToDictionary(x => x.Item1, x => x.Item2);
                    foreach (string name in userNotesUsernames)
                    {
                        userNotes.Add(new Tuple<string, string>(name, lookupNotes[name]));
                    }
                    selectedImportedData = new ImportedData(selectedImportedData.Value.UserList, selectedImportedData.Value.IgnoredBanned, selectedImportedData.Value.Wishlist, userNotes);
                    pager.SetCurrentItem(pager.CurrentItem + 1, true);
                    break;
                case 4:
                    //finish
                    selectedImportedData = new ImportedData(selectedImportedData.Value.UserList, selectedImportedData.Value.IgnoredBanned, ((pager.Adapter as WizardPagerAdapter).GetItem(pager.CurrentItem) as ImportListFragment).GetSelectedItems(), selectedImportedData.Value.UserNotes);
                    ImportSelectedData(selectedImportedData.Value);
                    Toast.MakeText(this, Resource.String.SuccessfullyImported, ToastLength.Long).Show();
                    MemoryCleanup();
                    this.Finish();
                    break;
            }
            //pager.SetCurrentItem(pager.CurrentItem + 1, true);
        }

        private void MemoryCleanup()
        {
            selectedImportedData = null;
            fullImportedData = null;
        }

        private void ImportSelectedData(ImportedData selectedData)
        {
            foreach (string uname in selectedData.IgnoredBanned)
            {
                lock (SeekerState.IgnoreUserList)
                {
                    SeekerState.IgnoreUserList.Add(new UserListItem(uname, UserRole.Ignored));
                }
            }
            foreach (string uname in selectedData.UserList)
            {
                lock (SeekerState.UserList)
                {
                    UserListActivity.AddUserAPI(this, uname, null, true);
                }
            }
            foreach (var unote in selectedData.UserNotes)
            {
                SeekerState.UserNotes[unote.Item1] = unote.Item2;
            }
            foreach (var wish in selectedData.Wishlist)
            {
                //this guys state will always be good (SeekerApplication - on create)
                SearchTabHelper.AddWishlistSearchTabFromString(wish);
            }
            SearchTabHelper.SaveHeadersToSharedPrefs();
            //SearchTabHelper.SaveAllSearchTabsToDisk(SeekerState.ActiveActivityRef); //there are no additional results...
            CommonHelpers.SaveUserNotes();
            if (SeekerState.SharedPreferences != null && SeekerState.UserList != null)
            {
                PreferencesManager.SaveUserList(SerializationHelper.SaveUserListToString(SeekerState.UserList));
            }
            if (SeekerState.SharedPreferences != null && SeekerState.IgnoreUserList != null)
            {
                PreferencesManager.SaveIgnoreUserList(SerializationHelper.SaveUserListToString(SeekerState.IgnoreUserList));
            }
        }

        private void PrevButton_Click(object sender, EventArgs e)
        {
            switch (pager.CurrentItem)
            {
                case 0:
                    this.Finish();
                    break;
                default:
                    this.pager.SetCurrentItem(this.pager.CurrentItem - 1, true);
                    break;
            }
        }

        private void Pager_PageSelected(object sender, AndroidX.ViewPager.Widget.ViewPager.PageSelectedEventArgs e)
        {
            SetButtonText(e.Position);
            strip1.setCurrentPage(e.Position);
            if (e.Position != 0)
            {
                ((pager.Adapter as WizardPagerAdapter).GetItem(pager.CurrentItem) as ImportListFragment).SetState(this);
            }
        }

        private void SetButtonText(int position)
        {
            if (fullImportedData != null)
            {
                nextButton.Enabled = true;
                nextButton.Clickable = true;
                nextButton.Alpha = 1.0f;
            }
            else
            {
                nextButton.Enabled = false;
                nextButton.Clickable = false;
                nextButton.Alpha = 0.5f;
            }
            switch (position)
            {
                case 0:
                    prevButton.Text = SeekerApplication.GetString(Resource.String.cancel);
                    nextButton.Text = SeekerApplication.GetString(Resource.String.next);
                    break;
                case 4:
                    prevButton.Text = SeekerApplication.GetString(Resource.String.prev);
                    nextButton.Text = SeekerApplication.GetString(Resource.String.finish);
                    break;
                default:
                    prevButton.Text = SeekerApplication.GetString(Resource.String.prev);
                    nextButton.Text = SeekerApplication.GetString(Resource.String.next);
                    break;
            }
        }
    }


    public class StartPageFragment : AndroidX.Fragment.App.Fragment
    {
        private View rootView;
        private Button importButton;
        private AndroidX.Core.Widget.ContentLoadingProgressBar loadingBar;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            this.rootView = inflater.Inflate(Resource.Layout.import_start_page, container, false);
            this.importButton = this.rootView.FindViewById<Button>(Resource.Id.importData);
            importButton.Click += ImportButton_Click;
            this.loadingBar = this.rootView.FindViewById<AndroidX.Core.Widget.ContentLoadingProgressBar>(Resource.Id.contentLoadingProgressBar1);
            if (isLoading)
            {
                this.loadingBar.Show();
            }
            else
            {
                this.loadingBar.Hide();
            }
            Instance = this;
            return rootView;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            //nullref for this.Activity ??
            (SeekerState.ActiveActivityRef as ImportWizardActivity).LaunchImportIntent();
        }
        private static bool isLoading = false;
        public static StartPageFragment Instance = null; //needed for rotation.
        public void PreImportLoad()
        {
            isLoading = true;
            importButton.Enabled = false;
            importButton.Clickable = false;
            importButton.Alpha = 0.5f;
            loadingBar.Show();
        }

        public void PostImportLoad()
        {
            isLoading = false;
            importButton.Enabled = true;
            importButton.Clickable = true;
            importButton.Alpha = 1.0f;
            loadingBar.Hide();
        }
    }

    public enum ImportListType
    {
        UserList = 0,
        Ignore = 1,
        Wishlist = 2,
        UserNotes = 3
    }

    public class ImportItem
    {
        public ImportItem(string itemString, bool ischecked, bool asterisk)
        {
            item = itemString;
            isChecked = ischecked;
            showAsterisk = asterisk;
        }
        public bool showAsterisk;
        public bool isChecked;
        public string item;
    }


    public class ImportListAdapter : RecyclerView.Adapter
    {
        public void ToggleAll()
        {
            bool allChecked = localDataSet.TrueForAll((item) => item.isChecked);
            for (int i = 0; i < localDataSet.Count; i++)
            {
                //if they are all checked, then uncheck them all.  else check them all.
                localDataSet[i].isChecked = !allChecked;
            }
        }

        public List<ImportItem> localDataSet; //tab id's
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        //public BrowseFragment Owner;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            ImportItemView view = ImportItemView.inflate(parent);
            view.setupChildren();
            view.ImportItemCheckbox.CheckedChange += ImportItemCheckbox_CheckedChange;
            // .inflate(R.layout.text_row_item, viewGroup, false);
            //(view as SearchTabView).searchTabLayout.Click += SearchTabLayout_Click;
            //(view as SearchTabView).removeSearch.Click += RemoveSearch_Click;
            return new ImportItemViewHolder(view as View);


        }

        private void ImportItemCheckbox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            int pos = ((sender as TextView).Parent.Parent as ImportItemView).ViewHolder.AdapterPosition;
            localDataSet[pos].isChecked = e.IsChecked;
        }

        //private void View_Click(object sender, EventArgs e)
        //{
        //    int pos = ((sender as TextView).Parent.Parent as TreePathItemView).ViewHolder.AdapterPosition;
        //    Owner.GoUpDirectory(localDataSet.Count - pos - 2);
        //}

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as ImportItemViewHolder).pathItemView.setItem(localDataSet[position]);
        }


        //private void SearchTabLayout_Click(object sender, EventArgs e)
        //{
        //    position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.AdapterPosition;
        //    int tabToGoTo = localDataSet[position];
        //    SearchFragment.Instance.GoToTab(tabToGoTo, false);
        //    SearchTabDialog.Instance.Dismiss();
        //}

        public ImportListAdapter(List<ImportItem> ti)
        {
            localDataSet = ti;
        }

    }

    public class ImportItemViewHolder : RecyclerView.ViewHolder
    {
        public ImportItemView pathItemView;


        public ImportItemViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            pathItemView = (ImportItemView)view;
            pathItemView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public ImportItemView getUnderlyingView()
        {
            return pathItemView;
        }
    }



    public class ImportItemView : LinearLayout
    {
        //public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        public CheckBox ImportItemCheckbox;
        public ImportItem InnerImportItem { get; set; }
        public ImportItemViewHolder ViewHolder;

        public ImportItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.import_item_view, this, true);
            setupChildren();
        }
        public ImportItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.import_item_view, this, true);
            setupChildren();
        }

        public static ImportItemView inflate(ViewGroup parent)
        {
            ImportItemView itemView = (ImportItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.import_item_view_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            ImportItemCheckbox = FindViewById<CheckBox>(Resource.Id.importItemCheckbox);
        }

        public void setItem(ImportItem item)
        {
            InnerImportItem = item;
            if (item.showAsterisk)
            {
                ImportItemCheckbox.Text = item.item + "*";
            }
            else
            {
                ImportItemCheckbox.Text = item.item;
            }
            ImportItemCheckbox.Checked = item.isChecked;
        }
    }



    public class ImportListFragment : AndroidX.Fragment.App.Fragment
    {
        private View rootView;
        private TextView noneFound;
        private TextView alreadyAdded;
        private TextView selectTheFollowing;
        private Button toggleAll;
        private AndroidX.RecyclerView.Widget.RecyclerView recyclerView;
        private Guid guid = Guid.NewGuid();
        private ImportListType importListType;

        public List<string> GetSelectedItems()
        {
            return this.importListAdapter.localDataSet.Where((item) => item.isChecked).Select(item => item.item).ToList();
        }

        //private Recyc alreadyAdded;
        /// <summary>
        /// Default constructor necessary for android system
        /// </summary>
        public ImportListFragment()
        {

        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutInt("IMPORT_LIST_TYPE", (int)importListType);
        }


        public ImportListFragment(ImportListType _importListType)
        {
            importListType = _importListType;
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (savedInstanceState != null)
            {
                importListType = (ImportListType)(savedInstanceState.GetInt("IMPORT_LIST_TYPE", (int)-1));
            }
            this.rootView = inflater.Inflate(Resource.Layout.import_list_layout, container, false);
            noneFound = this.rootView.FindViewById<TextView>(Resource.Id.noneFound);
            alreadyAdded = this.rootView.FindViewById<TextView>(Resource.Id.alreadyPresentTextView);
            selectTheFollowing = this.rootView.FindViewById<TextView>(Resource.Id.selectTheFollowing);
            recyclerView = this.rootView.FindViewById<AndroidX.RecyclerView.Widget.RecyclerView>(Resource.Id.recyclerViewImportList);
            var lm = new LinearLayoutManager(this.Context, LinearLayoutManager.Vertical, false);
            recyclerView.SetLayoutManager(lm);
            toggleAll = this.rootView.FindViewById<Button>(Resource.Id.toggleAllButton);
            toggleAll.Click += ToggleAll_Click;

            (SeekerState.ActiveActivityRef as ImportWizardActivity).UpdatePagerReference(this, importListType);
            Console.WriteLine("OnCreateView: " + importListType.ToString() + " " + guid.ToString());
            return rootView;
        }

        private void ToggleAll_Click(object sender, EventArgs e)
        {
            this.importListAdapter.ToggleAll();
            this.importListAdapter.NotifyDataSetChanged();
        }

        public override void OnDestroy()
        {
            Console.WriteLine("OnDestroy: " + importListType.ToString() + " " + guid.ToString());
            base.OnDestroy();
        }

        public override void OnDestroyView()
        {
            Console.WriteLine("OnDestroyView: " + importListType.ToString() + " " + guid.ToString());
            base.OnDestroyView();
        }

        public override void OnResume()
        {
            Console.WriteLine("OnResume: " + importListType.ToString() + " " + guid.ToString());
            (SeekerState.ActiveActivityRef as ImportWizardActivity).UpdatePagerReference(this, importListType);
            if ((SeekerState.ActiveActivityRef as ImportWizardActivity).IsCurrentStep(this))
            {
                SetState(ImportWizardActivity.fullImportedData.Value, this.importListType);
            }
            base.OnResume();
        }

        public override void OnAttach(Context context)
        {
            Console.WriteLine("OnAttach: " + importListType.ToString() + " " + guid.ToString());
            base.OnAttach(context);
        }

        public void SetState(ImportWizardActivity wizard)
        {
            if ((wizard as ImportWizardActivity).IsCurrentStep(this))
            {
                SetState(ImportWizardActivity.fullImportedData.Value, this.importListType);
            }
        }

        private string CreateAlreadyAddedString(IEnumerable<string> usernames, ImportListType listType)
        {
            string userString = "users";
            string userListString = "User List";
            if (listType == ImportListType.Ignore)
            {
                userListString = "Ignore List";
            }
            else if (listType == ImportListType.Wishlist)
            {
                userString = "searches";
                userListString = "Wishlist";
            }
            StringBuilder alreadyAddedNote = new StringBuilder(string.Format("Note: The following {0} are already present in {1} - ", userString, userListString));
            if (usernames.Count() > 10)
            {
                foreach (string name in usernames.Take(10))
                {
                    alreadyAddedNote.Append(name);
                    alreadyAddedNote.Append(", ");
                }
                alreadyAddedNote.Append(" and others...");
                return alreadyAddedNote.ToString();
            }
            else
            {
                foreach (string name in usernames)
                {
                    alreadyAddedNote.Append(name);
                    alreadyAddedNote.Append(", ");
                }
                alreadyAddedNote.Remove(alreadyAddedNote.Length - 2, 2);
                return alreadyAddedNote.ToString();
            }
        }

        private ImportListAdapter importListAdapter;
        public void SetState(ImportedData data, ImportListType listType)
        {
            string title = "Select the following {0} to add to {1}";
            string none = "No {0} found to import";
            if (selectTheFollowing == null)
            {
                return;//too early.
            }
            switch (listType)
            {
                case ImportListType.UserList:
                    selectTheFollowing.Text = string.Format(title, "friends", "User List");
                    if (data.UserList == null || data.UserList.Count == 0)
                    {
                        noneFound.Visibility = ViewStates.Visible;
                        noneFound.Text = string.Format(none, "friends");
                    }
                    else
                    {
                        noneFound.Visibility = ViewStates.Gone;
                    }
                    //todo already present
                    var currentlyHave = SeekerState.UserList.Select(item => item.Username).ToList();
                    var notYetAdded = data.UserList.Except(currentlyHave).ToList();
                    var alreadyAddedList = data.UserList.Except(notYetAdded).ToList();
                    if (alreadyAddedList.Count == 0)
                    {
                        alreadyAdded.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        alreadyAdded.Visibility = ViewStates.Visible;
                        alreadyAdded.Text = CreateAlreadyAddedString(alreadyAddedList, listType);
                    }

                    if (ImportWizardActivity.selectedImportedData.Value.UserList != null)
                    {
                        var selectedItemsDict = ImportWizardActivity.selectedImportedData.Value.UserList.ToDictionary(x => x, x => 0);
                        importListAdapter = new ImportListAdapter(notYetAdded.Select(item => new ImportItem(item, selectedItemsDict.ContainsKey(item) ? true : false, false)).ToList());
                    }
                    else
                    {
                        importListAdapter = new ImportListAdapter(notYetAdded.Select(item => new ImportItem(item, true, false)).ToList());
                    }

                    this.recyclerView.SetAdapter(importListAdapter);

                    if (notYetAdded == null || notYetAdded.Count == 0)
                    {
                        this.recyclerView.Visibility = ViewStates.Gone;
                        this.toggleAll.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        this.recyclerView.Visibility = ViewStates.Visible;
                        this.toggleAll.Visibility = ViewStates.Visible;
                    }

                    //
                    break;
                case ImportListType.Ignore:
                    selectTheFollowing.Text = string.Format(title, "users", "Ignored");
                    if (data.IgnoredBanned == null || data.IgnoredBanned.Count == 0)
                    {
                        noneFound.Visibility = ViewStates.Visible;
                        noneFound.Text = string.Format(none, "users");
                    }
                    else
                    {
                        noneFound.Visibility = ViewStates.Gone;
                    }
                    //todo already present
                    var currentlyHaveIgnored = SeekerState.IgnoreUserList.Select(item => item.Username).ToList();
                    var notYetIgnored = data.IgnoredBanned.Except(currentlyHaveIgnored).ToList();
                    var alreadyIgnoredList = data.IgnoredBanned.Except(notYetIgnored).ToList();
                    if (alreadyIgnoredList.Count == 0)
                    {
                        alreadyAdded.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        alreadyAdded.Visibility = ViewStates.Visible;
                        alreadyAdded.Text = CreateAlreadyAddedString(alreadyIgnoredList, listType);
                    }
                    if (ImportWizardActivity.selectedImportedData.Value.IgnoredBanned != null)
                    {
                        var selectedItemsIgDict = ImportWizardActivity.selectedImportedData.Value.IgnoredBanned.ToDictionary(x => x, x => 0);
                        importListAdapter = new ImportListAdapter(notYetIgnored.Select(item => new ImportItem(item, selectedItemsIgDict.ContainsKey(item) ? true : false, false)).ToList());
                    }
                    else
                    {
                        importListAdapter = new ImportListAdapter(notYetIgnored.Select(item => new ImportItem(item, true, false)).ToList());
                    }
                    this.recyclerView.SetAdapter(importListAdapter);
                    //}
                    if (notYetIgnored == null || notYetIgnored.Count == 0)
                    {
                        this.recyclerView.Visibility = ViewStates.Gone;
                        this.toggleAll.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        this.recyclerView.Visibility = ViewStates.Visible;
                        this.toggleAll.Visibility = ViewStates.Visible;
                    }
                    break;
                case ImportListType.UserNotes:
                    selectTheFollowing.Text = SeekerApplication.GetString(Resource.String.SelectUserNotes);
                    if (data.UserNotes == null || data.UserNotes.Count == 0)
                    {
                        noneFound.Visibility = ViewStates.Visible;
                        noneFound.Text = string.Format(none, "user notes");
                    }
                    else
                    {
                        noneFound.Visibility = ViewStates.Gone;
                    }
                    //todo already present
                    //maybe do asterick
                    var currentlyHaveNoted = SeekerState.UserNotes.Select(item => item.Key).ToList();
                    var notYetNoted = data.UserNotes.Select(item => item.Item1).Except(currentlyHaveNoted).ToList();
                    var alreadyNotedList = data.UserNotes.Select(item => item.Item1).Except(notYetNoted).ToList();
                    if (alreadyNotedList.Count == 0)
                    {
                        alreadyAdded.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        alreadyAdded.Visibility = ViewStates.Visible;
                        alreadyAdded.Text = "* denotes that the user has a current note which will be overwritten if selected.";
                    }
                    //if (importListAdapter == null)
                    //{
                    var notYetNotedItems = notYetNoted.Select(item => new ImportItem(item, true, false)).ToList();
                    notYetNotedItems.AddRange(alreadyNotedList.Select(item => new ImportItem(item, true, true)));
                    if (ImportWizardActivity.selectedImportedData.Value.UserNotes != null)
                    {
                        var selectedItemsNotesDict = ImportWizardActivity.selectedImportedData.Value.UserNotes.ToDictionary(x => x.Item1, x => 0);
                        foreach (var item in notYetNotedItems)
                        {
                            if (!selectedItemsNotesDict.ContainsKey(item.item))
                            {
                                item.isChecked = false;
                            }
                        }
                    }
                    importListAdapter = new ImportListAdapter(notYetNotedItems.ToList());
                    this.recyclerView.SetAdapter(importListAdapter);
                    if (notYetNotedItems == null || notYetNotedItems.Count == 0)
                    {
                        this.recyclerView.Visibility = ViewStates.Gone;
                        this.toggleAll.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        this.recyclerView.Visibility = ViewStates.Visible;
                        this.toggleAll.Visibility = ViewStates.Visible;
                    }
                    //}
                    break;
                case ImportListType.Wishlist:
                    selectTheFollowing.Text = string.Format(title, "searches", "Wishlist");
                    if (data.Wishlist == null || data.Wishlist.Count == 0)
                    {
                        noneFound.Visibility = ViewStates.Visible;
                        noneFound.Text = string.Format(none, "wishlist searches");
                    }
                    else
                    {
                        noneFound.Visibility = ViewStates.Gone;
                    }
                    var currentlyHaveWishes = SearchTabHelper.SearchTabCollection.Where(item => item.Key < 0).Select(item => item.Value.LastSearchTerm).ToList();
                    var notYetWished = data.Wishlist.Except(currentlyHaveWishes).ToList();
                    var alreadyWishedList = data.Wishlist.Except(notYetWished).ToList();
                    if (alreadyWishedList.Count == 0)
                    {
                        alreadyAdded.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        alreadyAdded.Visibility = ViewStates.Visible;
                        alreadyAdded.Text = CreateAlreadyAddedString(alreadyWishedList, listType);
                    }
                    if (ImportWizardActivity.selectedImportedData.Value.Wishlist != null)
                    {
                        var selectedItemsWishesDict = ImportWizardActivity.selectedImportedData.Value.Wishlist.ToDictionary(x => x, x => 0);
                        importListAdapter = new ImportListAdapter(notYetWished.Select(item => new ImportItem(item, selectedItemsWishesDict.ContainsKey(item) ? true : false, false)).ToList());
                    }
                    else
                    {
                        importListAdapter = new ImportListAdapter(notYetWished.Select(item => new ImportItem(item, true, false)).ToList());
                    }
                    this.recyclerView.SetAdapter(importListAdapter);
                    //}

                    if (notYetWished == null || notYetWished.Count == 0)
                    {
                        this.recyclerView.Visibility = ViewStates.Gone;
                        this.toggleAll.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        this.recyclerView.Visibility = ViewStates.Visible;
                        this.toggleAll.Visibility = ViewStates.Visible;
                    }

                    //todo already present
                    //maybe do asterick
                    break;
            }
        }

    }


    public class WizardPagerAdapter : FragmentPagerAdapter
    {
        AndroidX.Fragment.App.Fragment startPage = null;

        AndroidX.Fragment.App.Fragment userListPage1 = null;
        AndroidX.Fragment.App.Fragment ignoredPage2 = null;
        AndroidX.Fragment.App.Fragment userNotesPage3 = null;
        AndroidX.Fragment.App.Fragment wishlistPage4 = null;


        public WizardPagerAdapter(AndroidX.Fragment.App.FragmentManager fm) : base(fm)
        {
            startPage = new StartPageFragment();
            userListPage1 = new ImportListFragment(ImportListType.UserList);
            ignoredPage2 = new ImportListFragment(ImportListType.Ignore);
            userNotesPage3 = new ImportListFragment(ImportListType.UserNotes);
            wishlistPage4 = new ImportListFragment(ImportListType.Wishlist);
        }

        public void UpdatePagerReference(AndroidX.Fragment.App.Fragment frag, ImportListType importListType)
        {
            switch (importListType)
            {
                case ImportListType.UserList:
                    userListPage1 = frag;
                    break;
                case ImportListType.Ignore:
                    ignoredPage2 = frag;
                    break;
                case ImportListType.UserNotes:
                    userNotesPage3 = frag;
                    break;
                case ImportListType.Wishlist:
                    wishlistPage4 = frag;
                    break;
            }
        }

        public override int Count => 5;

        public override AndroidX.Fragment.App.Fragment GetItem(int position)
        {
            AndroidX.Fragment.App.Fragment frag = null;
            switch (position)
            {
                case 0:
                    frag = startPage;
                    break;
                case 1:
                    frag = userListPage1;
                    break;
                case 2:
                    frag = ignoredPage2;
                    break;
                case 3:
                    frag = userNotesPage3;
                    break;
                case 4:
                    frag = wishlistPage4;
                    break;
                default:
                    throw new System.Exception("Invalid Tab");
            }
            return frag;
        }

        public override int GetItemPosition(Java.Lang.Object @object)
        {
            return PositionNone;
        }

        //public override ICharSequence GetPageTitleFormatted(int position)
        //{
        //    ICharSequence title;
        //    switch (position)
        //    {
        //        case 0:
        //            title = new Java.Lang.String(SeekerState.ActiveActivityRef.GetString(Resource.String.account_tab));
        //            break;
        //        case 1:
        //            title = new Java.Lang.String(SeekerState.ActiveActivityRef.GetString(Resource.String.searches_tab));
        //            break;
        //        case 2:
        //            title = new Java.Lang.String(SeekerState.ActiveActivityRef.GetString(Resource.String.transfer_tab));
        //            break;
        //        case 3:
        //            title = new Java.Lang.String(SeekerState.ActiveActivityRef.GetString(Resource.String.browse_tab));
        //            break;
        //        default:
        //            throw new System.Exception("Invalid Tab");
        //    }
        //    return title;
        //}
    }



    public class StepPagerStrip : View
    {
        private static int[] ATTRS = new int[]{
            Android.Resource.Attribute.Gravity    };
        private int mPageCount;
        private int mCurrentPage;

        private int mGravity = (int)(GravityFlags.Left | GravityFlags.Top);
        private float mTabWidth;
        private float mTabHeight;
        private float mTabSpacing;

        private Paint mPrevTabPaint;
        private Paint mSelectedTabPaint;
        private Paint mSelectedLastTabPaint;
        private Paint mNextTabPaint;

        private RectF mTempRectF = new RectF();

        //private Scroller mScroller;

        //private OnPageSelectedListener mOnPageSelectedListener;

        public StepPagerStrip(Context context) : this(context, null, 0)
        {

        }

        public StepPagerStrip(Context context, IAttributeSet attrs) : this(context, attrs, 0)
        {
        }

        public StepPagerStrip(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {

            //final TypedArray a = context.obtainStyledAttributes(attrs, ATTRS);
            //mGravity = a.getInteger(0, mGravity);
            //a.recycle();

            //final Resources res = getResources();
            mTabWidth = this.Resources.GetDimensionPixelSize(Resource.Dimension.step_pager_tab_width);
            mTabHeight = this.Resources.GetDimensionPixelSize(Resource.Dimension.step_pager_tab_height);
            mTabSpacing = this.Resources.GetDimensionPixelSize(Resource.Dimension.step_pager_tab_spacing);

            mPrevTabPaint = new Paint();
            mPrevTabPaint.Color = this.Resources.GetColor(Resource.Color.prevPage);

            mSelectedTabPaint = new Paint();
            mSelectedTabPaint.Color = this.Resources.GetColor(Resource.Color.currentPage);

            mSelectedLastTabPaint = new Paint();
            mSelectedLastTabPaint = mSelectedTabPaint;//Color.Red;

            mNextTabPaint = new Paint();
            mNextTabPaint.Color = this.Resources.GetColor(Resource.Color.nextPage);
        }

        //public void setOnPageSelectedListener(OnPageSelectedListener onPageSelectedListener)
        //{
        //    mOnPageSelectedListener = onPageSelectedListener;
        //}

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (mPageCount == 0)
            {
                return;
            }

            float totalWidth = mPageCount * (mTabWidth + mTabSpacing) - mTabSpacing;
            float totalLeft;
            bool fillHorizontal = false;

            switch (((GravityFlags)mGravity & GravityFlags.HorizontalGravityMask))
            {
                case GravityFlags.CenterHorizontal:
                    totalLeft = (this.Width - totalWidth) / 2;
                    break;
                case GravityFlags.Right:
                    totalLeft = this.Width - this.Right - totalWidth;
                    break;
                case GravityFlags.FillHorizontal:
                    totalLeft = this.PaddingLeft;
                    fillHorizontal = true;
                    break;
                default:
                    totalLeft = this.PaddingLeft;
                    break;
            }

            switch (((GravityFlags)mGravity & GravityFlags.VerticalGravityMask))
            {
                case GravityFlags.CenterVertical:
                    mTempRectF.Top = (int)(this.Height - mTabHeight) / 2;
                    break;
                case GravityFlags.Bottom:
                    mTempRectF.Top = this.Height - this.PaddingBottom - mTabHeight;
                    break;
                default:
                    mTempRectF.Top = this.PaddingTop;
                    break;
            }

            mTempRectF.Bottom = mTempRectF.Top + mTabHeight;

            float tabWidth = mTabWidth;
            if (fillHorizontal)
            {
                tabWidth = (this.Width - this.PaddingRight - this.PaddingLeft
                        - (mPageCount - 1) * mTabSpacing) / mPageCount;
            }

            for (int i = 0; i < mPageCount; i++)
            {
                mTempRectF.Left = totalLeft + (i * (tabWidth + mTabSpacing));
                mTempRectF.Right = mTempRectF.Left + tabWidth;
                canvas.DrawRect(mTempRectF, i < mCurrentPage
                        ? mPrevTabPaint
                        : (i > mCurrentPage
                                ? mNextTabPaint
                                : (i == mPageCount - 1
                                        ? mSelectedLastTabPaint
                                        : mSelectedTabPaint)));
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            SetMeasuredDimension(
                    View.ResolveSize(
                            (int)(mPageCount * (mTabWidth + mTabSpacing) - mTabSpacing)
                                    + this.PaddingLeft + this.PaddingRight,
                            widthMeasureSpec),
                    View.ResolveSize(
                            (int)mTabHeight
                                    + this.PaddingTop + this.PaddingBottom,
                            heightMeasureSpec));
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            scrollCurrentPageIntoView();
            base.OnSizeChanged(w, h, oldw, oldh);
        }

        //@Override
        //public boolean onTouchEvent(MotionEvent event)
        //{
        //    if (mOnPageSelectedListener != null)
        //    {
        //        switch (event.getActionMasked()) {
        //            case MotionEvent.ACTION_DOWN:
        //            case MotionEvent.ACTION_MOVE:
        //                int position = hitTest(event.getX());
        //            if (position >= 0)
        //            {
        //                mOnPageSelectedListener.onPageStripSelected(position);
        //            }
        //            return true;
        //        }
        //    }
        //    return super.onTouchEvent(event);
        //}

        //private int hitTest(float x)
        //{
        //    if (mPageCount == 0)
        //    {
        //        return -1;
        //    }

        //    float totalWidth = mPageCount * (mTabWidth + mTabSpacing) - mTabSpacing;
        //    float totalLeft;
        //    boolean fillHorizontal = false;

        //    switch (mGravity & Gravity.HORIZONTAL_GRAVITY_MASK)
        //    {
        //        case Gravity.CENTER_HORIZONTAL:
        //            totalLeft = (getWidth() - totalWidth) / 2;
        //            break;
        //        case Gravity.RIGHT:
        //            totalLeft = getWidth() - getPaddingRight() - totalWidth;
        //            break;
        //        case Gravity.FILL_HORIZONTAL:
        //            totalLeft = getPaddingLeft();
        //            fillHorizontal = true;
        //            break;
        //        default:
        //            totalLeft = getPaddingLeft();
        //    }

        //    float tabWidth = mTabWidth;
        //    if (fillHorizontal)
        //    {
        //        tabWidth = (getWidth() - getPaddingRight() - getPaddingLeft()
        //                - (mPageCount - 1) * mTabSpacing) / mPageCount;
        //    }

        //    float totalRight = totalLeft + (mPageCount * (tabWidth + mTabSpacing));
        //    if (x >= totalLeft && x <= totalRight && totalRight > totalLeft)
        //    {
        //        return (int)(((x - totalLeft) / (totalRight - totalLeft)) * mPageCount);
        //    }
        //    else
        //    {
        //        return -1;
        //    }
        //}

        public void setCurrentPage(int currentPage)
        {
            mCurrentPage = currentPage;
            this.Invalidate();
            //scrollCurrentPageIntoView();

            // TODO: Set content description appropriately
        }

        private void scrollCurrentPageIntoView()
        {
            // TODO: only works with left gravity for now
            //
            //        float widthToActive = getPaddingLeft() + (mCurrentPage + 1) * (mTabWidth + mTabSpacing)
            //                - mTabSpacing;
            //        int viewWidth = getWidth();
            //
            //        int startScrollX = getScrollX();
            //        int destScrollX = (widthToActive > viewWidth) ? (int) (widthToActive - viewWidth) : 0;
            //
            //        if (mScroller == null) {
            //            mScroller = new Scroller(getContext());
            //        }
            //
            //        mScroller.abortAnimation();
            //        mScroller.startScroll(startScrollX, 0, destScrollX - startScrollX, 0);
            //        postInvalidate();
        }

        public void setPageCount(int count)
        {
            mPageCount = count;
            this.Invalidate();

            // TODO: Set content description appropriately
        }


    }
}