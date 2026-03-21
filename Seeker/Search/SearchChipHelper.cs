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

        /// <summary>
        /// Extracts just the bitrate portion like "320kbps" from "mp3 (320 kbs)"
        /// </summary>
        public static string ExtractBitRate(string dominantFileTypeStr)
        {
            if (string.IsNullOrEmpty(dominantFileTypeStr))
            {
                return "";
            }
            // format is like "mp3 (320 kbs)" - extract the part in parens
            int parenStart = dominantFileTypeStr.IndexOf('(');
            int parenEnd = dominantFileTypeStr.IndexOf(')');
            if (parenStart >= 0 && parenEnd > parenStart)
            {
                string inner = dominantFileTypeStr.Substring(parenStart + 1, parenEnd - parenStart - 1).Trim();
                // "320 kbs" -> "320kbps"
                inner = inner.Replace(" ", "").Replace("kbs", "kbps");
                return inner;
            }
            return "";
        }

        public static (int textColorResId, int bgColorResId) GetFormatChipColorResIds(string formatName)
        {
            switch (formatName)
            {
                // orange
                case "flac":
                case "epub":
                    return (Resource.Color.searchChipFlacText, Resource.Color.searchChipFlacBg);
                // blue
                case "mp3":
                case "pdf":
                    return (Resource.Color.searchChipMp3Text, Resource.Color.searchChipMp3Bg);
                // pink
                case "m4a":
                case "azw3":
                    return (Resource.Color.searchChipM4aText, Resource.Color.searchChipM4aBg);
                // green
                case "wav":
                    return (Resource.Color.searchChipWavText, Resource.Color.searchChipWavBg);
                // teal
                case "aac":
                case "mobi":
                    return (Resource.Color.searchChipAacText, Resource.Color.searchChipAacBg);
                case "wma":
                    return (Resource.Color.searchChipWmaText, Resource.Color.searchChipWmaBg);
                case "aiff":
                    return (Resource.Color.searchChipAiffText, Resource.Color.searchChipAiffBg);
                default:
                    return (Resource.Color.searchChipOtherText, Resource.Color.searchChipOtherBg);
            }
        }

        private static void ApplyChipStyle(TextView chip, int textColor, int bgColor)
        {
            chip.SetTextColor(new Color(textColor));
            chip.SetTypeface(chip.Typeface, TypefaceStyle.Bold);
            chip.SetTextSize(ComplexUnitType.Sp, 10);

            var bg = chip.Background?.Mutate() as GradientDrawable;
            if (bg != null)
            {
                bg.SetColor(bgColor);
                bg.SetStroke(0, Color.Transparent);
            }
        }

        /// <summary>
        /// Shows full format string like "FLAC 320kbps" as a colored chip
        /// </summary>
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
            ApplyChipStyle(chip, resources.GetColor(textColorResId, theme), resources.GetColor(bgColorResId, theme));
        }

        /// <summary>
        /// Shows just the format name in uppercase like "FLAC" as a colored chip
        /// </summary>
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
            ApplyChipStyle(chip, resources.GetColor(textColorResId, theme), resources.GetColor(bgColorResId, theme));
        }

        /// <summary>
        /// Shows format and bitrate as a single combined chip. e.g. [WAV · 192kbps]
        /// bitrateChip is always hidden (kept for layout compat).
        /// </summary>
        public static void StyleFormatAndBitrateChips(TextView formatChip, TextView bitrateChip, string fullFormatStr)
        {
            bitrateChip.Visibility = ViewStates.Gone;
            if (string.IsNullOrEmpty(fullFormatStr))
            {
                formatChip.Visibility = ViewStates.Gone;
                return;
            }
            string formatName = ExtractFormatName(fullFormatStr);
            string bitRate = ExtractBitRate(fullFormatStr);

            string chipText = formatName.ToUpperInvariant();
            if (!string.IsNullOrEmpty(bitRate))
            {
                chipText += " \u00b7 " + bitRate;
            }

            formatChip.Visibility = ViewStates.Visible;
            formatChip.Text = chipText;

            var (textColorResId, bgColorResId) = GetFormatChipColorResIds(formatName);
            var resources = formatChip.Context.Resources;
            var theme = formatChip.Context.Theme;
            ApplyChipStyle(formatChip,
                resources.GetColor(textColorResId, theme),
                resources.GetColor(bgColorResId, theme));
        }

        public static void StyleQueueChip(TextView chip, bool hasFreeSlot, int queueLength)
        {
            if (hasFreeSlot)
            {
                chip.Visibility = ViewStates.Gone;
                return;
            }
            chip.Visibility = ViewStates.Visible;
            chip.Text = "Q:" + queueLength;

            var resources = chip.Context.Resources;
            var theme = chip.Context.Theme;
            ApplyChipStyle(chip,
                resources.GetColor(Resource.Color.searchChipQueueText, theme),
                resources.GetColor(Resource.Color.searchChipQueueBg, theme));
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
            ApplyChipStyle(chip,
                resources.GetColor(Resource.Color.searchChipNoSlotText, theme),
                resources.GetColor(Resource.Color.searchChipNoSlotBg, theme));
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

        /// <summary>
        /// Shows "Free" in green or "No slot" in red
        /// </summary>
        public static void StyleFreeSlotIndicator(TextView view, bool hasFreeSlot)
        {
            view.Visibility = ViewStates.Visible;
            if (hasFreeSlot)
            {
                view.Text = "Free";
                var resources = view.Context.Resources;
                var theme = view.Context.Theme;
                int greenColor = resources.GetColor(Resource.Color.searchChipMp3Text, theme);
                view.SetTextColor(new Color(greenColor));
            }
            else
            {
                view.Text = "No slot";
                var resources = view.Context.Resources;
                var theme = view.Context.Theme;
                int redColor = resources.GetColor(Resource.Color.searchChipNoSlotText, theme);
                view.SetTextColor(new Color(redColor));
            }
        }

        /// <summary>
        /// Shows "No slot · Q:N" in red, or "Free" in green
        /// </summary>
        public static void StyleSlotAndQueue(TextView view, bool hasFreeSlot, int queueLength)
        {
            view.Visibility = ViewStates.Visible;
            if (hasFreeSlot)
            {
                view.Text = "Free";
                var resources = view.Context.Resources;
                var theme = view.Context.Theme;
                int greenColor = resources.GetColor(Resource.Color.searchChipMp3Text, theme);
                view.SetTextColor(new Color(greenColor));
            }
            else
            {
                string text = "No slot";
                if (queueLength > 0)
                {
                    text += " \u00b7 Q:" + queueLength;
                }
                view.Text = text;
                var resources = view.Context.Resources;
                var theme = view.Context.Theme;
                int redColor = resources.GetColor(Resource.Color.searchChipNoSlotText, theme);
                view.SetTextColor(new Color(redColor));
            }
        }

        public static void StyleSpeedChip(TextView chip, string speedText)
        {
            chip.Visibility = ViewStates.Visible;
            chip.Text = speedText;

            var resources = chip.Context.Resources;
            var theme = chip.Context.Theme;
            int textColor = resources.GetColor(Resource.Color.searchChipOtherText, theme);
            ApplyChipStyle(chip, textColor,
                resources.GetColor(Resource.Color.searchChipOtherBg, theme));

            var drawables = chip.GetCompoundDrawablesRelative();
            if (drawables[0] != null)
            {
                var tinted = drawables[0].Mutate();
                tinted.SetTint(textColor);
                chip.SetCompoundDrawablesRelativeWithIntrinsicBounds(tinted, null, null, null);
            }
        }

        public static void StyleSpeed(TextView view, string speedText)
        {
            view.Text = speedText;
            var drawables = view.GetCompoundDrawablesRelative();
            if (drawables[0] != null)
            {
                var tinted = drawables[0].Mutate();
                tinted.SetTint(view.CurrentTextColor);
                view.SetCompoundDrawablesRelativeWithIntrinsicBounds(tinted, null, null, null);
            }
        }

        public static void StyleFileCount(TextView view, int fileCount)
        {
            view.Text = fileCount.ToString();
            var drawables = view.GetCompoundDrawablesRelative();
            if (drawables[0] != null)
            {
                var tinted = drawables[0].Mutate();
                tinted.SetTint(view.CurrentTextColor);
                view.SetCompoundDrawablesRelativeWithIntrinsicBounds(tinted, null, null, null);
            }
        }
    }
}
