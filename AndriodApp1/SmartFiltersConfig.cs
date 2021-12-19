using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndriodApp1
{
    public interface ItemTouchHelperAdapter
    {

        /**
         * Called when an item has been dragged far enough to trigger a move. This is called every time
         * an item is shifted, and <strong>not</strong> at the end of a "drop" event.<br/>
         * <br/>
         * Implementations should call {@link RecyclerView.Adapter#notifyItemMoved(int, int)} after
         * adjusting the underlying data to reflect this move.
         *
         * @param fromPosition The start position of the moved item.
         * @param toPosition   Then resolved position of the moved item.
         * @return True if the item was moved to the new adapter position.
         *
         * @see RecyclerView#getAdapterPositionFor(RecyclerView.ViewHolder)
         * @see RecyclerView.ViewHolder#getAdapterPosition()
         */
        bool onItemMove(int fromPosition, int toPosition);


        /**
         * Called when an item has been dismissed by a swipe.<br/>
         * <br/>
         * Implementations should call {@link RecyclerView.Adapter#notifyItemRemoved(int)} after
         * adjusting the underlying data to reflect this removal.
         *
         * @param position The position of the item dismissed.
         *
         * @see RecyclerView#getAdapterPositionFor(RecyclerView.ViewHolder)
         * @see RecyclerView.ViewHolder#getAdapterPosition()
         */
        void onItemDismiss(int position);
    }

    public interface ItemTouchHelperViewHolder
    {

        /**
         * Called when the {@link ItemTouchHelper} first registers an item as being moved or swiped.
         * Implementations should update the item view to indicate it's active state.
         */
        void onItemSelected();


        /**
         * Called when the {@link ItemTouchHelper} has completed the move or swipe, and the active item
         * state should be cleared.
         */
        void onItemClear();
    }

    public class DragDropItemTouchHelper : ItemTouchHelper.SimpleCallback
    {
        public const float ALPHA_FULL = 1.0f;

        private readonly ItemTouchHelperAdapter mAdapter;

        public DragDropItemTouchHelper(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public DragDropItemTouchHelper(RecyclerView.Adapter adapter) : base(3, 0)
        {
            mAdapter = adapter as AndriodApp1.ItemTouchHelperAdapter;
        }

        public override bool IsLongPressDragEnabled => true;
        public override bool IsItemViewSwipeEnabled => true;
        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int flags = ItemTouchHelper.Up | ItemTouchHelper.Down;
            return MakeMovementFlags(flags, 0);
        }
        public override bool OnMove(RecyclerView p0, RecyclerView.ViewHolder p1, RecyclerView.ViewHolder p2)
        {
            // Notify the adapter of the move
            mAdapter.onItemMove(p1.AdapterPosition, p2.AdapterPosition);
            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder p0, int p1)
        {
        }

        public override void OnSelectedChanged(RecyclerView.ViewHolder viewHolder, int actionState)
        {
            // We only want the active item to change
            if (actionState != ItemTouchHelper.ActionStateIdle)
            {
                if (viewHolder is ItemTouchHelperViewHolder)
                {
                    // Let the view holder know that this item is being moved or dragged
                    ItemTouchHelperViewHolder itemViewHolder = (ItemTouchHelperViewHolder)viewHolder;
                    itemViewHolder.onItemSelected();
                }
            }

            base.OnSelectedChanged(viewHolder, actionState);
        }

        public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            base.ClearView(recyclerView, viewHolder);

            viewHolder.ItemView.Alpha = (ALPHA_FULL);

            if (viewHolder is ItemTouchHelperViewHolder)
            {
                // Tell the view holder it's time to restore the idle state
                ItemTouchHelperViewHolder itemViewHolder = (ItemTouchHelperViewHolder)viewHolder;
                itemViewHolder.onItemClear();
            }
        }
    }

    public class ConfigureChipItems
    {
        public bool Enabled;
        public string Name;
    }

    public class RecyclerListAdapter : RecyclerView.Adapter
        , ItemTouchHelperAdapter
    {
        public ItemTouchHelper ItemTouchHelper;

        public List<ConfigureChipItems> GetAdapterItems()
        {
            return mItems;
        }

        private List<ConfigureChipItems> mItems = null;

        private OnStartDragListener mDragStartListener;

        public RecyclerListAdapter(Context context, OnStartDragListener dragStartListener, List<ConfigureChipItems> configureChipItems)
        {
            mDragStartListener = dragStartListener;
            mItems = configureChipItems;
            //mItems.addAll(Arrays.asList(context.getResources().getStringArray(R.array.dummy_items)));
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.smart_filter_config_item, parent, false);
            ItemViewHolder itemViewHolder = new ItemViewHolder(view);
            return itemViewHolder;
        }
        public RecyclerView.ViewHolder Holder;
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as ItemViewHolder).textView.Text = mItems[position].Name;
            (holder as ItemViewHolder).checkBoxEnable.Checked = mItems[position].Enabled;
            (holder as ItemViewHolder).ItemView.Touch += (object sender, View.TouchEventArgs e) =>
            {
                if (e.Event.ActionMasked == MotionEventActions.Down)
                {
                    this.ItemTouchHelper.StartDrag(holder);
                }
            };

            (holder as ItemViewHolder).checkBoxEnable.CheckedChange += (object sender, CompoundButton.CheckedChangeEventArgs e) =>
            {
                mItems[holder.AdapterPosition].Enabled = e.IsChecked;
            };


            // Start a drag whenever the handle view it touched
            //    holder.handleView.setOnTouchListener(new View.OnTouchListener() {
            //        @Override
            //        public boolean onTouch(View v, MotionEvent event)
            //    {
            //        if (MotionEventCompat.getActionMasked(event) == MotionEvent.ACTION_DOWN)
            //        {
            //            mDragStartListener.onStartDrag(holder);
            //        }
            //        return false;
            //    }
            //});
        }

        public void onItemDismiss(int position)
        {
            //mItems.remove(position);
            //notifyItemRemoved(position);
        }

        public bool onItemMove(int fromPosition, int toPosition)
        {
            if (Math.Min(fromPosition, toPosition) >= mItems.Count - 1)
            {
                return true;
            }
            mItems.Reverse(Math.Min(fromPosition, toPosition), 2);
            this.NotifyItemMoved(fromPosition, toPosition);
            return true;
        }
        public override int ItemCount => mItems.Count;

    }
    /**
     * Simple example of a view holder that implements {@link ItemTouchHelperViewHolder} and has a
     * "handle" view that initiates a drag event when touched.
     */
    public class ItemViewHolder : RecyclerView.ViewHolder,
            AndriodApp1.ItemTouchHelperViewHolder
    {

        public TextView textView;
        public CheckBox checkBoxEnable;

        public ItemViewHolder(View itemView) : base(itemView)
        {
            textView = (TextView)itemView.FindViewById(Resource.Id.displayNameMain);
            checkBoxEnable = (CheckBox)itemView.FindViewById<CheckBox>(Resource.Id.checkBoxEnable);
            //handleView = (ImageView)itemView.FindViewById(R.id.handle);
        }

        public void onItemSelected()
        {
            ItemView.Alpha = 0.5f;
            //itemView.setBackgroundColor(Color.LTGRAY);
        }

        public void onItemClear()
        {
            ItemView.Alpha = 1.0f;
        }
    }

    public interface OnStartDragListener
    {

        /**
         * Called when a view is requesting a start of a drag.
         *
         * @param viewHolder The holder of the view to drag.
         */
        void onStartDrag(RecyclerView.ViewHolder viewHolder);

    }
}