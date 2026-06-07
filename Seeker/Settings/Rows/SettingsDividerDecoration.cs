using Android.Content;
using Android.Graphics;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Seeker.Settings.Rows
{
    /// <summary>
    /// Draws a thin horizontal divider beneath each non-header row.
    /// </summary>
    internal sealed class SettingsDividerDecoration : RecyclerView.ItemDecoration
    {
        private readonly Paint _paint;
        private readonly int _heightPx;
        private readonly int _insetStartPx;
        private readonly int _insetEndPx;

        public SettingsDividerDecoration(Context ctx)
        {
            var density = ctx.Resources.DisplayMetrics.Density;
            _heightPx = (int)System.Math.Max(1, 0.5f * density);
            _insetStartPx = (int)(16 * density);
            _insetEndPx = (int)(16 * density);

            var tv = new Android.Util.TypedValue();
            int color = unchecked((int)0x1F000000); // fallback
            if (ctx.Theme.ResolveAttribute(Resource.Attribute.settingsDividerColor, tv, true))
            {
                color = tv.Data;
            }
            _paint = new Paint(PaintFlags.AntiAlias) { Color = new Color(color) };
        }

        public override void OnDrawOver(Canvas c, RecyclerView parent, RecyclerView.State state)
        {
            var adapter = parent.GetAdapter() as SettingsAdapter;
            if (adapter == null)
            {
                return;
            }

            int childCount = parent.ChildCount;
            int right = parent.Width - parent.PaddingRight - _insetEndPx;
            for (int i = 0; i < childCount; i++)
            {
                var child = parent.GetChildAt(i);
                int pos = parent.GetChildAdapterPosition(child);
                if (pos == RecyclerView.NoPosition)
                {
                    continue;
                }

                // Skip if this is the last row OR the next row is a header (avoid double-line above section).
                if (!ShouldDrawDividerBelow(adapter, pos))
                {
                    continue;
                }

                float y = child.Bottom + child.TranslationY;
                c.DrawRect(_insetStartPx, y, right, y + _heightPx, _paint);
            }
        }

        private static bool ShouldDrawDividerBelow(SettingsAdapter adapter, int pos)
        {
            if (pos < 0 || pos >= adapter.ItemCount - 1)
            {
                return false;
            }
            var thisType = (SettingRowType)adapter.GetItemViewType(pos);
            if (thisType == SettingRowType.Header || thisType == SettingRowType.Empty
                || thisType == SettingRowType.Button)
            {
                return false;
            }
            var nextType = (SettingRowType)adapter.GetItemViewType(pos + 1);
            if (nextType == SettingRowType.Header || nextType == SettingRowType.Button)
            {
                return false;
            }

            var current = adapter.GetVisibleRow(pos);
            var next = adapter.GetVisibleRow(pos + 1);
            if (AreInSameParentChildGroup(current, next))
            {
                return false;
            }
            return true;
        }

        private static bool AreInSameParentChildGroup(SettingRow current, SettingRow next)
        {
            if (current == null || next == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(next.ParentId) && next.ParentId == current.Id)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(current.ParentId) && current.ParentId == next.ParentId)
            {
                return true;
            }

            return false;
        }
    }
}
