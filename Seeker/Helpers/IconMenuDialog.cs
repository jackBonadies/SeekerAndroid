using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using Seeker.Helpers.ActionSheet;

namespace Seeker.Helpers
{
    // MaterialAlertDialog list with an icon per row (browse / transfers item menus)
    public static class IconMenuDialog
    {
        // UI thread only. A second tap can reach the anchor before the first dialog takes input.
        private static AndroidX.AppCompat.App.AlertDialog current;

        // a dialog leaked by a destroyed activity can still report IsShowing, so also require its window
        private static bool IsAnyShowing => current != null && current.IsShowing && current.Window?.DecorView?.IsAttachedToWindow == true;

        public static void Show(Context context, IList<ActionSheetRow> rows, string title = null)
        {
            if (IsAnyShowing)
            {
                return;
            }
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(context);
            if (!string.IsNullOrEmpty(title))
            {
                builder.SetTitle(title);
            }
            var adapter = new IconMenuAdapter(context, rows);
            builder.SetAdapter(adapter, (sender, args) =>
            {
                if (args.Which >= 0 && args.Which < rows.Count)
                {
                    // this dialog is still showing until the click returns, and a row may open another (User options)
                    current = null;
                    rows[args.Which].OnClick?.Invoke();
                }
            });
            var dialog = builder.Show();
            current = dialog;
            dialog.DismissEvent += (s, e) =>
            {
                if (current == dialog)
                {
                    current = null;
                }
            };
        }

        private sealed class IconMenuAdapter : ArrayAdapter<string>
        {
            private readonly IList<ActionSheetRow> rows;
            private readonly int normalColor;
            private readonly int destructiveColor;

            public IconMenuAdapter(Context context, IList<ActionSheetRow> rows)
                : base(context, 0, GetLabels(rows))
            {
                this.rows = rows;
                normalColor = UiHelpers.GetColorFromAttribute(context, Resource.Attribute.colorOnSurface, context.Theme);
                destructiveColor = UiHelpers.GetColorFromAttribute(context, Resource.Attribute.destructiveColor, context.Theme);
            }

            private static string[] GetLabels(IList<ActionSheetRow> rows)
            {
                var labels = new string[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                {
                    labels[i] = rows[i].Label;
                }
                return labels;
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                if (convertView == null)
                {
                    convertView = LayoutInflater.From(Context).Inflate(Resource.Layout.icon_menu_item, parent, false);
                }

                var icon = convertView.FindViewById<ImageView>(Resource.Id.menuIcon);
                var text = convertView.FindViewById<TextView>(Resource.Id.menuText);

                ActionSheetRow row = rows[position];
                var color = new Android.Graphics.Color(row.Destructive ? destructiveColor : normalColor);
                text.Text = row.Label;
                text.SetTextColor(color);

                var drawable = AndroidX.Core.Content.ContextCompat.GetDrawable(Context, row.IconResId).Mutate();
                AndroidX.Core.Graphics.Drawable.DrawableCompat.SetTint(drawable, color);
                icon.SetImageDrawable(drawable);

                return convertView;
            }
        }
    }
}
