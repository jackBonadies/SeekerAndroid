using Seeker.Helpers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
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
        PageDotsIndicator pageDots;
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

            var buttonBar = this.FindViewById<LinearLayout>(Resource.Id.wizard_button_bar);
            AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(buttonBar, new BottomOnlyInsetsListener());

            pager = this.FindViewById<AndroidX.ViewPager.Widget.ViewPager>(Resource.Id.pager);
            pager.Adapter = new WizardPagerAdapter(this.SupportFragmentManager);
            pager.PageSelected += Pager_PageSelected;
            pager.PageScrolled += Pager_PageScrolled;

            pageDots = this.FindViewById<PageDotsIndicator>(Resource.Id.strip);
            pageDots.SetPageCount(pager.Adapter.Count);
            pageDots.SetPosition(pager.CurrentItem);

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
                            if (cursor != null)
                            {
                                try
                                {
                                    if (cursor.MoveToFirst())
                                    {
                                        realName = cursor.GetString(0);
                                    }
                                }
                                finally
                                {
                                    cursor.Close();
                                }
                            }
                        }
                        else
                        {
                            //i.e. "file" scheme from some file managers.  OpenInputStream handles it too.
                            realName = data.Data.LastPathSegment ?? string.Empty;
                        }

                        using (var stream = this.ContentResolver.OpenInputStream(data.Data))
                        {
                            fullImportedData = ImportHelper.ImportFile(realName, stream);
                        }
                        selectedImportedData = new ImportedData();
                    }).ContinueWith(
                            (System.Threading.Tasks.Task t) =>
                            {
                                this.RunOnUiThread(() =>
                                {
                                    if (t.IsCompletedSuccessfully)
                                    {
                                        StartPageFragment.Instance.PostImportLoad();
                                        SetButtonText(this.pager.CurrentItem);
                                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.SuccessfullyParsed), ToastLength.Long);
                                    }
                                    else
                                    {
                                        StartPageFragment.Instance.PostImportLoad();
                                        SetButtonText(this.pager.CurrentItem);
                                        if (t.Exception.InnerException is ImportHelper.NicotineParsingException npe)
                                        {
                                            SeekerApplication.Toaster.ShowToast(String.Format(SeekerApplication.GetString(Resource.String.FailedToParseReasonContactDev), npe.MessageToToast), ToastLength.Long);
                                        }
                                        else
                                        {
                                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.FailedToParseContactDev), ToastLength.Long);
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
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.NoSuitableFileManager), ToastLength.Long);
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
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.SuccessfullyImported), ToastLength.Long);
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
                lock (CommonState.IgnoreUserList)
                {
                    CommonState.IgnoreUserList.Add(new UserListItem(uname, UserRole.Ignored));
                }
            }
            foreach (string uname in selectedData.UserList)
            {
                UserListService.AddUserMassImport(uname);
            }
            foreach (var unote in selectedData.UserNotes)
            {
                UserMetadataService.UserNotes[unote.Item1] = unote.Item2;
            }
            foreach (var wish in selectedData.Wishlist)
            {
                //this guys state will always be good (SeekerApplication - on create)
                SearchTabHelper.AddWishlistSearchTabFromString(wish);
            }
            SearchTabHelper.SaveHeadersToSharedPrefs();
            //SearchTabHelper.SaveAllSearchTabsToDisk(SeekerState.ActiveActivityRef); //there are no additional results...
            CommonHelpers.SaveUserNotes();
            if (SeekerState.SharedPreferences != null && CommonState.UserList != null)
            {
                PreferencesManager.SaveUserList(SerializationHelper.SaveUserListToString(CommonState.UserList));
            }
            if (SeekerState.SharedPreferences != null && CommonState.IgnoreUserList != null)
            {
                PreferencesManager.SaveIgnoreUserList(SerializationHelper.SaveUserListToString(CommonState.IgnoreUserList));
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

        private void Pager_PageScrolled(object sender, AndroidX.ViewPager.Widget.ViewPager.PageScrolledEventArgs e)
        {
            pageDots.SetPosition(e.Position + e.PositionOffset);
        }

        private void Pager_PageSelected(object sender, AndroidX.ViewPager.Widget.ViewPager.PageSelectedEventArgs e)
        {
            SetButtonText(e.Position);
            pageDots.SetPosition(e.Position);
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
                    prevButton.Text = SeekerApplication.GetString(Resource.String.back_desc);
                    nextButton.Text = SeekerApplication.GetString(Resource.String.finish);
                    break;
                default:
                    prevButton.Text = SeekerApplication.GetString(Resource.String.back_desc);
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
            SetExportPathLine(Resource.Id.qtExportPath, Resource.String.ImportPathQT, ".scd1");
            SetExportPathLine(Resource.Id.nicotineExportPath, Resource.String.ImportPathNicotine, ".tar.bz2", "config");
            SetExportPathLine(Resource.Id.seekerExportPath, Resource.String.ImportPathSeeker, ".xml");
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

        /// <summary>
        /// Sets a "where to find the export" line, i.e. the translatable menu path followed by
        /// the literal file names / extensions rendered as monospace accent-colored code chunks.
        /// </summary>
        private void SetExportPathLine(int textViewId, int pathStringId, params string[] codeChunks)
        {
            var textView = this.rootView.FindViewById<TextView>(textViewId);
            var accentColor = UiHelpers.GetColorFromAttribute(textView.Context, Resource.Attribute.mainPurple);
            var builder = new Android.Text.SpannableStringBuilder(this.GetString(pathStringId));
            bool firstSeparator = true;
            foreach (string chunk in codeChunks)
            {
                if (firstSeparator)
                {
                    int separatorStart = builder.Length();
                    builder.Append(" — ");
                    var subduedColor = new Color(textView.CurrentTextColor);
                    subduedColor.A = (byte)(subduedColor.A / 2);
                    builder.SetSpan(new Android.Text.Style.ForegroundColorSpan(subduedColor), separatorStart, builder.Length(), Android.Text.SpanTypes.ExclusiveExclusive);
                }
                else
                {
                    builder.Append(" / ");
                }
                firstSeparator = false;
                int start = builder.Length();
                builder.Append(chunk);
                builder.SetSpan(new Android.Text.Style.TypefaceSpan("monospace"), start, builder.Length(), Android.Text.SpanTypes.ExclusiveExclusive);
                builder.SetSpan(new Android.Text.Style.ForegroundColorSpan(accentColor), start, builder.Length(), Android.Text.SpanTypes.ExclusiveExclusive);
                builder.SetSpan(new Android.Text.Style.RelativeSizeSpan(0.95f), start, builder.Length(), Android.Text.SpanTypes.ExclusiveExclusive);
            }
            textView.TextFormatted = builder;
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
        /// <summary>
        /// Raised whenever the user toggles a row, so the fragment can refresh the select-all checkbox.
        /// </summary>
        public event Action SelectionChanged;

        public void SetAll(bool isChecked)
        {
            for (int i = 0; i < localDataSet.Count; i++)
            {
                localDataSet[i].isChecked = isChecked;
            }
        }

        public List<ImportItem> localDataSet; //tab id's
        public override int ItemCount => localDataSet.Count;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            ImportItemView view = ImportItemView.inflate(parent);
            view.setupChildren();
            var holder = new ImportItemViewHolder(view as View);
            (view as View).Click += (object sender, EventArgs e) =>
            {
                int pos = holder.BindingAdapterPosition;
                if (pos == RecyclerView.NoPosition)
                {
                    return;
                }
                localDataSet[pos].isChecked = !localDataSet[pos].isChecked;
                view.ImportItemCheckbox.Checked = localDataSet[pos].isChecked;
                SelectionChanged?.Invoke();
            };
            return holder;


        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as ImportItemViewHolder).pathItemView.setItem(localDataSet[position]);
            (holder as ImportItemViewHolder).pathItemView.SetDividerVisible(position != ItemCount - 1);
        }

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
        public TextView ImportItemText;
        private View importItemDivider;
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
            ImportItemText = FindViewById<TextView>(Resource.Id.importItemText);
            importItemDivider = FindViewById<View>(Resource.Id.importItemDivider);
        }

        public void setItem(ImportItem item)
        {
            InnerImportItem = item;
            if (item.showAsterisk)
            {
                ImportItemText.Text = item.item + "*";
            }
            else
            {
                ImportItemText.Text = item.item;
            }
            ImportItemCheckbox.Checked = item.isChecked;
        }

        public void SetDividerVisible(bool visible)
        {
            importItemDivider.Visibility = visible ? ViewStates.Visible : ViewStates.Gone;
        }
    }



    public class ImportListFragment : AndroidX.Fragment.App.Fragment
    {
        // Mirrors Material's MaterialCheckBox.STATE_* constants. Using local copies
        // because the Xamarin binding surface for these constants varies by version;
        // Java source guarantees these exact int values.
        private const int STATE_UNCHECKED = 0;
        private const int STATE_CHECKED = 1;
        private const int STATE_INDETERMINATE = 2;

        private View rootView;
        private View noneFoundView;
        private TextView noneFound;
        private TextView alreadyAdded;
        private TextView importHeader;
        private Google.Android.Material.CheckBox.MaterialCheckBox selectAllCheckbox;
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
            noneFoundView = this.rootView.FindViewById<View>(Resource.Id.noneFoundView);
            noneFound = this.rootView.FindViewById<TextView>(Resource.Id.noneFound);
            alreadyAdded = this.rootView.FindViewById<TextView>(Resource.Id.alreadyPresentTextView);
            alreadyAdded.MovementMethod = Android.Text.Method.ScrollingMovementMethod.Instance; //capped at 4 lines, scrollable beyond that
            importHeader = this.rootView.FindViewById<TextView>(Resource.Id.selectTheFollowing);
            recyclerView = this.rootView.FindViewById<AndroidX.RecyclerView.Widget.RecyclerView>(Resource.Id.recyclerViewImportList);
            var lm = new LinearLayoutManager(this.Context, LinearLayoutManager.Vertical, false);
            recyclerView.SetLayoutManager(lm);
            recyclerView.ClipToOutline = true; //so row ripples do not draw over the card's rounded corners
            selectAllCheckbox = this.rootView.FindViewById<Google.Android.Material.CheckBox.MaterialCheckBox>(Resource.Id.selectAllCheckbox);
            selectAllCheckbox.Click += SelectAll_Click;

            (SeekerState.ActiveActivityRef as ImportWizardActivity).UpdatePagerReference(this, importListType);
            Console.WriteLine("OnCreateView: " + importListType.ToString() + " " + guid.ToString());
            return rootView;
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {
            //the click already toggled the checkbox (indeterminate counts as unchecked, so
            //clicking an indeterminate checkbox moves it to checked i.e. "select all").
            bool isChecked = selectAllCheckbox.CheckedState == STATE_CHECKED;
            this.importListAdapter.SetAll(isChecked);
            this.importListAdapter.NotifyDataSetChanged();
        }

        private void UpdateSelectAllState()
        {
            if (selectAllCheckbox == null || importListAdapter == null)
            {
                return;
            }
            int checkedCount = importListAdapter.localDataSet.Count((item) => item.isChecked);
            if (checkedCount == 0)
            {
                selectAllCheckbox.CheckedState = STATE_UNCHECKED;
            }
            else if (checkedCount == importListAdapter.localDataSet.Count)
            {
                selectAllCheckbox.CheckedState = STATE_CHECKED;
            }
            else
            {
                selectAllCheckbox.CheckedState = STATE_INDETERMINATE;
            }
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

        private Java.Lang.ICharSequence CreateAlreadyAddedString(IEnumerable<string> usernames, ImportListType listType)
        {
            string alreadyPresentPrefix = listType switch {
                ImportListType.Ignore => SeekerApplication.GetString(Resource.String.ImportIgnoredAlreadyPresent),
                ImportListType.Wishlist => SeekerApplication.GetString(Resource.String.ImportWishlistAlreadyPresent),
                _ => SeekerApplication.GetString(Resource.String.ImportFriendsAlreadyPresent)
            };
            var alreadyAddedNote = new Android.Text.SpannableStringBuilder(alreadyPresentPrefix + " ");

            bool first = true;
            foreach (string name in usernames)
            {
                if (!first)
                {
                    alreadyAddedNote.Append(", ");
                }
                first = false;
                int start = alreadyAddedNote.Length();
                alreadyAddedNote.Append(name);
                alreadyAddedNote.SetSpan(new Android.Text.Style.StyleSpan(TypefaceStyle.Bold), start, alreadyAddedNote.Length(), Android.Text.SpanTypes.ExclusiveExclusive);
            }
            return alreadyAddedNote;
        }

        private void SetAlreadyAddedText(Java.Lang.ICharSequence text)
        {
            alreadyAdded.TextFormatted = text;
            alreadyAdded.ScrollTo(0, 0); //the note is scrollable; reset in case a previous longer note was scrolled
        }

        /// <summary>
        /// Toggles between the list view and the centered empty state.
        /// Empty is "Nothing (new) to import"
        /// </summary>
        private void SetListVisibility(bool hasItemsToImport, bool sourceHadItems)
        {
            if (hasItemsToImport)
            {
                this.recyclerView.Visibility = ViewStates.Visible;
                this.selectAllCheckbox.Visibility = ViewStates.Visible;
                this.noneFoundView.Visibility = ViewStates.Gone;
            }
            else
            {
                this.recyclerView.Visibility = ViewStates.Gone;
                this.selectAllCheckbox.Visibility = ViewStates.Gone;
                this.noneFoundView.Visibility = ViewStates.Visible;
                this.noneFound.SetText(sourceHadItems ? Resource.String.NothingNewToImport : Resource.String.NothingToImport);
            }
        }

        private ImportListAdapter importListAdapter;
        public void SetState(ImportedData data, ImportListType listType)
        {
            if (importHeader == null)
            {
                return;//too early.
            }
            switch (listType)
            {
                case ImportListType.UserList:
                    importHeader.Text = this.GetString(Resource.String.ImportFriends);
                    //todo already present
                    var currentlyHave = CommonState.UserList.Select(item => item.Username).ToList();
                    var notYetAdded = data.UserList.Except(currentlyHave).ToList();
                    var alreadyAddedList = data.UserList.Except(notYetAdded).ToList();
                    if (alreadyAddedList.Count == 0)
                    {
                        alreadyAdded.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        alreadyAdded.Visibility = ViewStates.Visible;
                        SetAlreadyAddedText(CreateAlreadyAddedString(alreadyAddedList, listType));
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

                    importListAdapter.SelectionChanged += UpdateSelectAllState;
                    this.recyclerView.SetAdapter(importListAdapter);
                    UpdateSelectAllState();

                    SetListVisibility(notYetAdded.Count > 0, data.UserList != null && data.UserList.Count > 0);
                    break;
                case ImportListType.Ignore:
                    importHeader.Text = this.GetString(Resource.String.ImportIgnored);
                    //todo already present
                    var currentlyHaveIgnored = CommonState.IgnoreUserList.Select(item => item.Username).ToList();
                    var notYetIgnored = data.IgnoredBanned.Except(currentlyHaveIgnored).ToList();
                    var alreadyIgnoredList = data.IgnoredBanned.Except(notYetIgnored).ToList();
                    if (alreadyIgnoredList.Count == 0)
                    {
                        alreadyAdded.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        alreadyAdded.Visibility = ViewStates.Visible;
                        SetAlreadyAddedText(CreateAlreadyAddedString(alreadyIgnoredList, listType));
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
                    importListAdapter.SelectionChanged += UpdateSelectAllState;
                    this.recyclerView.SetAdapter(importListAdapter);
                    UpdateSelectAllState();
                    SetListVisibility(notYetIgnored.Count > 0, data.IgnoredBanned != null && data.IgnoredBanned.Count > 0);
                    break;
                case ImportListType.UserNotes:
                    importHeader.Text = this.GetString(Resource.String.ImportUserNotes);
                    //todo already present
                    //maybe do asterick
                    var currentlyHaveNoted = UserMetadataService.UserNotes.Select(item => item.Key).ToList();
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
                    importListAdapter.SelectionChanged += UpdateSelectAllState;
                    this.recyclerView.SetAdapter(importListAdapter);
                    UpdateSelectAllState();
                    SetListVisibility(notYetNotedItems.Count > 0, data.UserNotes != null && data.UserNotes.Count > 0);
                    break;
                case ImportListType.Wishlist:
                    importHeader.Text = this.GetString(Resource.String.ImportWishlist);
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
                        SetAlreadyAddedText(CreateAlreadyAddedString(alreadyWishedList, listType));
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
                    importListAdapter.SelectionChanged += UpdateSelectAllState;
                    this.recyclerView.SetAdapter(importListAdapter);
                    UpdateSelectAllState();

                    SetListVisibility(notYetWished.Count > 0, data.Wishlist != null && data.Wishlist.Count > 0);
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

    }



    /// <summary>
    /// Centered "worm" style page indicator. Inactive pages are small dots and the
    /// current page expands into a rounded pill. The position is fractional (driven
    /// by ViewPager.PageScrolled) so the expansion animates with the page transition.
    /// </summary>
    public class PageDotsIndicator : View
    {
        private int mPageCount;
        private float mPosition;

        private readonly float mDotDiameter;
        private readonly float mPillWidth;
        private readonly float mDotSpacing;

        private readonly Paint mPaint;
        private readonly Android.Graphics.Color mActiveColor;
        private readonly Android.Graphics.Color mInactiveColor;

        private readonly RectF mTempRectF = new RectF();

        public PageDotsIndicator(Context context) : this(context, null)
        {
        }

        public PageDotsIndicator(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            float density = this.Resources.DisplayMetrics.Density;
            mDotDiameter = 8f * density;
            mPillWidth = 36f * density;
            mDotSpacing = 8f * density;
            var color = UiHelpers.GetColorFromAttribute(context, Resource.Attribute.mainPurple);
            mActiveColor = color;
            mInactiveColor = Android.Graphics.Color.Argb(0x42, mActiveColor.R, mActiveColor.G, mActiveColor.B);

            mPaint = new Paint(PaintFlags.AntiAlias);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (mPageCount == 0)
            {
                return;
            }

            float position = Math.Clamp(mPosition, 0f, mPageCount - 1);
            float pillExtra = mPillWidth - mDotDiameter;
            // the expansion influences below always sum to 1, so total width is constant
            float totalWidth = mPageCount * mDotDiameter + (mPageCount - 1) * mDotSpacing + pillExtra;
            float left = (this.Width - totalWidth) / 2f;
            float top = this.PaddingTop + (this.Height - this.PaddingTop - this.PaddingBottom - mDotDiameter) / 2f;

            mTempRectF.Top = top;
            mTempRectF.Bottom = top + mDotDiameter;
            float cornerRadius = mDotDiameter / 2f;

            for (int i = 0; i < mPageCount; i++)
            {
                float influence = Math.Max(0f, 1f - Math.Abs(i - position));
                float width = mDotDiameter + pillExtra * influence;
                // visited pages stay fully colored; the upcoming page fades in as it becomes current
                float colorFraction = Math.Clamp(position - i + 1f, 0f, 1f);
                mPaint.Color = BlendColor(mInactiveColor, mActiveColor, colorFraction);

                mTempRectF.Left = left;
                mTempRectF.Right = left + width;
                canvas.DrawRoundRect(mTempRectF, cornerRadius, cornerRadius, mPaint);
                left += width + mDotSpacing;
            }
        }

        private static Android.Graphics.Color BlendColor(Android.Graphics.Color from, Android.Graphics.Color to, float t)
        {
            return Android.Graphics.Color.Argb(
                (int)(from.A + (to.A - from.A) * t),
                (int)(from.R + (to.R - from.R) * t),
                (int)(from.G + (to.G - from.G) * t),
                (int)(from.B + (to.B - from.B) * t));
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int contentWidth = (int)Math.Ceiling(
                mPageCount * mDotDiameter + Math.Max(0, mPageCount - 1) * mDotSpacing + (mPillWidth - mDotDiameter));
            SetMeasuredDimension(
                View.ResolveSize(contentWidth + this.PaddingLeft + this.PaddingRight, widthMeasureSpec),
                View.ResolveSize((int)Math.Ceiling(mDotDiameter) + this.PaddingTop + this.PaddingBottom, heightMeasureSpec));
        }

        public void SetPosition(float position)
        {
            mPosition = position;
            this.Invalidate();
        }

        public void SetPageCount(int count)
        {
            mPageCount = count;
            this.RequestLayout();
            this.Invalidate();
        }
    }
}