using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;

namespace Seeker.Search
{
    public static class SearchChipHelper
    {
        public static string ExtractFormatName(string dominantFileTypeStr)
        {
            if (string.IsNullOrEmpty(dominantFileTypeStr))
            {
                return "other";
            }
            // dominantFileTypeStr is like "mp3 (320 kbs)" or "flac (16,44kHz)" or just "mp3"
            string lower = dominantFileTypeStr.Trim().ToLowerInvariant();
            // strip leading dot if present
            if (lower.StartsWith("."))
            {
                lower = lower.Substring(1);
            }
            // take just the first word (before any space or paren)
            int spaceIdx = lower.IndexOf(' ');
            if (spaceIdx > 0)
            {
                lower = lower.Substring(0, spaceIdx);
            }
            return lower;
        }

        public static (int textColorResId, int bgColorResId) GetFormatChipColorResIds(string formatName)
        {
            switch (formatName)
            {
                case "flac":
                    return (Resource.Color.searchChipFlacText, Resource.Color.searchChipFlacBg);
                case "mp3":
                    return (Resource.Color.searchChipMp3Text, Resource.Color.searchChipMp3Bg);
                case "m4a":
                    return (Resource.Color.searchChipM4aText, Resource.Color.searchChipM4aBg);
                case "wav":
                    return (Resource.Color.searchChipWavText, Resource.Color.searchChipWavBg);
                case "aac":
                    return (Resource.Color.searchChipAacText, Resource.Color.searchChipAacBg);
                case "wma":
                    return (Resource.Color.searchChipWmaText, Resource.Color.searchChipWmaBg);
                case "aiff":
                    return (Resource.Color.searchChipAiffText, Resource.Color.searchChipAiffBg);
                default:
                    return (Resource.Color.searchChipOtherText, Resource.Color.searchChipOtherBg);
            }
        }

        public static void StyleFormatChip(TextView chip, string fullFormatStr)
        {
            if (string.IsNullOrEmpty(fullFormatStr))
            {
                chip.Visibility = ViewStates.Gone;
                return;
            }
            chip.Visibility = ViewStates.Visible;
            chip.Text = fullFormatStr;

            string formatName = ExtractFormatName(fullFormatStr);
            var (textColorResId, bgColorResId) = GetFormatChipColorResIds(formatName);

            var resources = chip.Context.Resources;
            var theme = chip.Context.Theme;
            int textColor = resources.GetColor(textColorResId, theme);
            int bgColor = resources.GetColor(bgColorResId, theme);

            chip.SetTextColor(new Color(textColor));
            chip.SetTypeface(chip.Typeface, TypefaceStyle.Bold);
            chip.SetTextSize(ComplexUnitType.Sp, 10);

            var bg = chip.Background?.Mutate() as GradientDrawable;
            if (bg != null)
            {
                bg.SetColor(bgColor);
                int strokeWidth = (int)(1 * resources.DisplayMetrics.Density);
                bg.SetStroke(strokeWidth, new Color(textColor));
            }
        }

        public static void StyleFormatChipShort(TextView chip, string fullFormatStr)
        {
            if (string.IsNullOrEmpty(fullFormatStr))
            {
                chip.Visibility = ViewStates.Gone;
                return;
            }
            string formatName = ExtractFormatName(fullFormatStr);
            chip.Text = formatName.ToUpperInvariant();

            chip.Visibility = ViewStates.Visible;
            var (textColorResId, bgColorResId) = GetFormatChipColorResIds(formatName);

            var resources = chip.Context.Resources;
            var theme = chip.Context.Theme;
            int textColor = resources.GetColor(textColorResId, theme);
            int bgColor = resources.GetColor(bgColorResId, theme);

            chip.SetTextColor(new Color(textColor));
            chip.SetTypeface(chip.Typeface, TypefaceStyle.Bold);
            chip.SetTextSize(ComplexUnitType.Sp, 10);

            var bg = chip.Background?.Mutate() as GradientDrawable;
            if (bg != null)
            {
                bg.SetColor(bgColor);
                int strokeWidth = (int)(1 * resources.DisplayMetrics.Density);
                bg.SetStroke(strokeWidth, new Color(textColor));
            }
        }

        public static void StyleQueueChip(TextView chip, int queueLength)
        {
            if (queueLength <= 0)
            {
                chip.Visibility = ViewStates.Gone;
                return;
            }
            chip.Visibility = ViewStates.Visible;
            chip.Text = "Q:" + queueLength;

            var resources = chip.Context.Resources;
            var theme = chip.Context.Theme;
            int textColor = resources.GetColor(Resource.Color.searchChipQueueText, theme);
            int bgColor = resources.GetColor(Resource.Color.searchChipQueueBg, theme);

            chip.SetTextColor(new Color(textColor));
            chip.SetTypeface(chip.Typeface, TypefaceStyle.Bold);
            chip.SetTextSize(ComplexUnitType.Sp, 10);

            var bg = chip.Background?.Mutate() as GradientDrawable;
            if (bg != null)
            {
                bg.SetColor(bgColor);
                int strokeWidth = (int)(1 * resources.DisplayMetrics.Density);
                bg.SetStroke(strokeWidth, new Color(textColor));
            }
        }

        public static void StyleQueueText(TextView view, int queueLength)
        {
            if (queueLength <= 0)
            {
                view.Visibility = ViewStates.Gone;
                return;
            }
            view.Visibility = ViewStates.Visible;
            view.Text = "Q:" + queueLength;
        }

        public static void StyleNoSlotChip(TextView chip, bool hasFreeSlot)
        {
            if (hasFreeSlot)
            {
                chip.Visibility = ViewStates.Gone;
                return;
            }
            chip.Visibility = ViewStates.Visible;
            chip.Text = "No Slots";

            var resources = chip.Context.Resources;
            var theme = chip.Context.Theme;
            int textColor = resources.GetColor(Resource.Color.searchChipNoSlotText, theme);
            int bgColor = resources.GetColor(Resource.Color.searchChipNoSlotBg, theme);

            chip.SetTextColor(new Color(textColor));
            chip.SetTypeface(chip.Typeface, TypefaceStyle.Bold);
            chip.SetTextSize(ComplexUnitType.Sp, 10);

            var bg = chip.Background?.Mutate() as GradientDrawable;
            if (bg != null)
            {
                bg.SetColor(bgColor);
                int strokeWidth = (int)(1 * resources.DisplayMetrics.Density);
                bg.SetStroke(strokeWidth, new Color(textColor));
            }
        }

        public static void StyleNoSlotText(TextView view, bool hasFreeSlot)
        {
            if (hasFreeSlot)
            {
                view.Visibility = ViewStates.Gone;
                return;
            }
            view.Visibility = ViewStates.Visible;
            view.Text = "!";
        }

        public static void StyleSpeedChip(TextView chip, string speedText)
        {
            chip.Visibility = ViewStates.Visible;
            chip.Text = speedText;

            var resources = chip.Context.Resources;
            var theme = chip.Context.Theme;
            int textColor = resources.GetColor(Resource.Color.searchChipOtherText, theme);
            int bgColor = resources.GetColor(Resource.Color.searchChipOtherBg, theme);

            chip.SetTextColor(new Color(textColor));
            chip.SetTypeface(chip.Typeface, TypefaceStyle.Bold);
            chip.SetTextSize(ComplexUnitType.Sp, 10);

            var bg = chip.Background?.Mutate() as GradientDrawable;
            if (bg != null)
            {
                bg.SetColor(bgColor);
                int strokeWidth = (int)(1 * resources.DisplayMetrics.Density);
                bg.SetStroke(strokeWidth, new Color(textColor));
            }
        }
    }
}
