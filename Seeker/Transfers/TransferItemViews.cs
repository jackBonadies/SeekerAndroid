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
using AndroidX.RecyclerView.Widget;
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

        public void setItem(ITransferItem ti, bool isInBatchMode);

        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }

        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView();

        public View GetStatusDot();

        public TextView GetSizeTextView();

        public TextView GetSpeedTextView();

        public TextView GetSizeSeparatorView();

        public TextView GetTimeRemainingTextView();

        public TextView GetTimeRemainingSeparatorView();

        public bool GetShowProgressSize();

        public bool GetShowSpeed();

        public bool GetShowTimeRemaining();
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
        private TextView viewTimeRemaining;
        private TextView viewTimeRemainingSeparator;
        private ImageView selectionCheckbox;
        private FrameLayout actionContainer;

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }
        public Transfers.SegmentedProgressBar segmentedProgressBar;

        private Drawable cachedDownloadArrow;
        private Drawable cachedUploadArrow;
        private ImageSpan cachedDownloadArrowSpan;
        private ImageSpan cachedUploadArrowSpan;
        private int cachedDlColor;

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

        public TextView GetTimeRemainingTextView()
        {
            return viewTimeRemaining;
        }

        public TextView GetTimeRemainingSeparatorView()
        {
            return viewTimeRemainingSeparator;
        }

        public bool showSize;
        public bool showSpeed;
        public bool showTimeRemaining;

        public bool GetShowProgressSize()
        {
            return showSize;
        }

        public bool GetShowSpeed()
        {
            return showSpeed;
        }

        public bool GetShowTimeRemaining()
        {
            return showTimeRemaining;
        }

        public TransferItemViewFolder(Context context) : base(context)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_folder_item, this, true);
            setupChildren();
        }

        public static TransferItemViewFolder Create(ViewGroup parent, bool showSize, bool showSpeed, bool showTimeRemaining)
        {
            var itemView = new TransferItemViewFolder(parent.Context);
            itemView.LayoutParameters = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            itemView.showSpeed = showSpeed;
            itemView.showSize = showSize;
            itemView.showTimeRemaining = showTimeRemaining;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFoldername = FindViewById<TextView>(Resource.Id.textViewFoldername);
            segmentedProgressBar = FindViewById<Transfers.SegmentedProgressBar>(Resource.Id.segmentedProgressBar);
            progressBar = null;

            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);
            viewNumRemaining = FindViewById<TextView>(Resource.Id.filesRemaining);
            viewCurrentFilename = FindViewById<TextView>(Resource.Id.currentFile);

            statusDot = FindViewById<View>(Resource.Id.statusDot);
            viewSize = FindViewById<TextView>(Resource.Id.textViewSize);
            viewSpeed = FindViewById<TextView>(Resource.Id.textViewSpeed);
            viewSizeSeparator = FindViewById<TextView>(Resource.Id.textViewSizeSeparator);
            viewTimeRemaining = FindViewById<TextView>(Resource.Id.textViewTimeRemaining);
            viewTimeRemainingSeparator = FindViewById<TextView>(Resource.Id.textViewTimeRemainingSeparator);

            selectionCheckbox = FindViewById<ImageView>(Resource.Id.selectionCheckbox);
            actionContainer = FindViewById<FrameLayout>(Resource.Id.actionContainer);

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                viewFoldername.Typeface = Typeface.Create(viewFoldername.Typeface, 600, false);
            }

            var resources = Context.Resources;
            var theme = Context.Theme;
            cachedDlColor = resources.GetColor(Resource.Color.transferChipDownloadingText, theme);
            int iconSize = (int)(10 * resources.DisplayMetrics.Density);
            int arrowOffsetUp = (int)(1.6 * resources.DisplayMetrics.Density);
            cachedDownloadArrow = resources.GetDrawable(Resource.Drawable.arrow_down, theme);
            cachedDownloadArrow.SetTint(cachedDlColor);
            cachedUploadArrow = resources.GetDrawable(Resource.Drawable.arrow_up, theme);
            cachedUploadArrow.SetTint(cachedDlColor);

            // InsetDrawable shifts the arrow up by arrowOffset: ImageSpan anchors on the
            // inset wrapper's bounds (iconSize tall), the inset moves the arrow within it.
            var downloadInset = new InsetDrawable(cachedDownloadArrow, 0, -arrowOffsetUp, 0, arrowOffsetUp);
            downloadInset.SetBounds(0, 0, iconSize, iconSize);
            var uploadInset = new InsetDrawable(cachedUploadArrow, 0, -arrowOffsetUp, 0, arrowOffsetUp);
            uploadInset.SetBounds(0, 0, iconSize, iconSize);
            cachedDownloadArrowSpan = new ImageSpan(downloadInset, SpanAlign.Bottom);
            cachedUploadArrowSpan = new ImageSpan(uploadInset, SpanAlign.Bottom);
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

            TransferViewHelper.SetAdditionalStatusText(statusDot, viewStatusAdditionalInfo, viewSizeSeparator, viewSize, viewSpeed, item, state, this.showSize, this.showSpeed, isFolder: true);
            TransferViewHelper.SetTimeRemainingText(viewTimeRemainingSeparator, viewTimeRemaining, viewSpeed, item, this.showTimeRemaining);
            var arrowSpan = folderItem.IsUpload() ? cachedUploadArrowSpan : cachedDownloadArrowSpan;
            TransferViewHelper.SetAdditionalFolderInfoState(viewNumRemaining, viewCurrentFilename, folderItem, state, arrowSpan, cachedDlColor);
            TransferViewHelper.UpdateSegmentedProgressBar(segmentedProgressBar, folderItem);

            viewUsername.Text = folderItem.Username;

            if (isInBatchMode)
            {
                actionContainer.Visibility = ViewStates.Visible;
                selectionCheckbox.Visibility = ViewStates.Visible;
                bool isSelected = TransfersViewState.Instance.BatchSelectedItems.Contains(item);
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


        public static void SetAdditionalFolderInfoState(TextView filesLongStatus, TextView currentFile, FolderItem fi, TransferStates folderState, ImageSpan cachedArrowSpan, int dlColor)
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

                var spannable = new SpannableString("  " + currentFilename);
                spannable.SetSpan(cachedArrowSpan, 0, 1, SpanTypes.ExclusiveExclusive);

                currentFile.Visibility = ViewStates.Visible;
                currentFile.SetTextColor(new Color(dlColor));
                // Ellipsize only works with BufferType Normal not Spannable
                //   Normal: creates a readonly copy
                //   Spannable: allows you to change styling at runtime
                currentFile.SetText(spannable, TextView.BufferType.Normal);
            }
            else
            {
                currentFile.Visibility = ViewStates.Gone;
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



        public static void SetSizeText(TextView size, long bytesTransferred, long sizeBytes)
        {
            if (sizeBytes > 0 && bytesTransferred >= sizeBytes)
            {
                size.Text = SimpleHelpers.GetHumanReadableSize(sizeBytes);
            }
            else
            {
                size.Text = SimpleHelpers.GetHumanReadableProgressSize(bytesTransferred, sizeBytes);
            }
        }


        public static void SetTimeRemainingText(TextView separator, TextView timeRemainingView, TextView speedView, ITransferItem item, bool showTimeRemaining)
        {
            TimeSpan? remaining = showTimeRemaining ? item.GetRemainingTime() : null;
            if (remaining == null)
            {
                timeRemainingView.Visibility = ViewStates.Gone;
                separator.Visibility = ViewStates.Gone;
                return;
            }
            timeRemainingView.Text = SimpleHelpers.FormatTimeRemaining(remaining.Value);
            timeRemainingView.Visibility = ViewStates.Visible;
            separator.Visibility = speedView.Visibility == ViewStates.Visible ? ViewStates.Visible : ViewStates.Gone;
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

        private static int GetChipBgColorResId(TransferChipType chipType)
        {
            switch (chipType)
            {
                case TransferChipType.Completed:
                    return Resource.Color.transferChipCompletedBg;
                case TransferChipType.Downloading:
                    return Resource.Color.transferChipDownloadingBg;
                case TransferChipType.Queued:
                    return Resource.Color.transferChipQueuedBg;
                case TransferChipType.Paused:
                    return Resource.Color.transferChipPausedBg;
                case TransferChipType.Failed:
                default:
                    return Resource.Color.transferChipFailedBg;
            }
        }

        // folder rows: filled tonal pill, no dot
        private static void StyleStatusChip(View dot, TextView text, string label, TransferChipType chipType)
        {
            dot.Visibility = ViewStates.Gone;
            if (label == string.Empty)
            {
                text.Visibility = ViewStates.Gone;
                return;
            }
            text.Visibility = ViewStates.Visible;
            text.Text = label;

            var resources = text.Context.Resources;
            var theme = text.Context.Theme;
            int textColor = resources.GetColor(GetChipTextColorResId(chipType), theme);
            int bgColor = resources.GetColor(GetChipBgColorResId(chipType), theme);

            text.SetTextColor(new Color(textColor));

            var bg = text.Background?.Mutate() as GradientDrawable;
            if (bg != null)
            {
                bg.SetColor(bgColor);
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

        // Cached typeface so we dont recreate it every time
        private static Typeface speedFaceNormal;
        private static Typeface speedFaceBold;

        private static void SetSpeedTypeface(TextView speedView, bool bold)
        {
            if (speedFaceBold == null)
            {
                speedFaceNormal = speedView.Typeface ?? Typeface.Default;
                speedFaceBold = Typeface.Create(speedFaceNormal, TypefaceStyle.Bold);
            }
            speedView.Typeface = bold ? speedFaceBold : speedFaceNormal;
        }

        public static void SetSpeedText(TextView speedView, ITransferItem item, TransferStates state)
        {
            double avgSpeed = item.GetAvgSpeed();
            if (avgSpeed <= 0)
            {
                speedView.Visibility = ViewStates.Gone;
                return;
            }
            var resources = speedView.Context.Resources;
            var theme = speedView.Context.Theme;
            speedView.Visibility = ViewStates.Visible;
            speedView.Text = SimpleHelpers.GetTransferSpeedString(avgSpeed);
            if (state.HasFlag(TransferStates.Succeeded))
            {
                int color = resources.GetColor(Resource.Color.transferSpeedSubdued, theme);
                speedView.SetTextColor(new Color(color));
                SetSpeedTypeface(speedView, bold: false);
            }
            else
            {
                int color = resources.GetColor(Resource.Color.transferChipDownloadingText, theme);
                speedView.SetTextColor(new Color(color));
                SetSpeedTypeface(speedView, bold: true);
            }
        }

        public static void UpdateSegmentedProgressBar(Transfers.SegmentedProgressBar bar, FolderItem fi)
        {
            long bytesSucceeded = 0;
            long bytesInProgress = 0;
            long bytesFailed = 0;
            long bytesNotYet = 0;
            long bytesPaused = 0;

            lock (fi.TransferItems)
            {
                foreach (var ti in fi.TransferItems)
                {
                    long size = ti.Size;
                    if (ti.State.HasFlag(TransferStates.Succeeded))
                    {
                        bytesSucceeded += size;
                    }
                    else if (ti.State.HasFlag(TransferStates.InProgress) || ti.State.HasFlag(TransferStates.Initializing) || ti.State.HasFlag(TransferStates.Requested) || ti.State.HasFlag(TransferStates.Aborted))
                    {
                        long completedBytes = System.Math.Min(ti.GetBytesTransferred(), size);
                        bytesInProgress += completedBytes;
                        bytesNotYet += size - completedBytes;
                    }
                    else if (ti.State.HasFlag(TransferStates.Errored) || ti.State.HasFlag(TransferStates.Rejected) || ti.State.HasFlag(TransferStates.TimedOut))
                    {
                        bytesFailed += size;
                    }
                    else if (ti.State.HasFlag(TransferStates.Cancelled))
                    {
                        long completedBytes = System.Math.Min(ti.GetBytesTransferred(), size);
                        bytesPaused += completedBytes;
                        bytesNotYet += size - completedBytes;
                    }
                    else
                    {
                        bytesNotYet += size;
                    }
                }
            }

            bar.SetSegments(bytesSucceeded, bytesInProgress, bytesNotYet, bytesFailed, bytesPaused);
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

            pb.ProgressTintList = ColorStateList.ValueOf(new Color(color));
        }

        public static void SetAdditionalStatusText(
            View statusDot, TextView statusText, TextView sizeSeparator,
            TextView sizeView, TextView speedView,
            ITransferItem item, TransferStates state, bool showSize, bool showSpeed, bool isFolder = false)
        {
            Action<View, TextView, string, TransferChipType> StyleStatus = isFolder
                ? (Action<View, TextView, string, TransferChipType>)StyleStatusChip
                : StyleStatusIndicator;

            // Status label + dot
            if (state.HasFlag(TransferStates.Succeeded))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.completed), TransferChipType.Completed);
            }
            else if (state.HasFlag(TransferStates.InProgress))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.in_progress), TransferChipType.Downloading);
            }
            else if (state.HasFlag(TransferStates.Initializing))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.starting), TransferChipType.Downloading);
            }
            else if (state.HasFlag(TransferStates.Requested))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.requested), TransferChipType.Downloading);
            }
            else if (!item.IsUpload() && state.HasFlag(TransferStates.Queued) && state.HasFlag(TransferStates.Locally))
            {
                // the earliest state
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.pending), TransferChipType.Queued);
            }
            else if (state.HasFlag(TransferStates.Queued))
            {
                string label = SeekerApplication.GetString(Resource.String.queued);
                if (!item.IsUpload())
                {
                    int queueLen = item.GetQueueLength();
                    if (queueLen != int.MaxValue)
                    {
                        label += $" #{queueLen.ToString()}";
                    }
                }
                StyleStatus(statusDot, statusText, label, TransferChipType.Queued);
            }
            else if (state.HasFlag(TransferStates.Cancelled))
            {
                string label = item.IsUpload() ? SeekerApplication.GetString(Resource.String.Aborted) : SeekerApplication.GetString(Resource.String.paused);
                StyleStatus(statusDot, statusText, label, TransferChipType.Paused);
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
                StyleStatus(statusDot, statusText, label, TransferChipType.Failed);
            }
            else if (state.HasFlag(TransferStates.TimedOut))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.TimedOut), TransferChipType.Failed);
            }
            else if (state.HasFlag(TransferStates.UserOffline))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.UserIsOffline), TransferChipType.Failed);
            }
            else if (state.HasFlag(TransferStates.CannotConnect))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.CannotConnect), TransferChipType.Failed);
            }
            else if (item is TransferItem ti2 && ti2.TransferItemExtra.HasFlag(TransferItemExtras.DirNotSet))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.DirectoryNotSet), TransferChipType.Failed);
            }
            else if (state.HasFlag(TransferStates.Aborted))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.re_requesting), TransferChipType.Downloading);
            }
            else if (state.HasFlag(TransferStates.Errored))
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.failed), TransferChipType.Failed);
            }
            else if (!item.IsUpload() && state == TransferStates.None)
            {
                StyleStatus(statusDot, statusText, SeekerApplication.GetString(Resource.String.paused), TransferChipType.Paused);
            }
            else
            {
                StyleStatus(statusDot, statusText, "", TransferChipType.Queued);
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
                    SetSizeText(sizeView, ti.GetBytesTransferred(), ti.Size);
                }
                else if (item is FolderItem fi)
                {
                    var (totalBytes, completedBytes) = fi.GetFolderProgress();
                    SetSizeText(sizeView, completedBytes, totalBytes);
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
        private TextView viewTimeRemaining;
        private TextView viewTimeRemainingSeparator;
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

        public TextView GetTimeRemainingTextView()
        {
            return viewTimeRemaining;
        }

        public TextView GetTimeRemainingSeparatorView()
        {
            return viewTimeRemainingSeparator;
        }

        public bool GetShowProgressSize()
        {
            return showSizes;
        }
        public bool GetShowSpeed()
        {
            return showSpeed;
        }

        public bool GetShowTimeRemaining()
        {
            return showTimeRemaining;
        }


        public bool showSpeed;
        public bool showSizes;
        public bool showTimeRemaining;
        public TransferItemViewDetails(Context context) : base(context)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_single_item, this, true);
            setupChildren();
        }

        public static TransferItemViewDetails Create(ViewGroup parent, bool showSizes, bool showSpeed, bool showTimeRemaining)
        {
            var itemView = new TransferItemViewDetails(parent.Context);
            itemView.LayoutParameters = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            itemView.showSpeed = showSpeed;
            itemView.showSizes = showSizes;
            itemView.showTimeRemaining = showTimeRemaining;
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
            viewTimeRemaining = FindViewById<TextView>(Resource.Id.textViewTimeRemaining);
            viewTimeRemainingSeparator = FindViewById<TextView>(Resource.Id.textViewTimeRemainingSeparator);

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
            progressBar.Progress = ti.GetProgressForPresentation();
            TransferViewHelper.SetAdditionalStatusText(statusDot, viewStatusAdditionalInfo, viewSizeSeparator, viewSize, viewSpeed, ti, ti.State, this.showSizes, this.showSpeed);
            TransferViewHelper.SetTimeRemainingText(viewTimeRemainingSeparator, viewTimeRemaining, viewSpeed, ti, this.showTimeRemaining);
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
                bool isSelected = TransfersViewState.Instance.BatchSelectedItems.Contains(item);
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