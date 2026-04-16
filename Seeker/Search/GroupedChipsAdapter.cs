using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Common;
using Seeker.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    /// <summary>
    /// Gmail style grouped chips
    /// </summary>
    public class GroupedChipsItemRecyclerAdapter : RecyclerView.Adapter
    {
        private readonly List<ChipDataItem> allItems;
        private readonly List<ChipType> categoryOrder;

        public GroupedChipsItemRecyclerAdapter(List<ChipDataItem> items)
        {
            allItems = items ?? new List<ChipDataItem>();
            categoryOrder = BuildCategoryOrder(allItems);
        }

        private static List<ChipType> BuildCategoryOrder(List<ChipDataItem> items)
        {
            var order = new List<ChipType>();
            foreach (var t in new[] { ChipType.FileType, ChipType.Keyword, ChipType.FileCount })
            {
                if (items.Any(i => i.ChipType == t))
                {
                    order.Add(t);
                }
            }
            return order;
        }

        public override int ItemCount => categoryOrder.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.grouped_chip_item_view, parent, false);
            return new GroupedChipViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var h = (GroupedChipViewHolder)holder;
            var category = categoryOrder[position];
            var categoryItems = allItems.Where(i => i.ChipType == category).ToList();
            var checkedItems = categoryItems.Where(i => i.IsChecked).ToList();

            h.Text.Text = BuildLabel(h.ItemView.Context, category, checkedItems);
            h.ItemView.Selected = checkedItems.Count > 0;

            h.ItemView.Click -= h.ClickHandler;
            h.ClickHandler = (s, e) => OnChipClicked(category);
            h.ItemView.Click += h.ClickHandler;
        }

        private static string BuildLabel(Android.Content.Context ctx, ChipType category, List<ChipDataItem> checkedItems)
        {
            if (checkedItems.Count == 0)
            {
                return CategoryDisplayName(ctx, category);
            }
            if (checkedItems.Count == 1)
            {
                return checkedItems[0].GetFullDisplayText();
            }
            return checkedItems[0].GetFullDisplayText() + " +" + (checkedItems.Count - 1);
        }

        public static string CategoryDisplayName(Android.Content.Context ctx, ChipType category)
        {
            switch (category)
            {
                case ChipType.FileType: return ctx.GetString(Resource.String.category_file_type);
                case ChipType.Keyword: return ctx.GetString(Resource.String.category_keyword);
                case ChipType.FileCount: return ctx.GetString(Resource.String.category_num_files);
                default: return category.ToString();
            }
        }

        private void OnChipClicked(ChipType category)
        {
            var fragment = SearchFragment.Instance;
            if (fragment == null)
            {
                return;
            }
            var sheet = CategoryFilterBottomSheet.NewInstance(category);
            sheet.Show(fragment.ChildFragmentManager, CategoryFilterBottomSheet.TAG);
        }

        public void NotifyCategoryChanged(ChipType category)
        {
            int idx = categoryOrder.IndexOf(category);
            if (idx >= 0)
            {
                NotifyItemChanged(idx);
            }
        }
    }

    public class GroupedChipViewHolder : RecyclerView.ViewHolder
    {
        public TextView Text;
        public ImageView Arrow;
        public System.EventHandler ClickHandler;

        public GroupedChipViewHolder(View view) : base(view)
        {
            Text = view.FindViewById<TextView>(Resource.Id.groupedChipText);
            Arrow = view.FindViewById<ImageView>(Resource.Id.groupedChipArrow);
        }
    }
}
