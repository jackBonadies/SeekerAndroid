using System.Collections.Generic;
using System.Linq;

using ActionMode = AndroidX.AppCompat.View.ActionMode;

namespace Seeker.Helpers
{
    // Select all / invert / title for the batch-select action modes (Browse, Messages, Transfers).
    public static class BatchSelection
    {
        public static void SelectAll<T>(ICollection<T> selected, IEnumerable<T> all)
        {
            selected.Clear();
            foreach (var item in all)
            {
                selected.Add(item);
            }
        }

        public static void Invert<T>(ICollection<T> selected, IEnumerable<T> all)
        {
            var old = new HashSet<T>(selected);
            var inverted = all.Where(item => !old.Contains(item)).ToList();
            SelectAll(selected, inverted);
        }

        public static IEnumerable<int> Positions(int count)
        {
            return Enumerable.Range(0, count);
        }

        public static void ShowCount(ActionMode mode, int count)
        {
            if (mode == null)
            {
                return;
            }
            mode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), count.ToString());
            mode.Invalidate();
        }
    }
}
