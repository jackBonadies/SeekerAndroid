using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace Seeker.Helpers.AnchoredMenu
{
    /// <summary>
    /// Gmail-style anchored popup menu. Renders an <see cref="AnchoredMenuConfig"/> as a
    /// <see cref="PopupWindow"/> with a <see cref="RecyclerView"/> child. Supports per-row
    /// icons, inline checkables, and nested submenus with a back row.
    /// </summary>
    public class AnchoredMenuPopup
    {
        private const int PopupWidthDp = 260;
        private const int PopupMarginRight = 6;
        private const int PopupMarginTop = 6;
        private const int CheckableDismissDelayMs = 300;

        public static AnchoredMenuPopup Show(View anchor, AnchoredMenuConfig config)
        {
            var popup = new AnchoredMenuPopup(anchor.Context, config);
            popup.ShowAt(anchor);
            return popup;
        }

        private readonly Context context;
        private readonly AnchoredMenuConfig rootConfig;
        private readonly Stack<Frame> backStack = new Stack<Frame>();

        private PopupWindow window;
        private RecyclerView recycler;
        private Adapter adapter;
        private List<Item> items;
        private bool dismissing;

        private AnchoredMenuPopup(Context context, AnchoredMenuConfig config)
        {
            this.context = context;
            this.rootConfig = config;
        }

        private void ShowAt(View anchor)
        {
            var inflater = LayoutInflater.From(context);
            var container = inflater.Inflate(Resource.Layout.anchored_menu_container, null);
            recycler = container.FindViewById<RecyclerView>(Resource.Id.anchoredMenuRecycler);
            recycler.SetLayoutManager(new LinearLayoutManager(context));

            items = BuildItems(rootConfig, null);
            adapter = new Adapter(items, OnRowClicked);
            recycler.SetAdapter(adapter);

            float density = context.Resources.DisplayMetrics.Density;
            int widthPx = (int)(PopupWidthDp * density + 0.5f);

            window = new PopupWindow(container, widthPx, ViewGroup.LayoutParams.WrapContent, true);
            window.Elevation = 8 * density;
            window.SetBackgroundDrawable(
                AndroidX.Core.Content.ContextCompat.GetDrawable(context, Resource.Drawable.anchored_menu_background));

            int statusBarHeight = 0;
            var insets = anchor.RootWindowInsets;
            if (insets != null)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    statusBarHeight = insets.GetInsets(WindowInsetsCompat.Type.StatusBars()).Top;
                }
                else
                {
                    statusBarHeight = insets.SystemWindowInsetTop;
                }
            }

            int marginRightPx = (int)(PopupMarginRight * density + 0.5f);
            int marginTopPx = (int)(PopupMarginTop * density + 0.5f);

            window.ShowAtLocation(
                anchor,
                GravityFlags.Top | GravityFlags.End,
                marginRightPx,
                marginTopPx + statusBarHeight);
        }

        public void Dismiss()
        {
            window?.Dismiss();
        }

        private void OnRowClicked(Item item)
        {
            if (dismissing)
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
                            row.OnClick?.Invoke();
                            Dismiss();
                            return;
                        case AnchoredMenuRowKind.Checkable:
                            bool now = row.GetChecked != null && row.GetChecked();
                            row.OnChecked?.Invoke(!now);
                            adapter.NotifyDataSetChanged();
                            dismissing = true;
                            recycler.PostDelayed(Dismiss, CheckableDismissDelayMs);
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
            backStack.Push(new Frame { Items = items });
            items = BuildItems(row.SubMenu, row.SubMenuTitle ?? row.Label);
            adapter.SetItems(items);
        }

        private void PopBack()
        {
            if (backStack.Count == 0)
            {
                Dismiss();
                return;
            }
            var frame = backStack.Pop();
            items = frame.Items;
            adapter.SetItems(items);
        }

        private static List<Item> BuildItems(AnchoredMenuConfig config, string backHeaderTitle)
        {
            var list = new List<Item>();
            if (backHeaderTitle != null)
            {
                list.Add(new Item { Type = ItemType.BackHeader, HeaderText = backHeaderTitle });
            }
            if (config?.Rows != null)
            {
                foreach (var row in config.Rows)
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

        private sealed class Frame
        {
            public List<Item> Items;
        }

        private sealed class Adapter : RecyclerView.Adapter
        {
            private const int ViewTypeBack = 0;
            private const int ViewTypePlain = 1;
            private const int ViewTypeCheckable = 2;
            private const int ViewTypeSubmenu = 3;

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
                    default:
                        return new PlainVh(inflater.Inflate(Resource.Layout.anchored_menu_row_plain, parent, false), onClick);
                }
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var item = items[position];
                if (holder is BackVh bvh)
                {
                    bvh.Bind(item);
                }
                else if (holder is PlainVh pvh)
                {
                    pvh.Bind(item);
                }
                else if (holder is CheckableVh cvh)
                {
                    cvh.Bind(item);
                }
                else if (holder is SubmenuVh svh)
                {
                    svh.Bind(item);
                }
            }
        }

        private abstract class RowVh : RecyclerView.ViewHolder
        {
            protected Item Current;
            protected RowVh(View itemView, Action<Item> onClick) : base(itemView)
            {
                // `?android:attr/selectableItemBackground` was resolving to an
                // invisible drawable in this dialog's context, so we build the
                // ripple programmatically. Color = normalTextColor at 20% alpha,
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
            public void Bind(Item item)
            {
                Current = item;
                var row = item.Row;
                if (row.IconResId != 0)
                {
                    icon.Visibility = ViewStates.Visible;
                    icon.SetImageResource(row.IconResId);
                }
                else
                {
                    icon.Visibility = ViewStates.Invisible;
                }
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
            }
            public void Bind(Item item)
            {
                Current = item;
                var row = item.Row;
                if (row.IconResId != 0)
                {
                    icon.Visibility = ViewStates.Visible;
                    icon.SetImageResource(row.IconResId);
                }
                else
                {
                    icon.Visibility = ViewStates.Invisible;
                }
                label.Text = row.Label;
                check.Checked = row.GetChecked != null && row.GetChecked();
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
            public void Bind(Item item)
            {
                Current = item;
                var row = item.Row;
                if (row.IconResId != 0)
                {
                    icon.Visibility = ViewStates.Visible;
                    icon.SetImageResource(row.IconResId);
                }
                else
                {
                    icon.Visibility = ViewStates.Invisible;
                }
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
            public void Bind(Item item)
            {
                Current = item;
                label.Text = item.HeaderText;
            }
        }
    }
}
