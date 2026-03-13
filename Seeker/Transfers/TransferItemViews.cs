using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Text;
using Android.Text.Style;

namespace Seeker
{
    /**
    Notes on Queue Position:
    The default queue position should be int.MaxValue (which we display as not known) not 0.  
    This is the case on QT where we download from an offline user, 
      or in general when we are queued by a user that does not send a queue position (slskd?).
    Both QT and Nicotine display it as "Queued" and then without a queue position (rather than queue position of 0).

    If we are downloading from a user with queue and they then go offline, the QT behavior is to still show "Queued" (nothing changes),
      the nicotine behavior is to change it to "User Logged Off".  I think nicotine behavior is more descriptive and helpful.
    **/

    public interface ITransferItemView
    {
        public ITransferItem InnerTransferItem { get; set; }

        public void setupChildren();

        public void setItem(ITransferItem ti, bool isInBatchMode);

        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }

        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView();

        public View GetStatusDot();

        public TextView GetSizeTextView();

        public TextView GetSpeedTextView();

        public TextView GetSizeSeparatorView();

        public bool GetShowProgressSize();

        public bool GetShowSpeed();
    }

    public class TransferItemViewFolder : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewCurrentFilename;
        private TextView viewNumRemaining;

        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.
        private View statusDot;
        private TextView viewSize;
        private TextView viewSpeed;
        private TextView viewSizeSeparator;
        private ImageView selectionCheckbox;
        private FrameLayout actionContainer;

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView()
        {
            return viewStatusAdditionalInfo;
        }

        public View GetStatusDot()
        {
            return statusDot;
        }

        public TextView GetSizeTextView()
        {
            return viewSize;
        }

        public TextView GetSpeedTextView()
        {
            return viewSpeed;
        }

        public TextView GetSizeSeparatorView()
        {
            return viewSizeSeparator;
        }

        public bool showSize;
        public bool showSpeed;

        public bool GetShowProgressSize()
        {
            return showSize;
        }

        public bool GetShowSpeed()
        {
            return showSpeed;
        }

        public TransferItemViewFolder(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder_showProgressSize, this, true);

            setupChildren();
        }
        public TransferItemViewFolder(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder_showProgressSize, this, true);

            setupChildren();
        }

        public static TransferItemViewFolder inflate(ViewGroup parent, bool _showSize, bool _showSpeed)
        {
            TransferItemViewFolder itemView = null;
            itemView = (TransferItemViewFolder)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_view_folder_dummy_showSizeProgress, parent, false);
            itemView.showSpeed = _showSpeed;
            itemView.showSize = _showSize;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFoldername = FindViewById<TextView>(Resource.Id.textViewFoldername);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);

            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);
            viewNumRemaining = FindViewById<TextView>(Resource.Id.filesRemaining);
            viewCurrentFilename = FindViewById<TextView>(Resource.Id.currentFile);

            statusDot = FindViewById<View>(Resource.Id.statusDot);
            viewSize = FindViewById<TextView>(Resource.Id.textViewSize);
            viewSpeed = FindViewById<TextView>(Resource.Id.textViewSpeed);
            viewSizeSeparator = FindViewById<TextView>(Resource.Id.textViewSizeSeparator);

            selectionCheckbox = FindViewById<ImageView>(Resource.Id.selectionCheckbox);
            actionContainer = FindViewById<FrameLayout>(Resource.Id.actionContainer);

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                viewFoldername.Typeface = Typeface.Create(viewFoldername.Typeface, 600, false);
            }
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu);
        }

        public void setItem(ITransferItem item, bool isInBatchMode)
        {
            InnerTransferItem = item;
            FolderItem folderItem = item as FolderItem;
            viewFoldername.Text = folderItem.GetDisplayFolderName();
            var state = folderItem.GetState(out bool isFailed, out _);

            TransferViewHelper.SetAdditionalStatusText(statusDot, viewStatusAdditionalInfo, viewSizeSeparator, viewSize, viewSpeed, item, state, this.showSize, this.showSpeed);
            TransferViewHelper.SetAdditionalFolderInfoState(viewNumRemaining, viewCurrentFilename, folderItem, state);
            int prog = folderItem.GetFolderProgress(out long totalBytes, out _);
            progressBar.Progress = prog;

            viewUsername.Text = folderItem.Username;
            if (item.IsUpload() && state.HasFlag(TransferStates.Cancelled))
            {
                isFailed = true;
            }
            if (isFailed)
            {
                progressBar.Progress = 100;
            }
            TransferViewHelper.SetProgressBarTint(progressBar, state, isFailed);

            if (isInBatchMode)
            {
                actionContainer.Visibility = ViewStates.Visible;
                selectionCheckbox.Visibility = ViewStates.Visible;
                bool isSelected = TransfersViewState.Instance.BatchSelectedItems.Contains(this.ViewHolder.AbsoluteAdapterPosition);
                selectionCheckbox.SetImageResource(isSelected ? Resource.Drawable.check_circle : Resource.Drawable.check_circle_outline);
                this.Background = isSelected ? Resources.GetDrawable(Resource.Color.batchSelectHighlight, null) : null;
            }
            else
            {
                actionContainer.Visibility = ViewStates.Gone;
                selectionCheckbox.Visibility = ViewStates.Invisible;
                this.Background = null;
            }
        }
    }




    public class TransferViewHelper
    {
        /// <summary>
        /// In Progress = InProgress proper, initializing, requested. 
        /// If In Progress or Queued you should be able to pause it (the official client lets you).
        /// </summary>
        /// <param name="transferItems"></param>
        /// <param name="numInProgress"></param>
        /// <param name="numFailed"></param>
        /// <param name="numPaused"></param>
        /// <param name="numSucceeded"></param>
        public static void GetStatusNumbers(IEnumerable<TransferItem> transferItems, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued)
        {
            numInProgress = 0;
            numFailed = 0;
            numPaused = 0;
            numSucceeded = 0;
            numQueued = 0;
            lock (transferItems)
            {
                foreach (var ti in transferItems)
                {
                    if (ti.State.HasFlag(TransferStates.Queued))
                    {
                        numQueued++;
                    }
                    else if (ti.State.HasFlag(TransferStates.InProgress) || ti.State.HasFlag(TransferStates.Initializing) || ti.State.HasFlag(TransferStates.Requested) || ti.State.HasFlag(TransferStates.Aborted))
                    {
                        numInProgress++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Errored) || ti.State.HasFlag(TransferStates.Rejected) || ti.State.HasFlag(TransferStates.TimedOut))
                    {
                        numFailed++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Cancelled))
                    {
                        numPaused++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Succeeded))
                    {
                        numSucceeded++;
                    }
                }
            }
        }


        public static void SetAdditionalFolderInfoState(TextView filesLongStatus, TextView currentFile, FolderItem fi, TransferStates folderState)
        {
            SetFolderStatusSpannable(filesLongStatus, fi);

            if (folderState.HasFlag(TransferStates.InProgress) || folderState.HasFlag(TransferStates.Initializing) || folderState.HasFlag(TransferStates.Requested) || folderState.HasFlag(TransferStates.Aborted))
            {
                string currentFilename = string.Empty;
                lock (fi.TransferItems)
                {
                    foreach (var ti in fi.TransferItems)
                    {
                        if (ti.State.HasFlag(TransferStates.InProgress))
                        {
                            currentFilename = ti.Filename;
                        }
                    }
                    if (currentFilename == string.Empty)
                    {
                        currentFilename = fi.TransferItems.First().Filename;
                    }
                }

                currentFile.Visibility = ViewStates.Visible;
                currentFile.Text = currentFilename;

                var resources = currentFile.Context.Resources;
                var theme = currentFile.Context.Theme;
                int dlColor = resources.GetColor(Resource.Color.transferChipDownloadingText, theme);
                currentFile.SetTextColor(new Color(dlColor));

                var arrow = resources.GetDrawable(Resource.Drawable.arrow_down, theme);
                int iconSize = (int)(10 * resources.DisplayMetrics.Density);
                arrow.SetBounds(0, 0, iconSize, iconSize);
                arrow.SetTint(dlColor);
                currentFile.SetCompoundDrawables(arrow, null, null, null);
                currentFile.CompoundDrawablePadding = (int)(2 * resources.DisplayMetrics.Density);
            }
            else
            {
                currentFile.Visibility = ViewStates.Gone;
                currentFile.SetCompoundDrawables(null, null, null, null);
            }
        }

        private static void SetFolderStatusSpannable(TextView view, FolderItem fi)
        {
            GetStatusNumbers(fi.TransferItems, out int numInProgress, out int numFailed,
                out int numPaused, out int numSucceeded, out int numQueued);
            int total = fi.TransferItems.Count;

            var resources = view.Context.Resources;
            var theme = view.Context.Theme;
            var sb = new SpannableStringBuilder();
            bool first = true;

            if (numSucceeded > 0) AppendStatusSegment(sb, $"{numSucceeded} done", Resource.Color.transferChipCompletedText, resources, theme, ref first);
            if (numInProgress > 0) AppendStatusSegment(sb, $"{numInProgress} active", Resource.Color.transferChipDownloadingText, resources, theme, ref first);
            if (numFailed > 0) AppendStatusSegment(sb, $"{numFailed} failed", Resource.Color.transferChipFailedText, resources, theme, ref first);
            if (numPaused > 0) AppendStatusSegment(sb, $"{numPaused} paused", Resource.Color.transferChipPausedText, resources, theme, ref first);
            if (numQueued > 0) AppendStatusSegment(sb, $"{numQueued} queued", Resource.Color.transferChipQueuedText, resources, theme, ref first);

            string ofTotal = $" of {total}";
            int subduedColor = resources.GetColor(Resource.Color.transferSpeedSubdued, theme);
            var ofTotalSpan = new SpannableString(ofTotal);
            ofTotalSpan.SetSpan(new ForegroundColorSpan(new Color(subduedColor)), 0, ofTotal.Length, SpanTypes.ExclusiveExclusive);
            sb.Append(ofTotalSpan);

            view.SetText(sb, TextView.BufferType.Spannable);
        }

        private static void AppendStatusSegment(SpannableStringBuilder sb, string text, int colorResId,
            Android.Content.Res.Resources resources, Android.Content.Res.Resources.Theme theme, ref bool first)
        {
            if (!first)
            {
                string sep = " \u00b7 ";
                int subduedColor = resources.GetColor(Resource.Color.transferSpeedSubdued, theme);
                var sepSpan = new SpannableString(sep);
                sepSpan.SetSpan(new ForegroundColorSpan(new Color(subduedColor)), 0, sep.Length, SpanTypes.ExclusiveExclusive);
                sb.Append(sepSpan);
            }
            first = false;

            int color = resources.GetColor(colorResId, theme);
            var segment = new SpannableString(text);
            segment.SetSpan(new ForegroundColorSpan(new Color(color)), 0, text.Length, SpanTypes.ExclusiveExclusive);
            segment.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, text.Length, SpanTypes.ExclusiveExclusive);
            sb.Append(segment);
        }



        public static void SetSizeText(TextView size, int progress, long sizeBytes)
        {
            if (progress == 100)
            {
                size.Text = SimpleHelpers.GetHumanReadableSize(sizeBytes);
            }
            else
            {
                long bytesTransferred = progress * sizeBytes;
                size.Text = SimpleHelpers.GetHumanReadableProgressSize(bytesTransferred / 100, sizeBytes);
            }
        }


        public static void SetViewStatusText(TextView viewStatus, TransferStates state, bool isUpload, bool isFolder)
        {
            if (state.HasFlag(TransferStates.Queued))
            {
                viewStatus.SetText(Resource.String.in_queue);
            }
            else if (state.HasFlag(TransferStates.Cancelled))
            {
                if (isUpload)
                {
                    viewStatus.Text = SeekerApplication.GetString(Resource.String.Aborted);
                }
                else
                {
                    viewStatus.SetText(Resource.String.paused);
                }
            }
            else if (isFolder && state.HasFlag(TransferStates.Rejected)) //if is folder we put the extra info here, else we put it in the additional status TextView
            {
                if (isUpload)
                {
                    viewStatus.Text = System.String.Format("{0} - {1}", SeekerApplication.GetString(Resource.String.failed), SeekerApplication.GetString(Resource.String.Cancelled));//if the user on the other end cancelled / paused / removed it.
                }
                else
                {
                    viewStatus.SetText(Resource.String.failed_denied);
                }
            }
            else if (isFolder && state.HasFlag(TransferStates.UserOffline))
            {
                viewStatus.SetText(Resource.String.failed_user_offline);
            }
            else if (isFolder && state.HasFlag(TransferStates.CannotConnect))
            {
                viewStatus.Text = System.String.Format("{0} - {1}", SeekerApplication.GetString(Resource.String.failed), SeekerApplication.GetString(Resource.String.CannotConnect));
                //"cannot connect" is too long for average screen. but the root problem needs to be fixed (for folder combine two TextView into one with padding???? TODO)
            }
            else if (state.HasFlag(TransferStates.Rejected) || state.HasFlag(TransferStates.TimedOut) || state.HasFlag(TransferStates.Errored))
            {
                viewStatus.SetText(Resource.String.failed);
            }
            else if (state.HasFlag(TransferStates.Initializing) || state.HasFlag(TransferStates.Requested))  //item.State.HasFlag(TransferStates.None) captures EVERYTHING!!
            {
                viewStatus.SetText(Resource.String.not_started);
            }
            else if (state.HasFlag(TransferStates.InProgress))
            {
                viewStatus.SetText(Resource.String.in_progress);
            }
            else if (state.HasFlag(TransferStates.Succeeded))
            {
                viewStatus.SetText(Resource.String.completed);
            }
            else if (state.HasFlag(TransferStates.Aborted))
            {
                // this is the case that the filesize is wrong. In that case we always immediately re-request.
                viewStatus.SetText(Resource.String.re_requesting);
            }
            else
            {
                //these views are recycled, so NEVER dont set them.
                //otherwise they will be whatever the view they recycled was.
                //so they may end up being Failed, Completed, etc.
                //viewStatus.Text = "None";
                
                viewStatus.SetText(Resource.String.not_started);
            }
        }


        public static string GetTimeRemainingString(TimeSpan? timeSpan)
        {
            if (timeSpan == null)
            {
                return SeekerState.ActiveActivityRef.GetString(Resource.String.unknown);
            }
            else
            {
                string[] hms = timeSpan.ToString().Split(':');
                string h = hms[0].TrimStart('0');
                if (h == string.Empty)
                {
                    h = "0";
                }
                string m = hms[1].TrimStart('0');
                if (m == string.Empty)
                {
                    m = "0";
                }
                string s = hms[2].TrimStart('0');
                if (s.Contains('.'))
                {
                    s = s.Substring(0, s.IndexOf('.'));
                }
                if (s == string.Empty)
                {
                    s = "0";
                }
                //it will always be length 3.  if the seconds is more than a day it will be like "[13.21:53:20]" and if just 2 it will be like "[00:00:02]"
                if (h != "0")
                {
                    //we have hours
                    return h + "h:" + m + "m:" + s + "s";
                }
                else if (m != "0")
                {
                    return m + "m:" + s + "s";
                }
                else
                {
                    return s + "s";
                }
            }
        }

        private enum TransferChipType
        {
            Completed,
            Downloading,
            Queued,
            Paused,
            Failed
        }

        private static int GetChipTextColorResId(TransferChipType chipType)
        {
            switch (chipType)
            {
                case TransferChipType.Completed:
                    return Resource.Color.transferChipCompletedText;
                case TransferChipType.Downloading:
                    return Resource.Color.transferChipDownloadingText;
                case TransferChipType.Queued:
                    return Resource.Color.transferChipQueuedText;
                case TransferChipType.Paused:
                    return Resource.Color.transferChipPausedText;
                case TransferChipType.Failed:
                default:
                    return Resource.Color.transferChipFailedText;
            }
        }

        private static void StyleStatusIndicator(View dot, TextView text, string label, TransferChipType chipType)
        {
            if (label == string.Empty)
            {
                dot.Visibility = ViewStates.Gone;
                text.Visibility = ViewStates.Gone;
                return;
            }
            dot.Visibility = ViewStates.Visible;
            text.Visibility = ViewStates.Visible;
            text.Text = label;

            int textColorResId = GetChipTextColorResId(chipType);

            var resources = text.Context.Resources;
            var theme = text.Context.Theme;
            int color = resources.GetColor(textColorResId, theme);

            text.SetTextColor(new Color(color));
            text.SetBackgroundColor(Color.Transparent);
            text.SetTypeface(text.Typeface, Android.Graphics.TypefaceStyle.Bold);
            text.SetTextSize(ComplexUnitType.Sp, 10);

            var bg = dot.Background?.Mutate() as GradientDrawable;
            if (bg != null)
            {
                bg.SetColor(color);
            }
        }

        public static void SetSpeedText(TextView speedView, ITransferItem item, TransferStates state)
        {
            double avgSpeed = item.GetAvgSpeed();
            var resources = speedView.Context.Resources;
            var theme = speedView.Context.Theme;
            if ((state.HasFlag(TransferStates.InProgress) || state.HasFlag(TransferStates.Initializing) || state.HasFlag(TransferStates.Requested)) && avgSpeed > 0)
            {
                speedView.Visibility = ViewStates.Visible;
                speedView.Text = SimpleHelpers.GetTransferSpeedString(avgSpeed);
                int color = resources.GetColor(Resource.Color.transferChipDownloadingText, theme);
                speedView.SetTextColor(new Color(color));
                speedView.SetTypeface(speedView.Typeface, TypefaceStyle.Bold);
            }
            else if (state.HasFlag(TransferStates.Succeeded) && avgSpeed > 0)
            {
                speedView.Visibility = ViewStates.Visible;
                speedView.Text = SimpleHelpers.GetTransferSpeedString(avgSpeed);
                //speedView.SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(speedView.Context, Resource.Attribute.transferSpeedSubdued));
                int color = resources.GetColor(Resource.Color.transferSpeedSubdued, theme);
                speedView.SetTextColor(new Color(color));
                speedView.SetTypeface(speedView.Typeface, TypefaceStyle.Normal);
            }
            else
            {
                speedView.Visibility = ViewStates.Gone;
            }
        }

        public static void SetProgressBarTint(ProgressBar pb, TransferStates state, bool isFailed)
        {
            int colorResId;
            if (isFailed)
            {
                colorResId = Resource.Color.transferChipFailedText;
            }
            else if (state.HasFlag(TransferStates.Succeeded))
            {
                colorResId = Resource.Color.transferChipCompletedText;
            }
            else if (state.HasFlag(TransferStates.Cancelled))
            {
                colorResId = Resource.Color.transferChipPausedText;
            }
            else if (state.HasFlag(TransferStates.Queued))
            {
                colorResId = Resource.Color.transferChipQueuedText;
            }
            else
            {
                colorResId = Resource.Color.transferChipDownloadingText;
            }

            var resources = pb.Context.Resources;
            var theme = pb.Context.Theme;
            int color = resources.GetColor(colorResId, theme);

#pragma warning disable 0618
            if (OperatingSystem.IsAndroidVersionAtLeast(21))
            {
                pb.ProgressTintList = ColorStateList.ValueOf(new Color(color));
            }
            else
            {
                pb.ProgressDrawable.SetColorFilter(new Color(color), PorterDuff.Mode.Multiply);
            }
#pragma warning restore 0618
        }

        public static void SetAdditionalStatusText(
            View statusDot, TextView statusText, TextView sizeSeparator,
            TextView sizeView, TextView speedView,
            ITransferItem item, TransferStates state, bool showSize, bool showSpeed)
        {
            // Status label + dot
            if (state.HasFlag(TransferStates.Succeeded))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.completed), TransferChipType.Completed);
            }
            else if (state.HasFlag(TransferStates.InProgress))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.in_progress), TransferChipType.Downloading);
            }
            else if (state.HasFlag(TransferStates.Initializing) || state.HasFlag(TransferStates.Requested))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.not_started), TransferChipType.Downloading);
            }
            else if (state.HasFlag(TransferStates.Queued))
            {
                string label = SeekerApplication.GetString(Resource.String.in_queue);
                if (!item.IsUpload())
                {
                    int queueLen = item.GetQueueLength();
                    if (queueLen != int.MaxValue)
                    {
                        label += " " + string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.position_), queueLen.ToString());
                    }
                }
                StyleStatusIndicator(statusDot, statusText, label, TransferChipType.Queued);
            }
            else if (state.HasFlag(TransferStates.Cancelled))
            {
                string label = item.IsUpload() ? SeekerApplication.GetString(Resource.String.Aborted) : SeekerApplication.GetString(Resource.String.paused);
                StyleStatusIndicator(statusDot, statusText, label, TransferChipType.Paused);
            }
            else if (state.HasFlag(TransferStates.Rejected))
            {
                string label;
                if (item.IsUpload())
                {
                    label = SeekerApplication.GetString(Resource.String.Cancelled);
                }
                else
                {
                    label = SeekerApplication.GetString(Resource.String.denied);
                }
                StyleStatusIndicator(statusDot, statusText, label, TransferChipType.Failed);
            }
            else if (state.HasFlag(TransferStates.TimedOut))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.TimedOut), TransferChipType.Failed);
            }
            else if (state.HasFlag(TransferStates.UserOffline))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.UserIsOffline), TransferChipType.Failed);
            }
            else if (state.HasFlag(TransferStates.CannotConnect))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.CannotConnect), TransferChipType.Failed);
            }
            else if (item is TransferItem ti2 && ti2.TransferItemExtra.HasFlag(TransferItemExtras.DirNotSet))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.DirectoryNotSet), TransferChipType.Failed);
            }
            else if (state.HasFlag(TransferStates.Aborted))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.re_requesting), TransferChipType.Downloading);
            }
            else if (state.HasFlag(TransferStates.Errored))
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.failed), TransferChipType.Failed);
            }
            else if (!item.IsUpload() && state == TransferStates.None)
            {
                StyleStatusIndicator(statusDot, statusText, SeekerApplication.GetString(Resource.String.paused), TransferChipType.Paused);
            }
            else
            {
                StyleStatusIndicator(statusDot, statusText, "", TransferChipType.Queued);
            }

            // Inline size text
            if (showSize && sizeView != null)
            {
                sizeView.Visibility = ViewStates.Visible;
                if (sizeSeparator != null)
                {
                    sizeSeparator.Visibility = ViewStates.Visible;
                }
                if (item is TransferItem ti)
                {
                    SetSizeText(sizeView, ti.Progress, ti.Size);
                }
                else if (item is FolderItem fi)
                {
                    int prog = fi.GetFolderProgress(out long totalBytes, out _);
                    SetSizeText(sizeView, prog, totalBytes);
                }
            }
            else
            {
                if (sizeView != null)
                {
                    sizeView.Visibility = ViewStates.Gone;
                }
                if (sizeSeparator != null)
                {
                    sizeSeparator.Visibility = ViewStates.Gone;
                }
            }

            // Speed text
            if (showSpeed && speedView != null)
            {
                SetSpeedText(speedView, item, state);
            }
            else if (speedView != null)
            {
                speedView.Visibility = ViewStates.Gone;
            }
        }
    }


    public class TransferItemViewDetails : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewFilename;

        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.
        private View statusDot;
        private TextView viewSize;
        private TextView viewSpeed;
        private TextView viewSizeSeparator;
        private ImageView selectionCheckbox;
        private FrameLayout actionContainer;

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView()
        {
            return viewStatusAdditionalInfo;
        }

        public View GetStatusDot()
        {
            return statusDot;
        }

        public TextView GetSizeTextView()
        {
            return viewSize;
        }

        public TextView GetSpeedTextView()
        {
            return viewSpeed;
        }

        public TextView GetSizeSeparatorView()
        {
            return viewSizeSeparator;
        }

        public bool GetShowProgressSize()
        {
            return showSizes;
        }
        public bool GetShowSpeed()
        {
            return showSpeed;
        }


        public bool showSpeed;
        public bool showSizes;
        public TransferItemViewDetails(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed_sizeProgressBar, this, true);
            setupChildren();
        }
        public TransferItemViewDetails(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed_sizeProgressBar, this, true);
            setupChildren();
        }

        public static TransferItemViewDetails inflate(ViewGroup parent, bool _showSizes, bool _showSpeed)
        {

            TransferItemViewDetails itemView = null;
            itemView = (TransferItemViewDetails)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_details_dummy_showProgressSize, parent, false);
            itemView.showSpeed = _showSpeed;
            itemView.showSizes = _showSizes;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFilename = FindViewById<TextView>(Resource.Id.textViewFileName);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);

            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);

            statusDot = FindViewById<View>(Resource.Id.statusDot);
            viewSize = FindViewById<TextView>(Resource.Id.textViewSize);
            viewSpeed = FindViewById<TextView>(Resource.Id.textViewSpeed);
            viewSizeSeparator = FindViewById<TextView>(Resource.Id.textViewSizeSeparator);

            selectionCheckbox = FindViewById<ImageView>(Resource.Id.selectionCheckbox);
            actionContainer = FindViewById<FrameLayout>(Resource.Id.actionContainer);

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                viewFilename.Typeface = Typeface.Create(viewFilename.Typeface, 600, false);
            }
        }




        public void setItem(ITransferItem item, bool isInBatchMode)
        {
            InnerTransferItem = item;
            TransferItem ti = item as TransferItem;
            viewFilename.Text = ti.Filename;
            progressBar.Progress = ti.Progress;
            TransferViewHelper.SetAdditionalStatusText(statusDot, viewStatusAdditionalInfo, viewSizeSeparator, viewSize, viewSpeed, ti, ti.State, this.showSizes, this.showSpeed);
            viewUsername.Text = ti.Username;
            bool isFailedOrAborted = ti.Failed;
            if (item.IsUpload() && ti.State.HasFlag(TransferStates.Cancelled))
            {
                isFailedOrAborted = true;
            }
            if (isFailedOrAborted)
            {
                progressBar.Progress = 100;
            }
            TransferViewHelper.SetProgressBarTint(progressBar, ti.State, isFailedOrAborted);

            if (isInBatchMode)
            {
                actionContainer.Visibility = ViewStates.Visible;
                selectionCheckbox.Visibility = ViewStates.Visible;
                bool isSelected = TransfersViewState.Instance.BatchSelectedItems.Contains(this.ViewHolder.AbsoluteAdapterPosition);
                selectionCheckbox.SetImageResource(isSelected ? Resource.Drawable.check_circle : Resource.Drawable.check_circle_outline);
                this.Background = isSelected ? Resources.GetDrawable(Resource.Color.batchSelectHighlight, null) : null;
            }
            else
            {
                actionContainer.Visibility = ViewStates.Gone;
                selectionCheckbox.Visibility = ViewStates.Invisible;
                this.Background = null;
            }
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu);
            //AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo) menuInfo;
            menu.Add(0, 0, 0, Resource.String.retry_dl);
            menu.Add(1, 1, 1, Resource.String.clear_from_list);
            menu.Add(2, 2, 2, Resource.String.cancel_and_clear);
        }
    }


}