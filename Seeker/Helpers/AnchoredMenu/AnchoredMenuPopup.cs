using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace Seeker.Helpers.AnchoredMenu
{
    // Gmail-style app bar menu: a PopupWindow over a RecyclerView with icon, checkable,
    // submenu and divider rows. See OverflowMenu for wiring it to a toolbar item.
    public class AnchoredMenuPopup
    {
        private const int PopupWidthDp = 260;
        private const int PopupMarginRight = 6;
        private const int PopupMarginTop = 6;
        private const int PopupMarginBottom = 6;
        // long enough to see the ripple on the tapped row before the menu goes away
        private const int SelectDismissDelayMs = 120;

        private bool selectionPending;

        // UI thread only. A second tap can reach the anchor before the first popup's window takes
        // input, which would stack two menus and let a row run twice.
        private static AnchoredMenuPopup current;

        public static bool IsAnyShowing => current?.window?.IsShowing == true;

        // dismissWithAnchor: also close when the anchor itself detaches. For action mode bars, which can
        // finish on their own (selection cleared, tab swipe). Not for toolbars: InvalidateOptionsMenu
        // recreates their action views.
        public static AnchoredMenuPopup Show(View anchor, AnchoredMenuConfig config, bool dismissWithAnchor = false)
        {
            if (IsAnyShowing)
            {
                return null;
            }
            var popup = new AnchoredMenuPopup(GetThemedContext(anchor), config);
            popup.ShowAt(anchor, dismissWithAnchor);
            current = popup;
            return popup;
        }

        // Below the anchor, end edges aligned (above it when there is more room there), for menus
        // opened from a view rather than the app bar.
        public static AnchoredMenuPopup ShowAsDropDown(View anchor, AnchoredMenuConfig config)
        {
            if (IsAnyShowing)
            {
                return null;
            }
            var popup = new AnchoredMenuPopup(GetThemedContext(anchor), config);
            popup.ShowBelow(anchor);
            current = popup;
            return popup;
        }

        // An action view's context is the toolbar's app bar overlay theme (a dark ActionBar
        // overlay in the classic light themes), so resolve colours against the Activity instead.
        private static Context GetThemedContext(View anchor)
        {
            Context c = anchor.Context;
            while (c is ContextWrapper wrapper)
            {
                if (c is Activity)
                {
                    return c;
                }
                c = wrapper.BaseContext;
            }
            return anchor.Context;
        }

        private readonly Context context;
        private readonly AnchoredMenuConfig rootConfig;
        private readonly Stack<List<Item>> backStack = new Stack<List<Item>>();

        private PopupWindow window;
        private View container;
        private RecyclerView recycler;
        private Adapter adapter;
        private List<Item> items;
        private int maxHeightPx;
        private View detachWatchView;
        private View anchorWatchView;

        private AnchoredMenuPopup(Context context, AnchoredMenuConfig config)
        {
            this.context = context;
            this.rootConfig = config;
        }

        private int CreateWindow()
        {
            var inflater = LayoutInflater.From(context);
            container = inflater.Inflate(Resource.Layout.anchored_menu_container, null);
            recycler = container.FindViewById<RecyclerView>(Resource.Id.anchoredMenuRecycler);
            recycler.SetLayoutManager(new LinearLayoutManager(context));

            items = BuildItems(rootConfig, null);
            adapter = new Adapter(items, OnRowClicked);
            recycler.SetAdapter(adapter);

            float density = context.Resources.DisplayMetrics.Density;
            int widthPx = (int)(PopupWidthDp * density + 0.5f);

            window = new PopupWindow(container, widthPx, ViewGroup.LayoutParams.WrapContent, true);
            window.Elevation = 8 * density;
            window.AnimationStyle = Resource.Style.AnchoredMenuAnimation;
            window.SetBackgroundDrawable(
                AndroidX.Core.Content.ContextCompat.GetDrawable(context, Resource.Drawable.anchored_menu_background));
            window.DismissEvent += OnDismissed;
            return widthPx;
        }

        private int DpToPx(int dp)
        {
            return (int)(dp * context.Resources.DisplayMetrics.Density + 0.5f);
        }

        private void ShowAt(View anchor, bool dismissWithAnchor)
        {
            int widthPx = CreateWindow();

            int statusBarHeight = 0;
            int navBarHeight = 0;
            var insets = anchor.RootWindowInsets;
            if (insets != null)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    statusBarHeight = insets.GetInsets(WindowInsetsCompat.Type.StatusBars()).Top;
                    navBarHeight = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars()).Bottom;
                }
                else
                {
                    statusBarHeight = insets.SystemWindowInsetTop;
                    navBarHeight = insets.SystemWindowInsetBottom;
                }
            }

            int marginRightPx = DpToPx(PopupMarginRight);
            int marginTopPx = DpToPx(PopupMarginTop);
            int marginBottomPx = DpToPx(PopupMarginBottom);

            int top = marginTopPx + statusBarHeight;
            maxHeightPx = anchor.RootView.Height - top - navBarHeight - marginBottomPx;
            FitHeight(widthPx);

            detachWatchView = anchor.RootView;
            detachWatchView.ViewDetachedFromWindow += OnHostDetached;
            if (dismissWithAnchor)
            {
                anchorWatchView = anchor;
                anchorWatchView.ViewDetachedFromWindow += OnHostDetached;
            }

            window.ShowAtLocation(anchor, GravityFlags.Top | GravityFlags.End, marginRightPx, top);
        }

        private void ShowBelow(View anchor)
        {
            int widthPx = CreateWindow();

            maxHeightPx = window.GetMaxAvailableHeight(anchor) - DpToPx(PopupMarginBottom);
            FitHeight(widthPx);

            detachWatchView = anchor.RootView;
            detachWatchView.ViewDetachedFromWindow += OnHostDetached;

            window.ShowAsDropDown(anchor, -DpToPx(PopupMarginRight), 0, GravityFlags.End);
        }

        // A PopupWindow taller than the screen is clipped, not scrolled, so cap the list.
        private void FitHeight(int widthPx)
        {
            if (maxHeightPx <= 0)
            {
                return;
            }
            var lp = recycler.LayoutParameters;
            lp.Height = ViewGroup.LayoutParams.WrapContent;
            recycler.LayoutParameters = lp;
            container.Measure(
                View.MeasureSpec.MakeMeasureSpec(widthPx, MeasureSpecMode.Exactly),
                View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified));
            if (container.MeasuredHeight > maxHeightPx)
            {
                lp.Height = maxHeightPx - container.PaddingTop - container.PaddingBottom;
                recycler.LayoutParameters = lp;
            }
        }

        private void OnHostDetached(object sender, View.ViewDetachedFromWindowEventArgs e)
        {
            Dismiss();
        }

        private void OnDismissed(object sender, EventArgs e)
        {
            if (current == this)
            {
                current = null;
            }
            if (detachWatchView != null)
            {
                detachWatchView.ViewDetachedFromWindow -= OnHostDetached;
                detachWatchView = null;
            }
            if (anchorWatchView != null)
            {
                anchorWatchView.ViewDetachedFromWindow -= OnHostDetached;
                anchorWatchView = null;
            }
        }

        public void Dismiss()
        {
            if (window != null && window.IsShowing)
            {
                window.Dismiss();
            }
        }

        private void OnRowClicked(Item item)
        {
            if (selectionPending)
            {
                return;
            }
            switch (item.Type)
            {
                case ItemType.BackHeader:
                    PopBack();
                    return;
                case ItemType.Row:
                    var row = item.Row;
                    switch (row.Kind)
                    {
                        case AnchoredMenuRowKind.Plain:
                            selectionPending = true;
                            // posted before the action so the menu still closes if the action throws
                            recycler.PostDelayed(Dismiss, SelectDismissDelayMs);
                            row.OnClick?.Invoke();
                            return;
                        case AnchoredMenuRowKind.Checkable:
                            bool now = row.GetChecked != null && row.GetChecked();
                            row.OnChecked?.Invoke(!now);
                            int pos = items.IndexOf(item);
                            if (pos >= 0)
                            {
                                adapter.NotifyItemChanged(pos);
                            }
                            return;
                        case AnchoredMenuRowKind.Submenu:
                            PushSubmenu(row);
                            return;
                    }
                    return;
            }
        }

        private void PushSubmenu(AnchoredMenuRow row)
        {
            if (row.SubMenu == null)
            {
                return;
            }
            backStack.Push(items);
            SetItems(BuildItems(row.SubMenu, row.SubMenuTitle ?? row.Label));
        }

        private void PopBack()
        {
            if (backStack.Count == 0)
            {
                Dismiss();
                return;
            }
            SetItems(backStack.Pop());
        }

        private void SetItems(List<Item> newItems)
        {
            items = newItems;
            adapter.SetItems(items);
            FitHeight(window.Width);
        }

        private static List<Item> BuildItems(AnchoredMenuConfig config, string backHeaderTitle)
        {
            var list = new List<Item>();
            if (backHeaderTitle != null)
            {
                list.Add(new Item { Type = ItemType.BackHeader, HeaderText = backHeaderTitle });
            }
            if (config != null)
            {
                foreach (var row in config.GetNormalizedRows())
                {
                    list.Add(new Item { Type = ItemType.Row, Row = row });
                }
            }
            return list;
        }

        private enum ItemType { Row, BackHeader }

        private sealed class Item
        {
            public ItemType Type;
            public AnchoredMenuRow Row;
            public string HeaderText;
        }

        private sealed class Adapter : RecyclerView.Adapter
        {
            private const int ViewTypeBack = 0;
            private const int ViewTypePlain = 1;
            private const int ViewTypeCheckable = 2;
            private const int ViewTypeSubmenu = 3;
            private const int ViewTypeDivider = 4;

            private List<Item> items;
            private readonly Action<Item> onClick;

            public Adapter(List<Item> items, Action<Item> onClick)
            {
                this.items = items;
                this.onClick = onClick;
            }

            public void SetItems(List<Item> newItems)
            {
                items = newItems;
                NotifyDataSetChanged();
            }

            public override int ItemCount => items.Count;

            public override int GetItemViewType(int position)
            {
                var item = items[position];
                if (item.Type == ItemType.BackHeader)
                {
                    return ViewTypeBack;
                }
                switch (item.Row.Kind)
                {
                    case AnchoredMenuRowKind.Checkable: return ViewTypeCheckable;
                    case AnchoredMenuRowKind.Submenu: return ViewTypeSubmenu;
                    case AnchoredMenuRowKind.Divider: return ViewTypeDivider;
                    default: return ViewTypePlain;
                }
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var inflater = LayoutInflater.From(parent.Context);
                switch (viewType)
                {
                    case ViewTypeBack:
                        return new BackVh(inflater.Inflate(Resource.Layout.anchored_menu_back_header, parent, false), onClick);
                    case ViewTypeCheckable:
                        return new CheckableVh(inflater.Inflate(Resource.Layout.anchored_menu_row_checkable, parent, false), onClick);
                    case ViewTypeSubmenu:
                        return new SubmenuVh(inflater.Inflate(Resource.Layout.anchored_menu_row_submenu, parent, false), onClick);
                    case ViewTypeDivider:
                        return new DividerVh(inflater.Inflate(Resource.Layout.anchored_menu_divider, parent, false));
                    default:
                        return new PlainVh(inflater.Inflate(Resource.Layout.anchored_menu_row_plain, parent, false), onClick);
                }
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var item = items[position];
                if (holder is RowVh rvh)
                {
                    rvh.Bind(item);
                }
            }
        }

        private sealed class DividerVh : RecyclerView.ViewHolder
        {
            public DividerVh(View itemView) : base(itemView)
            {
            }
        }

        private abstract class RowVh : RecyclerView.ViewHolder
        {
            protected Item Current;
            protected RowVh(View itemView, Action<Item> onClick) : base(itemView)
            {
                // `?android:attr/selectableItemBackground` was resolving to an
                // invisible drawable in this popup, so we build the ripple
                // programmatically. Color = normalTextColor at 20% alpha,
                // so light themes get a dim-dark ripple and dark themes get a
                // dim-light ripple.
                int textColor = UiHelpers.GetColorFromAttribute(itemView.Context, Resource.Attribute.normalTextColor);
                int rippleArgb = (textColor & 0x00FFFFFF) | (0x33 << 24);
                var rippleColor = Android.Content.Res.ColorStateList.ValueOf(new Android.Graphics.Color(rippleArgb));
                var mask = new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.White);
                itemView.Background = new Android.Graphics.Drawables.RippleDrawable(rippleColor, null, mask);

                itemView.Click += (s, e) =>
                {
                    if (Current != null)
                    {
                        onClick(Current);
                    }
                };
            }

            public abstract void Bind(Item item);

            protected static void BindIcon(ImageView icon, AnchoredMenuRow row)
            {
                if (row.IconResId != 0)
                {
                    icon.Visibility = ViewStates.Visible;
                    icon.SetImageResource(row.IconResId);
                }
                else
                {
                    icon.Visibility = ViewStates.Invisible;
                }
            }
        }

        private sealed class PlainVh : RowVh
        {
            private readonly ImageView icon;
            private readonly TextView label;
            public PlainVh(View v, Action<Item> onClick) : base(v, onClick)
            {
                icon = v.FindViewById<ImageView>(Resource.Id.anchoredMenuRowIcon);
                label = v.FindViewById<TextView>(Resource.Id.anchoredMenuRowLabel);
            }
            public override void Bind(Item item)
            {
                Current = item;
                var row = item.Row;
                BindIcon(icon, row);
                label.Text = row.Label;

                int attrResId = row.Destructive
                    ? Resource.Attribute.destructiveColor
                    : Resource.Attribute.normalTextColor;
                var color = new Android.Graphics.Color(UiHelpers.GetColorFromAttribute(ItemView.Context, attrResId));
                label.SetTextColor(color);
                icon.SetColorFilter(color);
            }
        }

        private sealed class CheckableVh : RowVh
        {
            private readonly ImageView icon;
            private readonly TextView label;
            private readonly CheckBox check;
            public CheckableVh(View v, Action<Item> onClick) : base(v, onClick)
            {
                icon = v.FindViewById<ImageView>(Resource.Id.anchoredMenuRowIcon);
                label = v.FindViewById<TextView>(Resource.Id.anchoredMenuRowLabel);
                check = v.FindViewById<CheckBox>(Resource.Id.anchoredMenuRowCheck);
                ViewCompat.SetAccessibilityDelegate(v, new CheckableRowAccessibility(() => check.Checked));
            }
            public override void Bind(Item item)
            {
                Current = item;
                var row = item.Row;
                BindIcon(icon, row);
                label.Text = row.Label;
                check.Checked = row.GetChecked != null && row.GetChecked();
            }
        }

        // the CheckBox is not focusable, so without this TalkBack reads only the label
        private sealed class CheckableRowAccessibility : AccessibilityDelegateCompat
        {
            private readonly Func<bool> isChecked;
            public CheckableRowAccessibility(Func<bool> isChecked)
            {
                this.isChecked = isChecked;
            }
            public override void OnInitializeAccessibilityNodeInfo(View host, AccessibilityNodeInfoCompat info)
            {
                base.OnInitializeAccessibilityNodeInfo(host, info);
                info.ClassName = "android.widget.CheckBox";
                info.Checkable = true;
                info.SetChecked(isChecked());
            }
        }

        private sealed class SubmenuVh : RowVh
        {
            private readonly ImageView icon;
            private readonly TextView label;
            public SubmenuVh(View v, Action<Item> onClick) : base(v, onClick)
            {
                icon = v.FindViewById<ImageView>(Resource.Id.anchoredMenuRowIcon);
                label = v.FindViewById<TextView>(Resource.Id.anchoredMenuRowLabel);
            }
            public override void Bind(Item item)
            {
                Current = item;
                var row = item.Row;
                BindIcon(icon, row);
                label.Text = row.Label;
            }
        }

        private sealed class BackVh : RowVh
        {
            private readonly TextView label;
            public BackVh(View v, Action<Item> onClick) : base(v, onClick)
            {
                label = v.FindViewById<TextView>(Resource.Id.anchoredMenuBackLabel);
            }
            public override void Bind(Item item)
            {
                Current = item;
                label.Text = item.HeaderText;
            }
        }
    }
}
