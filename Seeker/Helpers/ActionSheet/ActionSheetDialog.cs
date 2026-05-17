using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomSheet;

namespace Seeker.Helpers.ActionSheet
{
    public class ActionSheetDialog : BottomSheetDialogFragment
    {
        public static ActionSheetConfig PendingConfig;

        private ActionSheetConfig config;
        private List<Item> items;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            config = PendingConfig;
            PendingConfig = null;
            items = Flatten(config);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.action_sheet_dialog, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var bottomSheet = ((BottomSheetDialog)Dialog).FindViewById<View>(Resource.Id.design_bottom_sheet);
            if (bottomSheet != null)
            {
                var behavior = BottomSheetBehavior.From(bottomSheet);
                behavior.State = BottomSheetBehavior.StateExpanded;
                behavior.SkipCollapsed = true;
            }

            var recycler = view.FindViewById<RecyclerView>(Resource.Id.actionSheetRecycler);
            recycler.SetLayoutManager(new LinearLayoutManager(Context));
            recycler.SetAdapter(new Adapter(items, OnRowClicked));
        }

        private void OnRowClicked(ActionSheetRow row)
        {
            row.OnClick?.Invoke();
            Dismiss();
        }

        private static List<Item> Flatten(ActionSheetConfig cfg)
        {
            var list = new List<Item>();
            if (cfg == null)
            {
                return list;
            }
            for (int i = 0; i < cfg.Sections.Count; i++)
            {
                var section = cfg.Sections[i];
                if (i > 0)
                {
                    list.Add(new Item { Type = ItemType.Divider });
                }
                list.Add(new Item { Type = ItemType.Header, HeaderText = section.HeaderText });
                foreach (var row in section.Rows)
                {
                    list.Add(new Item { Type = ItemType.Row, Row = row });
                }
            }
            return list;
        }

        private enum ItemType { Header, Row, Divider }

        private sealed class Item
        {
            public ItemType Type;
            public string HeaderText;
            public ActionSheetRow Row;
        }

        private sealed class Adapter : RecyclerView.Adapter
        {
            private readonly List<Item> items;
            private readonly Action<ActionSheetRow> onClick;

            public Adapter(List<Item> items, Action<ActionSheetRow> onClick)
            {
                this.items = items;
                this.onClick = onClick;
            }

            public override int ItemCount => items.Count;

            public override int GetItemViewType(int position)
            {
                return (int)items[position].Type;
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var inflater = LayoutInflater.From(parent.Context);
                switch ((ItemType)viewType)
                {
                    case ItemType.Header:
                        return new HeaderVh(inflater.Inflate(Resource.Layout.action_sheet_section_header, parent, false));
                    case ItemType.Divider:
                        return new DividerVh(inflater.Inflate(Resource.Layout.action_sheet_divider, parent, false));
                    case ItemType.Row:
                    default:
                        return new RowVh(inflater.Inflate(Resource.Layout.action_sheet_row, parent, false), onClick);
                }
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var item = items[position];
                if (holder is HeaderVh hvh)
                {
                    hvh.Bind(item.HeaderText);
                }
                else if (holder is RowVh rvh)
                {
                    rvh.Bind(item.Row);
                }
            }
        }

        private sealed class HeaderVh : RecyclerView.ViewHolder
        {
            private readonly TextView label;
            public HeaderVh(View view) : base(view)
            {
                label = view.FindViewById<TextView>(Resource.Id.actionSheetSectionHeader);
            }
            public void Bind(string text)
            {
                label.Text = text;
            }
        }

        private sealed class DividerVh : RecyclerView.ViewHolder
        {
            public DividerVh(View view) : base(view) { }
        }

        private sealed class RowVh : RecyclerView.ViewHolder
        {
            private readonly ImageView icon;
            private readonly TextView label;
            private ActionSheetRow current;

            public RowVh(View view, Action<ActionSheetRow> onClick) : base(view)
            {
                icon = view.FindViewById<ImageView>(Resource.Id.actionSheetRowIcon);
                label = view.FindViewById<TextView>(Resource.Id.actionSheetRowLabel);
                view.Click += (s, e) =>
                {
                    if (current != null)
                    {
                        onClick(current);
                    }
                };
            }

            public void Bind(ActionSheetRow row)
            {
                current = row;
                icon.SetImageResource(row.IconResId);
                label.Text = row.Label;
                int attrResId = row.Destructive
                    ? Resource.Attribute.destructiveColor
                    : Resource.Attribute.normalTextColor;
                var color = new Android.Graphics.Color(UiHelpers.GetColorFromAttribute(ItemView.Context, attrResId));
                label.SetTextColor(color);
                icon.SetColorFilter(color);
            }
        }
    }
}
