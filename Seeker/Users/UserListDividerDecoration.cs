using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using AndroidX.RecyclerView.Widget;

namespace Seeker.Users
{
    // Variant of DividerItemDecoration but only between rows 
    // and starting username column rather than full edge
    internal sealed class UserListDividerDecoration : RecyclerView.ItemDecoration
    {
        private readonly Drawable divider;

        public UserListDividerDecoration(Context context)
        {
            var a = context.ObtainStyledAttributes(new[] { Android.Resource.Attribute.ListDivider });
            divider = a.GetDrawable(0);
            a.Recycle();
        }

        public override void OnDraw(Canvas c, RecyclerView parent, RecyclerView.State state)
        {
            var adapter = parent.GetAdapter();
            if (divider == null || adapter == null)
            {
                return;
            }

            int right = parent.Width - parent.PaddingRight;
            for (int i = 0; i < parent.ChildCount; i++)
            {
                var child = parent.GetChildAt(i);
                int pos = parent.GetChildAdapterPosition(child);
                if (pos == RecyclerView.NoPosition || pos + 1 >= adapter.ItemCount)
                {
                    continue;
                }
                if (adapter.GetItemViewType(pos) == (int)UserRole.Category || adapter.GetItemViewType(pos + 1) == (int)UserRole.Category)
                {
                    continue;
                }
                if (child is not UserRowView row)
                {
                    continue;
                }

                var username = row.viewUsername;
                int left = child.Left + (username.Parent as Android.Views.View).Left + username.Left;
                int bottom = child.Bottom + (int)child.TranslationY;
                divider.SetBounds(left, bottom - divider.IntrinsicHeight, right, bottom);
                divider.Draw(c);
            }
        }
    }
}
