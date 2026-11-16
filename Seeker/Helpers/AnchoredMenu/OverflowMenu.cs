using Android.Views;
using System;

namespace Seeker.Helpers.AnchoredMenu
{
    // Wires an app bar item whose actionLayout is anchored_menu_overflow_button to an
    // AnchoredMenuPopup. The config is built at click time, so it never needs OnPrepare.
    public static class OverflowMenu
    {
        public static void Attach(IMenu menu, int itemId, Func<AnchoredMenuConfig> build, bool dismissWithAnchor = false)
        {
            var view = menu?.FindItem(itemId)?.ActionView;
            if (view == null)
            {
                Logger.Firebase("OverflowMenu.Attach: no action view for " + itemId);
                return;
            }
            UiHelpers.SetToolTipText(view, view.Context.GetString(Resource.String.Options));
            // the action view is new on every menu inflate, so this cannot double up
            view.Click += (s, e) =>
            {
                var config = build();
                if (config == null || !config.HasRows())
                {
                    return;
                }
                AnchoredMenuPopup.Show(view, config, dismissWithAnchor);
            };
        }
    }
}
