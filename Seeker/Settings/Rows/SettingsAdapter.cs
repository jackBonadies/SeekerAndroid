using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.MaterialSwitch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker.Settings.Rows
{
    public sealed class SettingsAdapter : RecyclerView.Adapter
    {
        private List<SettingRow> _all;
        private List<SettingRow> _visible = new();
        private string _query = string.Empty;
        private readonly ISettingsHost _host;
        private readonly Context _ctx;

        public SettingsAdapter(ISettingsHost host, List<SettingRow> rows)
        {
            _host = host;
            _ctx = host.Activity;
            _all = rows;
            RebuildVisible();
        }

        public override int ItemCount => _visible.Count;
        public override int GetItemViewType(int position) => (int)_visible[position].Type;

        public string CurrentQuery => _query;

        internal SettingRow GetVisibleRow(int position)
        {
            if (position < 0 || position >= _visible.Count)
            {
                return null;
            }
            return _visible[position];
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var inflater = LayoutInflater.From(parent.Context);
            var type = (SettingRowType)viewType;
            return type switch
            {
                SettingRowType.Header => new HeaderViewHolder(inflater.Inflate(Resource.Layout.row_setting_header, parent, false)),
                SettingRowType.Toggle => new ToggleViewHolder(inflater.Inflate(Resource.Layout.row_setting_toggle, parent, false), this),
                SettingRowType.Value => new ValueViewHolder(inflater.Inflate(Resource.Layout.row_setting_value, parent, false), this),
                SettingRowType.Action => new ActionViewHolder(inflater.Inflate(Resource.Layout.row_setting_action, parent, false), this),
                SettingRowType.Button => new ButtonStandoutViewHolder(inflater.Inflate(Resource.Layout.row_setting_button_card, parent, false), this),
                SettingRowType.ButtonPair => new ButtonPairViewHolder(inflater.Inflate(Resource.Layout.row_setting_button_pair, parent, false), this),
                SettingRowType.Navigation => new NavigationViewHolder(inflater.Inflate(Resource.Layout.row_setting_nav, parent, false), this),
                SettingRowType.SharedFolder => new SharedFolderViewHolder(inflater.Inflate(Resource.Layout.row_setting_shared_folder, parent, false), this),
                SettingRowType.Empty => new EmptyViewHolder(inflater.Inflate(Resource.Layout.row_setting_empty, parent, false)),
                _ => throw new InvalidOperationException("Unknown view type"),
            };
        }

        public override void OnViewRecycled(Java.Lang.Object holder)
        {
            if (holder is ToggleViewHolder t) t.Detach();
            else if (holder is ValueViewHolder v) v.Detach();
            base.OnViewRecycled(holder);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var row = _visible[position];
            switch (holder)
            {
                case HeaderViewHolder h: h.Bind(_ctx, (HeaderRow)row); break;
                case ToggleViewHolder t: t.Bind(_ctx, (ToggleRow)row, FindParentToggle(row)); break;
                case ValueViewHolder v: v.Bind(_ctx, (ValueRow)row, FindParentToggle(row)); break;
                case ActionViewHolder a: a.Bind(_ctx, (ActionRow)row, FindParentToggle(row)); break;
                case ButtonStandoutViewHolder b: b.Bind(_ctx, (ButtonRow)row, FindParentToggle(row)); break;
                case ButtonPairViewHolder bp: bp.Bind(_ctx, (ButtonPairRow)row, FindParentToggle(row)); break;
                case NavigationViewHolder n: n.Bind(_ctx, (NavigationRow)row, FindParentToggle(row)); break;
                case SharedFolderViewHolder f: f.Bind(_ctx, (SharedFolderRow)row, FindParentToggle(row)); break;
                case EmptyViewHolder e: e.Bind(_ctx); break;
            }
        }

        internal ISettingsHost Host => _host;

        private ToggleRow FindParentToggle(SettingRow row)
        {
            if (string.IsNullOrEmpty(row.ParentId)) return null;
            // I dont think this is strictly necessary
            if (!_visible.Any(r => r.Id == row.ParentId)) return null;
            return _all.FirstOrDefault(r => r.Id == row.ParentId) as ToggleRow;
        }

        public void Filter(string query)
        {
            _query = query ?? string.Empty;
            RebuildVisible();
        }

        /// <summary>Swap in a freshly-built row list (e.g. after shared folders are added/removed).
        /// DiffUtil keys on SettingRow.Id for animation.</summary>
        public void RebuildRows(List<SettingRow> newRows)
        {
            _all = newRows;
            RebuildVisible();
        }

        // suppresses animations
        private static readonly Java.Lang.Object RebindInPlacePayload = new Java.Lang.String("sharing-rebind");

        public void NotifySharingRowsChanged(bool suppressAnimation = true)
        {
            for (int i = 0; i < _visible.Count; i++)
            {
                var id = _visible[i].Id;
                if (id == "sharing.enable" || (id != null && id.StartsWith("sharing.folder.", StringComparison.Ordinal)))
                {
                    NotifyItemChanged(i, suppressAnimation ? RebindInPlacePayload : null);
                }
            }
        }

        public void NotifyRowChanged(string rowId)
        {
            for (int i = 0; i < _visible.Count; i++)
            {
                if (_visible[i].Id == rowId)
                {
                    NotifyItemChanged(i);
                    return;
                }
            }
        }

        public int IndexOfRow(string rowId)
        {
            for (int i = 0; i < _visible.Count; i++)
            {
                if (_visible[i].Id == rowId)
                {
                    return i;
                }
            }
            return -1;
        }

        public void NotifyParentToggled(string parentId)
        {
            var surviving = _visible
                .Where(r => r.ParentId == parentId)
                .Select(r => r.Id)
                .ToHashSet();
            RebuildVisible();
            for (int i = 0; i < _visible.Count; i++)
            {
                var row = _visible[i];
                if (row.ParentId == parentId && surviving.Contains(row.Id))
                {
                    NotifyItemChanged(i);
                }
            }
        }

        private void RebuildVisible()
        {
            var newVisible = ComputeVisible();
            var diff = DiffUtil.CalculateDiff(new RowDiffCallback(_visible, newVisible));
            _visible = newVisible;
            diff.DispatchUpdatesTo(this);
        }

        private List<SettingRow> ComputeVisible()
        {
            // Rows which match query
            var matchesQuery = new bool[_all.Count];
            for (int i = 0; i < _all.Count; i++)
            {
                var row = _all[i];
                if (row.VisiblePredicate != null && !row.VisiblePredicate()) continue;
                if (row.Type == SettingRowType.Header) { matchesQuery[i] = true; continue; }
                matchesQuery[i] = MatchesQuery(row);
            }

            // Pull parents of matched children and headers
            var include = new bool[_all.Count];
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Type == SettingRowType.Header) continue;
                if (!matchesQuery[i]) continue;
                if (_all[i].VisiblePredicate != null && !_all[i].VisiblePredicate()) continue;
                include[i] = true;

                if (_all[i] is ToggleRow t && t.DependentRowIds != null)
                {
                    foreach (var depId in t.DependentRowIds)
                    {
                        var depIdx = _all.FindIndex(r => r.Id == depId);
                        if (depIdx < 0) continue;
                        var dep = _all[depIdx];
                        if (dep.VisiblePredicate != null && !dep.VisiblePredicate()) continue;
                        include[depIdx] = true;
                    }
                }
            }

            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Type != SettingRowType.Header) continue;
                for (int j = i + 1; j < _all.Count && _all[j].Type != SettingRowType.Header; j++)
                {
                    if (include[j]) { include[i] = true; break; }
                }
            }

            var result = new List<SettingRow>(_all.Count);
            for (int i = 0; i < _all.Count; i++)
            {
                if (include[i]) result.Add(_all[i]);
            }

            if (result.Count == 0 && !string.IsNullOrEmpty(_query))
            {
                result.Add(new EmptyStateRow { Id = "__empty__" });
            }
            return result;
        }

        private bool MatchesQuery(SettingRow row)
        {
            if (string.IsNullOrWhiteSpace(_query)) return true;
            var haystack = ((row.ResolveTitle(_ctx) ?? "") + " "
                + (row.ResolveSubtitle(_ctx) ?? "") + " "
                + row.ResolveKeywords(_ctx)).ToLowerInvariant();
            var tokens = _query.ToLowerInvariant().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var tok in tokens)
            {
                if (!haystack.Contains(tok)) return false;
            }
            return true;
        }

        private sealed class RowDiffCallback : DiffUtil.Callback
        {
            private readonly List<SettingRow> _old;
            private readonly List<SettingRow> _new;
            public RowDiffCallback(List<SettingRow> oldList, List<SettingRow> newList) { _old = oldList; _new = newList; }
            public override int OldListSize => _old.Count;
            public override int NewListSize => _new.Count;
            public override bool AreItemsTheSame(int oldPos, int newPos) => _old[oldPos].Id == _new[newPos].Id;
            public override bool AreContentsTheSame(int oldPos, int newPos) => true; // can revisit - set to true so that way when parent rows are toggled (i.e. Override Incomplete Folder Location) it doesnt rebind everything
        }
    }

    // ------------ ViewHolders ------------

    internal abstract class RowViewHolder : RecyclerView.ViewHolder
    {
        internal static readonly Android.Graphics.Color SuccessColor = new Android.Graphics.Color(0x4C, 0xAF, 0x50);

        private SettingsAdapter _adapter;
        protected RowViewHolder(View v, SettingsAdapter adapter) : base(v) { _adapter = adapter; }

        protected static int ResolveThemeColor(Context ctx, int attrResId)
        {
            var tv = new Android.Util.TypedValue();
            ctx.Theme.ResolveAttribute(attrResId, tv, true);
            return tv.Data;
        }

        protected void ApplyCommon(Context ctx, SettingRow row, View root, ImageView icon,
            TextView title, TextView subtitle, ImageView info, ToggleRow parent)
        {
            title.Text = row.ResolveTitle(ctx);
            var sub = row.ResolveSubtitle(ctx);
            if (!string.IsNullOrEmpty(sub)) { subtitle.Text = sub; subtitle.Visibility = ViewStates.Visible; }
            else { subtitle.Visibility = ViewStates.Gone; }

            if (row.IconRes.HasValue) { icon.SetImageResource(row.IconRes.Value); icon.Visibility = ViewStates.Visible; }
            else { icon.Visibility = ViewStates.Gone; }

            if (row.MoreInfoTextRes.HasValue)
            {
                info.Visibility = ViewStates.Visible;
                info.Click -= OnMoreInfoClick;
                info.Click += OnMoreInfoClick;
            }
            else
            {
                info.Visibility = ViewStates.Gone;
                info.Click -= OnMoreInfoClick;
            }
            
            SetIndentForChildIfApplicable(root, row, parent);

            bool enabled = IsEnabledByParent(row, parent);
            root.Enabled = enabled;
            root.Alpha = enabled ? 1f : 0.4f;
        }

        private SettingRow _currentRowForInfo;
        protected void RememberRowForInfo(SettingRow row) => _currentRowForInfo = row;

        protected bool IsEnabledByParent(SettingRow row, ToggleRow parent)
        {
            if (parent == null || parent.Getter == null || !row.DisableIfParentDisabled)
            {
                return true;
            }
            else
            {
                return parent.Getter();
            }
        }

        protected void SetIndentForChildIfApplicable(View root, SettingRow row, ToggleRow parent)
        {
            if (!row.Indent) 
            { 
                return;
            }
            var basePadStart = (int)(16 * root.Resources.DisplayMetrics.Density);
            var indentPadStart = (int)((16 + 24 + 20) * root.Resources.DisplayMetrics.Density);
            root.SetPaddingRelative(
                (parent != null && !string.IsNullOrEmpty(row.ParentId)) ? indentPadStart : basePadStart,
                root.PaddingTop, root.PaddingEnd, root.PaddingBottom);
        }

        protected void OnMoreInfoClick(object sender, EventArgs e)
        {
            var row = _currentRowForInfo;
            if (row == null || !row.MoreInfoTextRes.HasValue) return;
            if (row.MoreInfoOnClick != null)
            {
                row.MoreInfoOnClick(_adapter.Host, row);
                return;
            }
            MoreInfoBottomSheet.Show(_adapter.Host, row);
        }

        protected void RenderStatus(Func<SettingStatus> statusProvider, View statusLine, TextView statusText, View statusSpinner, View statusDot)
        {
            var status = statusProvider?.Invoke() ?? default;
            if (statusProvider == null || status.Kind == SettingStatusKind.None)
            {
                statusLine.Visibility = ViewStates.Gone;
                return;
            }

            statusLine.Visibility = ViewStates.Visible;
            statusText.Text = status.Text ?? string.Empty;

            if (status.Kind == SettingStatusKind.Running)
            {
                statusSpinner.Visibility = ViewStates.Visible;
                statusDot.Visibility = ViewStates.Gone;
            }
            else
            {
                statusSpinner.Visibility = ViewStates.Gone;
                if (status.Kind == SettingStatusKind.HideDot)
                {
                    statusDot.Visibility = ViewStates.Gone;
                }
                else
                {
                    statusDot.Visibility = ViewStates.Visible;
                    var color = status.Kind == SettingStatusKind.Success
                        ? SuccessColor
                        : new Android.Graphics.Color(ResolveThemeColor(statusDot.Context, Resource.Attribute.destructiveColor));
                    statusDot.Background?.SetColorFilter(color, Android.Graphics.PorterDuff.Mode.SrcIn);
                }
            }
        }
    }

    internal sealed class HeaderViewHolder : RecyclerView.ViewHolder
    {
        private readonly TextView _title;
        public HeaderViewHolder(View v) : base(v)
        {
            _title = v.FindViewById<TextView>(Resource.Id.headerTitle);
        }
        public void Bind(Context ctx, HeaderRow row) => _title.Text = row.ResolveTitle(ctx);
    }

    internal sealed class ToggleViewHolder : RowViewHolder
    {
        private readonly View _root;
        private readonly ImageView _icon, _info;
        private readonly TextView _title, _subtitle;
        private readonly MaterialSwitch _switch;
        private readonly View _statusLine, _statusDot;
        private readonly ProgressBar _statusSpinner;
        private readonly TextView _statusText;
        private readonly SettingsAdapter _adapter;
        private ToggleRow _row;
        private EventHandler<EventArgs> _statusListener;

        public ToggleViewHolder(View v, SettingsAdapter adapter) : base(v, adapter)
        {
            _adapter = adapter;
            _root = v.FindViewById<View>(Resource.Id.rowRoot);
            _icon = v.FindViewById<ImageView>(Resource.Id.rowIcon);
            _title = v.FindViewById<TextView>(Resource.Id.rowTitle);
            _subtitle = v.FindViewById<TextView>(Resource.Id.rowSubtitle);
            _info = v.FindViewById<ImageView>(Resource.Id.rowInfo);
            _switch = v.FindViewById<MaterialSwitch>(Resource.Id.rowSwitch);
            _statusLine = v.FindViewById<View>(Resource.Id.rowStatusLine);
            _statusSpinner = v.FindViewById<ProgressBar>(Resource.Id.rowStatusSpinner);
            _statusDot = v.FindViewById<View>(Resource.Id.rowStatusDot);
            _statusText = v.FindViewById<TextView>(Resource.Id.rowStatusText);

            _root.Click += (s, e) =>
            {
                if (_row == null || !_root.Enabled) return;
                bool newVal = !_switch.Checked;
                _switch.Checked = newVal;
                _row.Setter?.Invoke(newVal);
                if (_row.DependentRowIds != null && _row.DependentRowIds.Count > 0)
                    _adapter.NotifyParentToggled(_row.Id);
                // Reflect any immediate status change (e.g. spinner appearing) from the setter.
                RenderStatus();
            };
        }

        public void Bind(Context ctx, ToggleRow row, ToggleRow parent)
        {
            Detach();
            _row = row;
            RememberRowForInfo(row);
            ApplyCommon(ctx, row, _root, _icon, _title, _subtitle, _info, parent);
            _switch.Checked = row.Getter?.Invoke() ?? false;

            RenderStatus();
            if (row.StatusProvider != null && row.AddStatusListener != null)
            {
                _statusListener = (s, e) => _root.Post(RenderStatus);
                row.AddStatusListener(_statusListener);
            }
        }

        /// <summary>Detach the live status listener (on recycle or before rebind) so we don't leak
        /// or double-subscribe.</summary>
        public void Detach()
        {
            if (_statusListener != null && _row?.RemoveStatusListener != null)
                _row.RemoveStatusListener(_statusListener);
            _statusListener = null;
        }

        private void RenderStatus()
        {
            RenderStatus(_row?.StatusProvider, _statusLine, _statusText, _statusSpinner, _statusDot);
        }
    }

    internal sealed class ValueViewHolder : RowViewHolder
    {
        private readonly View _root;
        private readonly ImageView _icon, _info, _chevron;
        private readonly TextView _title, _subtitle;
        private readonly View _statusLine, _statusDot;
        private readonly ProgressBar _statusSpinner;
        private readonly TextView _statusText;
        private readonly SettingsAdapter _adapter;
        private ValueRow _row;
        private EventHandler<EventArgs> _statusListener;

        public ValueViewHolder(View v, SettingsAdapter adapter) : base(v, adapter)
        {
            _adapter = adapter;
            _root = v.FindViewById<View>(Resource.Id.rowRoot);
            _icon = v.FindViewById<ImageView>(Resource.Id.rowIcon);
            _title = v.FindViewById<TextView>(Resource.Id.rowTitle);
            _subtitle = v.FindViewById<TextView>(Resource.Id.rowSubtitle);
            _info = v.FindViewById<ImageView>(Resource.Id.rowInfo);
            _chevron = v.FindViewById<ImageView>(Resource.Id.rowChevron);
            _statusLine = v.FindViewById<View>(Resource.Id.rowStatusLine);
            _statusSpinner = v.FindViewById<ProgressBar>(Resource.Id.rowStatusSpinner);
            _statusDot = v.FindViewById<View>(Resource.Id.rowStatusDot);
            _statusText = v.FindViewById<TextView>(Resource.Id.rowStatusText);

            _root.Click += (s, e) =>
            {
                if (_row == null || !_root.Enabled) return;
                _row.OnClick?.Invoke(_adapter.Host, _row);
            };
        }

        public void Bind(Context ctx, ValueRow row, ToggleRow parent)
        {
            Detach();
            _row = row;
            RememberRowForInfo(row);

            // Trailing icon: optional override, else default chevron.
            _chevron.SetImageResource(row.TrailingIconRes ?? Resource.Drawable.ic_chevron_right);

            // Compose subtitle = explicit subtitle OR fallback to ValueProvider when no subtitle.
            string sub = row.ResolveSubtitle(ctx);
            if (string.IsNullOrEmpty(sub) && row.ValueProvider != null)
            {
                var derived = new ValueRow
                {
                    Id = row.Id, TitleRes = row.TitleRes, TitleProvider = row.TitleProvider,
                    IconRes = row.IconRes, MoreInfoTextRes = row.MoreInfoTextRes, ParentId = row.ParentId,
                    SubtitleProvider = c => row.ValueProvider(c),
                };
                ApplyCommon(ctx, derived, _root, _icon, _title, _subtitle, _info, parent);
            }
            else
            {
                ApplyCommon(ctx, row, _root, _icon, _title, _subtitle, _info, parent);
            }

            RenderStatus();
            if (row.StatusProvider != null && row.AddStatusListener != null)
            {
                _statusListener = (s, e) => _root.Post(RenderStatus);
                row.AddStatusListener(_statusListener);
            }
        }

        public void Detach()
        {
            if (_statusListener != null && _row?.RemoveStatusListener != null)
                _row.RemoveStatusListener(_statusListener);
            _statusListener = null;
        }

        private void RenderStatus()
        {
            RenderStatus(_row?.StatusProvider, _statusLine, _statusText, _statusSpinner, _statusDot);
        }
    }

    internal sealed class ActionViewHolder : RowViewHolder
    {
        private readonly View _root;
        private readonly ImageView _icon, _info;
        private readonly TextView _title, _subtitle;
        private readonly Google.Android.Material.Button.MaterialButton _btn;
        private readonly Android.Content.Res.ColorStateList _defaultTitleTextColors;
        private readonly SettingsAdapter _adapter;
        private ActionRow _row;

        public ActionViewHolder(View v, SettingsAdapter adapter) : base(v, adapter)
        {
            _adapter = adapter;
            _root = v.FindViewById<View>(Resource.Id.rowRoot);
            _icon = v.FindViewById<ImageView>(Resource.Id.rowIcon);
            _title = v.FindViewById<TextView>(Resource.Id.rowTitle);
            _subtitle = v.FindViewById<TextView>(Resource.Id.rowSubtitle);
            _info = v.FindViewById<ImageView>(Resource.Id.rowInfo);
            _btn = v.FindViewById<Google.Android.Material.Button.MaterialButton>(Resource.Id.rowActionButton);
            _defaultTitleTextColors = _title.TextColors;

            void TriggerClick(object s, EventArgs e)
            {
                if (_row == null || !_root.Enabled) return;
                _row.OnClick?.Invoke(_adapter.Host, _row);
            }
            _btn.Click += TriggerClick;
            _root.Click += TriggerClick;
        }

        public void Bind(Context ctx, ActionRow row, ToggleRow parent)
        {
            _row = row;
            RememberRowForInfo(row);
            ApplyCommon(ctx, row, _root, _icon, _title, _subtitle, _info, parent);
            var buttonText = row.ButtonTextProvider?.Invoke(ctx);
            if (buttonText != null)
            {
                _btn.SetText(buttonText, TextView.BufferType.Normal);
                _btn.Visibility = ViewStates.Visible;
            }
            else if (row.ButtonTextRes.HasValue)
            {
                _btn.SetText(row.ButtonTextRes.Value);
                _btn.Visibility = ViewStates.Visible;
            }
            else
            {
                _btn.Visibility = ViewStates.Gone;
            }

            if (row.Destructive)
            {
                var color = Android.Content.Res.ColorStateList.ValueOf(
                    new Android.Graphics.Color(ResolveThemeColor(ctx, Resource.Attribute.destructiveColor)));
                _title.SetTextColor(color);
                _btn.SetTextColor(color);
                _btn.StrokeColor = color;
            }
            else
            {
                var color = Android.Content.Res.ColorStateList.ValueOf(
                    new Android.Graphics.Color(ResolveThemeColor(ctx, Resource.Attribute.colorPrimary)));
                _title.SetTextColor(_defaultTitleTextColors);
                _btn.SetTextColor(color);
                _btn.StrokeColor = color;
            }
        }

    }

    internal sealed class ButtonStandoutViewHolder : RowViewHolder
    {
        private readonly Google.Android.Material.Button.MaterialButton _btn;
        private readonly ImageView _info;
        private readonly SettingsAdapter _adapter;
        private ButtonRow _row;
        private LinearLayout _root;

        public ButtonStandoutViewHolder(View v, SettingsAdapter adapter) : base(v, adapter)
        {
            _adapter = adapter;
            _btn = v.FindViewById<Google.Android.Material.Button.MaterialButton>(Resource.Id.buttonCardButton);
            _info = v.FindViewById<ImageView>(Resource.Id.buttonCardInfo);
            _btn.Click += (s, e) => _row?.OnClick?.Invoke(_adapter.Host, _row);
            _info.Click += OnMoreInfoClick;
            _root = v.FindViewById<LinearLayout>(Resource.Id.buttonCardRoot);

        }

        public void Bind(Context ctx, ButtonRow row, ToggleRow parent)
        {
            _row = row;
            RememberRowForInfo(row);

            var baseColor = new Android.Graphics.Color(ResolveThemeColor(ctx, row.Destructive
                ? Resource.Attribute.destructiveColor
                : Resource.Attribute.colorPrimary));

            var title = row.ResolveTitle(ctx);
            var sub = row.ResolveSubtitle(ctx);
            if (!string.IsNullOrEmpty(sub))
            {
                var full = title + "\n" + sub;
                var ss = new Android.Text.SpannableString(full);
                int start = title.Length + 1;
                ss.SetSpan(new Android.Text.Style.RelativeSizeSpan(0.78f), start, full.Length,
                    Android.Text.SpanTypes.ExclusiveExclusive);
                ss.SetSpan(new Android.Text.Style.ForegroundColorSpan(
                        Android.Graphics.Color.Argb(170, baseColor.R, baseColor.G, baseColor.B)),
                    start, full.Length, Android.Text.SpanTypes.ExclusiveExclusive);
                _btn.SetText(ss, Android.Widget.TextView.BufferType.Spannable);
            }
            else
            {
                _btn.Text = title;
            }

            if (row.IconRes.HasValue)
            {
                _btn.SetIconResource(row.IconRes.Value);
                _btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(baseColor);
            }
            else
            {
                _btn.Icon = null;
            }

            if (row.FullWidth)
            {
                _btn.LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent);
            } 
            else
            {
                _btn.LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
            }

            _btn.SetTextColor(baseColor);
            _btn.StrokeColor = Android.Content.Res.ColorStateList.ValueOf(baseColor);

            bool enabled = row.Enabled && IsEnabledByParent(row, parent);
            _btn.Alpha = enabled ? 1f : .5f;
            _btn.Enabled = enabled;
            _btn.Clickable = enabled;

            this.SetIndentForChildIfApplicable(_root, row, parent);

            // Optional info button to the right of the button.
            _info.Visibility = row.MoreInfoTextRes.HasValue ? ViewStates.Visible : ViewStates.Gone;
        }
    }

    internal sealed class ButtonPairViewHolder : RowViewHolder
    {
        private readonly View _root;
        private readonly Google.Android.Material.Button.MaterialButton _left, _right;
        private readonly SettingsAdapter _adapter;
        private ButtonPairRow _row;

        public ButtonPairViewHolder(View v, SettingsAdapter adapter) : base(v, adapter)
        {
            _adapter = adapter;
            _root = v.FindViewById<View>(Resource.Id.rowRoot);
            _left = v.FindViewById<Google.Android.Material.Button.MaterialButton>(Resource.Id.buttonPairLeft);
            _right = v.FindViewById<Google.Android.Material.Button.MaterialButton>(Resource.Id.buttonPairRight);

            _left.Click += (s, e) =>
            {
                if (_row == null || !_root.Enabled) return;
                _row.LeftOnClick?.Invoke(_adapter.Host, _row);
            };
            _right.Click += (s, e) =>
            {
                if (_row == null || !_root.Enabled) return;
                _row.RightOnClick?.Invoke(_adapter.Host, _row);
            };
        }

        public void Bind(Context ctx, ButtonPairRow row, ToggleRow parent)
        {
            _row = row;
            RememberRowForInfo(row);

            SetButtonText(ctx, _left, row.LeftTextProvider, row.LeftTextRes);
            SetButtonText(ctx, _right, row.RightTextProvider, row.RightTextRes);

            var color = Android.Content.Res.ColorStateList.ValueOf(
                new Android.Graphics.Color(ResolveThemeColor(ctx, Resource.Attribute.colorPrimary)));
            _left.SetTextColor(color);
            _left.StrokeColor = color;
            _right.SetTextColor(color);
            _right.StrokeColor = color;

            bool enabled = IsEnabledByParent(row, parent);
            _root.Enabled = enabled;
            _left.Alpha = enabled ? 1f : 0.4f;
            _right.Alpha = enabled ? 1f : 0.4f;

            SetIndentForChildIfApplicable(_root, row, parent);
        }

        private static void SetButtonText(Context ctx, Google.Android.Material.Button.MaterialButton btn,
            Func<Context, string> provider, int? textRes)
        {
            var text = provider?.Invoke(ctx);
            if (text != null)
            {
                btn.SetText(text, TextView.BufferType.Normal);
                btn.Visibility = ViewStates.Visible;
            }
            else if (textRes.HasValue)
            {
                btn.SetText(textRes.Value);
                btn.Visibility = ViewStates.Visible;
            }
            else
            {
                btn.Visibility = ViewStates.Gone;
            }
        }
    }

    internal sealed class NavigationViewHolder : RowViewHolder
    {
        private readonly View _root;
        private readonly ImageView _icon, _info;
        private readonly TextView _title, _subtitle;
        private readonly SettingsAdapter _adapter;
        private NavigationRow _row;

        public NavigationViewHolder(View v, SettingsAdapter adapter) : base(v, adapter)
        {
            _adapter = adapter;
            _root = v.FindViewById<View>(Resource.Id.rowRoot);
            _icon = v.FindViewById<ImageView>(Resource.Id.rowIcon);
            _title = v.FindViewById<TextView>(Resource.Id.rowTitle);
            _subtitle = v.FindViewById<TextView>(Resource.Id.rowSubtitle);
            _info = v.FindViewById<ImageView>(Resource.Id.rowInfo);
            _root.Click += (s, e) =>
            {
                if (_row == null || !_root.Enabled) return;
                if (_row.OnClick != null)
                {
                    _row.OnClick(_adapter.Host, _row);
                    return;
                }
                if (_row.ActivityType != null)
                {
                    var host = _adapter.Host;
                    host.Activity.StartActivity(new Intent(host.Activity, _row.ActivityType));
                }
            };
        }

        public void Bind(Context ctx, NavigationRow row, ToggleRow parent)
        {
            _row = row;
            RememberRowForInfo(row);
            ApplyCommon(ctx, row, _root, _icon, _title, _subtitle, _info, parent);
        }
    }

    internal sealed class SharedFolderViewHolder : RowViewHolder
    {
        private readonly View _root;
        private readonly View _content;
        private readonly ImageView _statusIcon;
        private readonly ProgressBar _statusSpinner;
        private readonly TextView _title, _subtitle;
        private readonly ImageView _edit, _remove;
        private readonly SettingsAdapter _adapter;
        private SharedFolderRow _row;

        public SharedFolderViewHolder(View v, SettingsAdapter adapter) : base(v, adapter)
        {
            _adapter = adapter;
            _root = v.FindViewById<View>(Resource.Id.rowRoot);
            _content = v.FindViewById<View>(Resource.Id.rowContent);
            _statusIcon = v.FindViewById<ImageView>(Resource.Id.folderStatusIcon);
            _statusSpinner = v.FindViewById<ProgressBar>(Resource.Id.folderStatusSpinner);
            _title = v.FindViewById<TextView>(Resource.Id.rowTitle);
            _subtitle = v.FindViewById<TextView>(Resource.Id.rowSubtitle);
            _edit = v.FindViewById<ImageView>(Resource.Id.folderEdit);
            _remove = v.FindViewById<ImageView>(Resource.Id.folderRemove);

            _edit.Click += (s, e) =>
            {
                if (_row == null || !_root.Enabled) return;
                _row.OnEdit?.Invoke(_adapter.Host, _row);
            };
            _remove.Click += (s, e) =>
            {
                if (_row == null || !_root.Enabled) return;
                _row.OnRemove?.Invoke(_adapter.Host, _row);
            };
        }

        public void Bind(Context ctx, SharedFolderRow row, ToggleRow parent)
        {
            _row = row;
            var entry = row.Entry;

            string name = entry.GetLastPathSegment();
            if (!string.IsNullOrEmpty(entry.Info.DisplayNameOverride))
            {
                name = name + " (" + entry.Info.DisplayNameOverride + ")";
            }
            _title.Text = name;

            if (entry.Info.HasError())
            {
                ShowIcon(Resource.Drawable.alert_circle_outline,
                    new Android.Graphics.Color(ResolveThemeColor(ctx, Resource.Attribute.destructiveColor)));
                SetSubtitle(UploadDirectoryManager.GetErrorString(entry.Info.ErrorState));
            }
            else if (Seeker.Services.SharedFileService.ParseStatus.IsParsing
                && !Seeker.Services.SharedFileService.ParseStatus.IsRootComplete(entry.Info.UploadDataDirectoryUri))
            {
                _statusIcon.Visibility = ViewStates.Gone;
                _statusSpinner.Visibility = ViewStates.Visible;
                SetSubtitle(SettingValueFormat.ParsingStatusText(ctx, entry));
            }
            else
            {
                ShowIcon(Resource.Drawable.check_circle, SuccessColor);
                SetSubtitle(SettingValueFormat.SharedFolderSubtitle(ctx, entry));
            }

            bool enabled = IsEnabledByParent(row, parent);
            _root.Enabled = enabled;
            _content.Alpha = enabled ? 1f : 0.4f;
            _edit.Enabled = enabled;
            _remove.Enabled = enabled;
        }

        private void ShowIcon(int drawableRes, Android.Graphics.Color tint)
        {
            _statusSpinner.Visibility = ViewStates.Gone;
            _statusIcon.Visibility = ViewStates.Visible;
            _statusIcon.SetImageResource(drawableRes);
            _statusIcon.SetColorFilter(tint, Android.Graphics.PorterDuff.Mode.SrcIn);
        }

        private void SetSubtitle(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _subtitle.Visibility = ViewStates.Gone;
            }
            else
            {
                _subtitle.Text = text;
                _subtitle.Visibility = ViewStates.Visible;
            }
        }
    }

    internal sealed class EmptyViewHolder : RecyclerView.ViewHolder
    {
        public EmptyViewHolder(View v) : base(v) { }
        public void Bind(Context ctx) { /* static text */ }
    }
}
