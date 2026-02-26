using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Seeker.Helpers;

namespace Seeker
{
    public class MaterialProgressBarPassThrough : LinearLayout
    {
        private bool disposed = false;
        private bool init = false;
        public MaterialProgressBarPassThrough(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }
        public MaterialProgressBarPassThrough(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Logger.Debug("MaterialProgressBarPassThrough disposed" + disposed);
            var c = new ContextThemeWrapper(context, Resource.Style.MaterialThemeForChip);
            LayoutInflater.From(c).Inflate(Resource.Layout.material_progress_bar_pass_through, this, true);
        }

        public static MaterialProgressBarPassThrough inflate(ViewGroup parent)
        {
            var c = new ContextThemeWrapper(parent.Context, Resource.Style.MaterialThemeForChip);
            MaterialProgressBarPassThrough itemView = (MaterialProgressBarPassThrough)LayoutInflater.From(c).Inflate(Resource.Layout.material_progress_bar_pass_through_dummy, parent, false);

            return itemView;
        }

        public MaterialProgressBarPassThrough(System.IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
        public MaterialProgressBarPassThrough(Context context) : this(context, null)
        {
        }

        protected override void Dispose(bool disposing)
        {
            disposed = true;
            base.Dispose(disposing);
        }
    }
}
