using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public TextView GetProgressSizeTextView();

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

        private TextView viewProgressSize;
        private TextView viewStatus; //In Queue, Failed, Done, In Progress
        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView()
        {
            return viewStatusAdditionalInfo;
        }

        public TextView GetProgressSizeTextView()
        {
            return viewProgressSize;
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

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder_showProgressSize, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder, this, true);
            }

            setupChildren();
        }
        public TransferItemViewFolder(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder_showProgressSize, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder, this, true);
            }

            setupChildren();
        }

        public static TransferItemViewFolder inflate(ViewGroup parent, bool _showSize, bool _showSpeed)
        {
            TransferItemViewFolder itemView = null;
            if (_showSize)
            {
                itemView = (TransferItemViewFolder)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_view_folder_dummy_showSizeProgress, parent, false);
            }
            else
            {
                itemView = (TransferItemViewFolder)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_view_folder_dummy, parent, false);
            }
            itemView.showSpeed = _showSpeed;
            itemView.showSize = _showSize;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFoldername = FindViewById<TextView>(Resource.Id.textViewFoldername);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);
            viewProgressSize = FindViewById<TextView>(Resource.Id.textViewProgressSize);

            viewStatus = FindViewById<TextView>(Resource.Id.textViewStatus);
            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);
            viewNumRemaining = FindViewById<TextView>(Resource.Id.filesRemaining);
            viewCurrentFilename = FindViewById<TextView>(Resource.Id.currentFile);
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


            TransferViewHelper.SetViewStatusText(viewStatus, state, item.IsUpload(), true);
            TransferViewHelper.SetAdditionalStatusText(viewStatusAdditionalInfo, item, state, true); //TODOTODO
            TransferViewHelper.SetAdditionalFolderInfoState(viewNumRemaining, viewCurrentFilename, folderItem, state);
            int prog = folderItem.GetFolderProgress(out long totalBytes, out _);
            progressBar.Progress = prog;
            if (this.showSize)
            {
                (viewProgressSize as TransfersFragment.ProgressSizeTextView).Progress = prog;
                TransferViewHelper.SetSizeText(viewProgressSize, prog, totalBytes);
            }


            viewUsername.Text = folderItem.Username;
            if (item.IsUpload() && state.HasFlag(TransferStates.Cancelled))
            {
                isFailed = true;
            }
            if (isFailed)//state.HasFlag(TransferStates.Errored) || state.HasFlag(TransferStates.Rejected) || state.HasFlag(TransferStates.TimedOut))
            {
                progressBar.Progress = 100;
                if (this.showSize)
                {
                    (viewProgressSize as Seeker.TransfersFragment.ProgressSizeTextView).Progress = 100;
                }
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.Red);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.Red, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            else
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            if (isInBatchMode && TransfersFragment.BatchSelectedItems.Contains(this.ViewHolder.AbsoluteAdapterPosition))
            {
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                    //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
            else
            {
                //this.Background
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                                           //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Color.cellback);
                                           //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
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
            //if in progress, X files remaining, Current File:
            //if in queue, ^ ^ (or initializing, requesting, basically if in progress in the literal sense)
            //if completed, X files suceeded - hide p2
            //if failed, X files suceeded (if applicable), X files failed. - hide p2
            //if paused, X files suceeded, X failed, X paused. - hide p2
            if (folderState.HasFlag(TransferStates.InProgress) || folderState.HasFlag(TransferStates.Queued) || folderState.HasFlag(TransferStates.Initializing) || folderState.HasFlag(TransferStates.Requested) || folderState.HasFlag(TransferStates.Aborted))
            {
                int numRemaining = 0;
                string currentFilename = string.Empty;
                int total = 0;
                lock (fi.TransferItems)
                {
                    foreach (var ti in fi.TransferItems)
                    {
                        total++;
                        if (!(ti.State.HasFlag(TransferStates.Completed)))
                        {
                            numRemaining++;
                        }
                        if (ti.State.HasFlag(TransferStates.InProgress))
                        {
                            currentFilename = ti.Filename;
                        }
                    }
                    if (currentFilename == string.Empty) //init or requested case
                    {
                        currentFilename = fi.TransferItems.First().Filename;
                    }
                }

                filesLongStatus.Text = string.Format(SeekerApplication.GetString(Resource.String.X_of_Y_Remaining), numRemaining, total);
                currentFile.Visibility = ViewStates.Visible;
                currentFile.Text = string.Format("Current: {0}", currentFilename);
            }
            else if (folderState.HasFlag(TransferStates.Succeeded))
            {
                int numSucceeded = fi.TransferItems.Count;

                filesLongStatus.Text = string.Format("{0} {1} {2}", SeekerApplication.GetString(Resource.String.all), numSucceeded, SeekerApplication.GetString(Resource.String.Succeeded).ToLower());
                currentFile.Visibility = ViewStates.Gone;
            }
            else if (folderState.HasFlag(TransferStates.Errored) || folderState.HasFlag(TransferStates.Rejected) || folderState.HasFlag(TransferStates.TimedOut))
            {
                int numFailed = 0;
                int numSucceeded = 0;
                int numPaused = 0;
                lock (fi.TransferItems)
                {

                    foreach (var ti in fi.TransferItems)
                    {
                        if (ti.State.HasFlag(TransferStates.Succeeded))
                        {
                            numSucceeded++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Errored) || ti.State.HasFlag(TransferStates.Rejected) || ti.State.HasFlag(TransferStates.TimedOut))
                        {
                            numFailed++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Cancelled))
                        {
                            numPaused++;
                        }
                    }
                }

                SetFilesLongStatusIfNotInProgress(filesLongStatus, fi, numFailed, numSucceeded, numPaused);
                currentFile.Visibility = ViewStates.Gone;
                //set views + visi
            }
            else if (folderState.HasFlag(TransferStates.Cancelled))
            {
                int numFailed = 0;
                int numSucceeded = 0;
                int numPaused = 0;
                lock (fi.TransferItems)
                {

                    foreach (var ti in fi.TransferItems)
                    {
                        if (ti.State.HasFlag(TransferStates.Succeeded))
                        {
                            numSucceeded++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Cancelled))
                        {
                            numPaused++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Errored))
                        {
                            numFailed++;
                        }
                    }
                }

                if (numPaused == 0)
                {
                    //error
                }

                SetFilesLongStatusIfNotInProgress(filesLongStatus, fi, numFailed, numSucceeded, numPaused);
                currentFile.Visibility = ViewStates.Gone;
                //set views + visi
            }
            else
            {
                //i.e. None, can be due to uploading 0 byte files. or for transfers that never got initialized.
                //     dont leave this as is bc it will display "3 files remaining and Current: filename..." always.
                currentFile.Visibility = ViewStates.Gone;
                filesLongStatus.Text = string.Format(SeekerApplication.GetString(Resource.String.Num_FilesRemaining), fi.TransferItems.Count);
            }

        }

        private static void SetFilesLongStatusIfNotInProgress(TextView filesLongStatus, FolderItem fi, int numFailed, int numSucceeded, int numPaused)
        {
            string failedString = SeekerApplication.GetString(Resource.String.failed).ToLower();
            string succeededString = SeekerApplication.GetString(Resource.String.Succeeded).ToLower();
            string AllString = SeekerApplication.GetString(Resource.String.all);
            string cancelledString = fi.IsUpload() ? SeekerApplication.GetString(Resource.String.Aborted).ToLower() : SeekerApplication.GetString(Resource.String.paused).ToLower();
            // 0 0 0 isnt one.
            if (numSucceeded == 0 && numFailed == 0 && numPaused != 0) //all paused
            {
                filesLongStatus.Text = string.Format(AllString + " {0} {1}", numPaused, cancelledString);
            }
            else if (numSucceeded == 0 && numFailed != 0 && numPaused == 0) //all failed
            {
                filesLongStatus.Text = string.Format(AllString + " {0} {1}", numFailed, failedString);
            }
            else if (numSucceeded == 0 && numFailed != 0 && numPaused != 0) //all failed or paused
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} {3}", numPaused, cancelledString, numFailed, failedString);
            }
            else if (numSucceeded != 0 && numFailed == 0 && numPaused == 0) //all succeeded
            {
                filesLongStatus.Text = string.Format(AllString + " {0} {1}", numSucceeded, succeededString);
            }
            else if (numSucceeded != 0 && numFailed == 0 && numPaused != 0) //all succeeded or paused
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} {3}", numPaused, cancelledString, numSucceeded, succeededString);
            }
            else if (numSucceeded != 0 && numFailed != 0 && numPaused == 0) //all succeeded or failed
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} {3}", numFailed, failedString, numSucceeded, succeededString);
            }
            else //all
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} {3}, {4} {5}", numPaused, cancelledString, numSucceeded, succeededString, numFailed, failedString);
            }
        }



        public static void SetSizeText(TextView size, int progress, long sizeBytes)
        {
            if (progress == 100)
            {
                if (sizeBytes > 1024 * 1024)
                {
                    size.Text = System.String.Format("{0:F1}mb", sizeBytes / 1048576.0);
                }
                else if (sizeBytes >= 0)
                {
                    size.Text = System.String.Format("{0:F1}kb", sizeBytes / 1024.0);
                }
                else
                {
                    size.Text = "??";
                }
            }
            else
            {
                long bytesTransferred = progress * sizeBytes;
                if (sizeBytes > 1024 * 1024)
                {
                    size.Text = System.String.Format("{0:F1}/{1:F1}mb", bytesTransferred / (1048576.0 * 100.0), sizeBytes / 1048576.0);
                }
                else if (sizeBytes >= 0)
                {
                    size.Text = System.String.Format("{0:F1}/{1:F1}kb", bytesTransferred / (1024.0 * 100.0), sizeBytes / 1024.0);
                }
                else
                {
                    size.Text = "??";
                }
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
                viewStatus.Text = "None";
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

        public static void SetAdditionalStatusText(TextView viewStatusAdditionalInfo, ITransferItem item, TransferStates state, bool showSpeed)
        {
            if (state.HasFlag(TransferStates.InProgress))
            {
                //Helpers.GetTransferSpeedString(avgSpeedBytes);
                if (showSpeed)
                {
                    viewStatusAdditionalInfo.Text = CommonHelpers.GetTransferSpeedString(item.GetAvgSpeed()) + "  •  " + GetTimeRemainingString(item.GetRemainingTime());
                }
                else
                {
                    viewStatusAdditionalInfo.Text = GetTimeRemainingString(item.GetRemainingTime());
                }
            }
            else if (state.HasFlag(TransferStates.Queued) && !(item.IsUpload()))
            {
                int queueLen = item.GetQueueLength();
                if (queueLen == int.MaxValue) //i.e. unknown
                {
                    viewStatusAdditionalInfo.Text = string.Empty;
                }
                else
                {
                    viewStatusAdditionalInfo.Text = string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.position_), queueLen.ToString());
                }
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.Rejected))
            {
                if (item.IsUpload())
                {
                    viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.Cancelled);
                }
                else
                {
                    viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.denied);
                }
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.TimedOut))
            {
                viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.TimedOut);
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.UserOffline))
            {
                viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.UserIsOffline);
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.CannotConnect))
            {
                viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.CannotConnect);
            }
            else
            {
                viewStatusAdditionalInfo.Text = "";
            }
        }
    }


    public class TransferItemViewDetails : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewFilename;

        private TextView viewStatus; //In Queue, Failed, Done, In Progress
        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.
        private TextView progressSize; //if in Queue then show position, if In Progress show time remaining.

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView()
        {
            return viewStatusAdditionalInfo;
        }

        public TextView GetProgressSizeTextView()
        {
            return progressSize;
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
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed_sizeProgressBar, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed, this, true);
            }

            setupChildren();
        }
        public TransferItemViewDetails(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed_sizeProgressBar, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed, this, true);
            }

            setupChildren();
        }

        public static TransferItemViewDetails inflate(ViewGroup parent, bool _showSizes, bool _showSpeed)
        {

            TransferItemViewDetails itemView = null;
            if (_showSizes)
            {
                itemView = (TransferItemViewDetails)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_details_dummy_showProgressSize, parent, false);
            }
            else
            {
                itemView = (TransferItemViewDetails)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_details_dummy, parent, false);
            }
            itemView.showSpeed = _showSpeed;
            itemView.showSizes = _showSizes;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFilename = FindViewById<TextView>(Resource.Id.textViewFileName);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);

            viewStatus = FindViewById<TextView>(Resource.Id.textViewStatus);
            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);

            progressSize = FindViewById<TextView>(Resource.Id.textViewProgressSize);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);

        }




        public void setItem(ITransferItem item, bool isInBatchMode)
        {
            InnerTransferItem = item;
            TransferItem ti = item as TransferItem;
            viewFilename.Text = ti.Filename;
            progressBar.Progress = ti.Progress;
            if (this.showSizes)
            {
                TransferViewHelper.SetSizeText(progressSize, ti.Progress, ti.Size);
            }
            TransferViewHelper.SetViewStatusText(viewStatus, ti.State, ti.IsUpload(), false);
            TransferViewHelper.SetAdditionalStatusText(viewStatusAdditionalInfo, ti, ti.State, this.showSpeed);
            viewUsername.Text = ti.Username;
            bool isFailedOrAborted = ti.Failed;
            if (item.IsUpload() && ti.State.HasFlag(TransferStates.Cancelled))
            {
                isFailedOrAborted = true;
            }
            if (isFailedOrAborted)
            {
                progressBar.Progress = 100;
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.Red);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.Red, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            else
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618

            }

            if (isInBatchMode && TransfersFragment.BatchSelectedItems.Contains(this.ViewHolder.AbsoluteAdapterPosition))
            {
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                    //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
            else
            {
                //this.Background
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                                           //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Color.cellback);
                                           //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
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