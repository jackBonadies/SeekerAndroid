using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;

namespace Seeker.Search
{
    // search chip colors are constant and only differ for day vs night
    //   so bind them all ahead of time instead of at bind time
    public sealed class SearchChipPalette
    {
        private static SearchChipPalette day;
        private static SearchChipPalette night;

        private static readonly (int text, int bg)[] FormatColorResIds =
        {
            (Resource.Color.searchChipFlacText, Resource.Color.searchChipFlacBg),
            (Resource.Color.searchChipMp3Text, Resource.Color.searchChipMp3Bg),
            (Resource.Color.searchChipM4aText, Resource.Color.searchChipM4aBg),
            (Resource.Color.searchChipWavText, Resource.Color.searchChipWavBg),
            (Resource.Color.searchChipAacText, Resource.Color.searchChipAacBg),
            (Resource.Color.searchChipWmaText, Resource.Color.searchChipWmaBg),
            (Resource.Color.searchChipAiffText, Resource.Color.searchChipAiffBg),
            (Resource.Color.searchChipOtherText, Resource.Color.searchChipOtherBg),
        };

        private readonly (Color text, Color bg)[] formatColors;
        public readonly Color QueueText;
        public readonly Color QueueBg;

        private SearchChipPalette(Resources resources, Resources.Theme theme)
        {
            formatColors = new (Color, Color)[FormatColorResIds.Length];
            for (int i = 0; i < FormatColorResIds.Length; i++)
            {
                formatColors[i] = (resources.GetColor(FormatColorResIds[i].text, theme),
                    resources.GetColor(FormatColorResIds[i].bg, theme));
            }
            QueueText = resources.GetColor(Resource.Color.searchChipQueueText, theme);
            QueueBg = resources.GetColor(Resource.Color.searchChipQueueBg, theme);
        }

        public static SearchChipPalette Get(Context context)
        {
            bool isNight = (context.Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
            ref SearchChipPalette slot = ref (isNight ? ref night : ref day);
            if (slot == null)
            {
                slot = new SearchChipPalette(context.Resources, context.Theme);
            }
            return slot;
        }

        public (Color text, Color bg) ForFormat(string formatName)
        {
            return formatColors[SearchChipHelper.GetFormatColorIndex(formatName)];
        }
    }

    // A chip TextView plus its own mutated copy of the search_format_chip_bg background, taken
    // once at row creation so that a bind is Visibility + Text + SetTextColor + SetColor.
    public sealed class SearchChip
    {
        private readonly TextView view;
        private readonly GradientDrawable background;

        public SearchChip(TextView view)
        {
            this.view = view;
            background = view.Background?.Mutate() as GradientDrawable;
        }

        public void Hide()
        {
            view.Visibility = ViewStates.Gone;
        }

        public void Show(string text, Color textColor, Color bgColor)
        {
            view.Visibility = ViewStates.Visible;
            view.Text = text;
            view.SetTextColor(textColor);
            background?.SetColor(bgColor);
        }
    }

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

        // "mp3 (320 kbs)" -> "320kbps"
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

        // Index into SearchChipPalette's format colours
        public static int GetFormatColorIndex(string formatName)
        {
            switch (formatName)
            {
                // orange
                case "flac":
                case "epub":
                    return 0;
                // blue
                case "mp3":
                case "pdf":
                    return 1;
                // pink
                case "m4a":
                case "azw3":
                    return 2;
                // green
                case "wav":
                    return 3;
                // teal
                case "aac":
                case "mobi":
                    return 4;
                case "wma":
                    return 5;
                case "aiff":
                    return 6;
                default:
                    int hash = formatName.GetHashCode() & 0x7FFFFFFF;
                    return hash % 8;
            }
        }

        // One combined chip, e.g. [WAV · 192kbps]
        public static void StyleFormatChip(SearchChip chip, string fullFormatStr, SearchChipPalette palette)
        {
            if (string.IsNullOrEmpty(fullFormatStr))
            {
                chip.Hide();
                return;
            }
            string formatName = ExtractFormatName(fullFormatStr);
            string bitRate = ExtractBitRate(fullFormatStr);

            string chipText = formatName.ToUpperInvariant();
            if (!string.IsNullOrEmpty(bitRate))
            {
                chipText += " \u00b7 " + bitRate;
            }

            var (textColor, bgColor) = palette.ForFormat(formatName);
            chip.Show(chipText, textColor, bgColor);
        }

        public static void StyleQueueChip(SearchChip chip, bool hasFreeSlot, int queueLength, SearchChipPalette palette)
        {
            if (hasFreeSlot)
            {
                chip.Hide();
                return;
            }
            chip.Show("Q:" + queueLength, palette.QueueText, palette.QueueBg);
        }
    }
}
