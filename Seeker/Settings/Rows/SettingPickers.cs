using Android.Content;
using Common;
using Seeker.Helpers;
using System.Collections.Generic;

namespace Seeker.Settings.Rows
{
    /// <summary>
    /// Static methods to launch BottomSheets (language, theme, day/night)
    /// </summary>
    internal static class SettingPickers
    {
        public static List<(string label, string value)> BuildLanguageOptions(Context ctx)
        {
            return new List<(string label, string value)>
            {
                (ctx.GetString(Resource.String.Automatic), PreferencesState.FieldLangAuto),
                ("العربية", PreferencesState.FieldLangAr),            // Arabic
                ("Bahasa Indonesia", PreferencesState.FieldLangId),   // Indonesian
                ("Català", PreferencesState.FieldLangCa),             // Catalan
                ("čeština", PreferencesState.FieldLangCs),            // Czech
                ("Dansk", PreferencesState.FieldLangDa),              // Danish
                ("Deutsch", PreferencesState.FieldLangDe),            // German
                ("English", PreferencesState.FieldLangEn),
                ("Español", PreferencesState.FieldLangEs),            // Spanish
                ("Français", PreferencesState.FieldLangFr),           // French
                ("italiano", PreferencesState.FieldLangIt),           // Italian
                ("Magyar", PreferencesState.FieldLangHu),             // Hungarian
                ("Nederlands", PreferencesState.FieldLangNl),         // Dutch
                ("Norsk", PreferencesState.FieldLangNo),              // Norwegian
                ("Polski", PreferencesState.FieldLangPl),             // Polish
                ("Português (Brazil)", PreferencesState.FieldLangPtBr),
                ("Português (Portugal)", PreferencesState.FieldLangPtPt),
                ("ру́сский язы́к", PreferencesState.FieldLangRu),       // Russian
                ("Srpski", PreferencesState.FieldLangSr),             // Serbian
                ("українська мо́ва", PreferencesState.FieldLangUk),     // Ukrainian
                ("简体中文", PreferencesState.FieldLangZhCn),           // Chinese Simplified
                ("日本語", PreferencesState.FieldLangJa),              // Japanese
            };
        }

        public static void PickLanguage(ISettingsHost host, ValueRow row)
        {
            var options = BuildLanguageOptions(host.Activity);
            OptionPickerBottomSheet.ShowOptions(host, row, options,
                // GetLegacyLanguageString reflects the per-app locale on API 33+,
                // falling back to the saved preference on older devices.
                () => LocaleHelper.GetLegacyLanguageString(),
                v =>
                {
                    if (LocaleHelper.GetLegacyLanguageString() == v)
                    {
                        return;
                    }
                    PreferencesState.Language = v;
                    PreferencesManager.SaveLanguage();
                    LocaleHelper.SetLanguage(v);
                });
        }

        public static void PickDayNightMode(ISettingsHost host, ValueRow row)
        {
            var ctx = host.Activity;
            var options = new List<(string label, int value)>
            {
                (ctx.GetString(Resource.String.follow_system), -1),
                (ctx.GetString(Resource.String.always_light), 1),
                (ctx.GetString(Resource.String.always_dark), 2),
            };
            OptionPickerBottomSheet.ShowOptions(host, row, options,
                () => PreferencesState.DayNightMode,
                v => {
                    var old = PreferencesState.DayNightMode;
                    PreferencesState.DayNightMode = v;
                    PreferencesManager.SaveDayNightMode();
                    if (old != v)
                    {
                        AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = v;
                    }
                });
        }

        public static void PickDayVariant(ISettingsHost host, ValueRow row)
        {
            var ctx = host.Activity;
            var variants = new[]
            {
                DayThemeType.ClassicPurple, DayThemeType.Red, DayThemeType.Blue, DayThemeType.Grey,
            };
            var options = new List<(string label, DayThemeType value)>(variants.Length);
            foreach (var variant in variants)
            {
                options.Add((SettingValueFormat.DayVariantLabel(ctx, variant), variant));
            }
            OptionPickerBottomSheet.ShowOptions(host, row, options,
                () => PreferencesState.DayModeVariant,
                v => {
                    var old = PreferencesState.DayModeVariant;
                    PreferencesState.DayModeVariant = v;
                    PreferencesManager.SaveDayModeVariant();
                    if (old != v && !host.Activity.Resources.Configuration.UiMode
                            .HasFlag(Android.Content.Res.UiMode.NightYes))
                    {
                        UiHelpers.SetActivityTheme(host.Activity);
                        SeekerApplication.RecreateActivies();
                    }
                });
        }

        public static void PickNightVariant(ISettingsHost host, ValueRow row)
        {
            var ctx = host.Activity;
            var variants = new[]
            {
                NightThemeType.ClassicPurple, NightThemeType.Grey, NightThemeType.Blue,
                NightThemeType.Red, NightThemeType.AmoledClassicPurple, NightThemeType.AmoledGrey,
            };
            var options = new List<(string label, NightThemeType value)>(variants.Length);
            foreach (var variant in variants)
            {
                options.Add((SettingValueFormat.NightVariantLabel(ctx, variant), variant));
            }
            OptionPickerBottomSheet.ShowOptions(host, row, options,
                () => PreferencesState.NightModeVariant,
                v => {
                    var old = PreferencesState.NightModeVariant;
                    PreferencesState.NightModeVariant = v;
                    PreferencesManager.SaveNightModeVariant();
                    if (old != v && host.Activity.Resources.Configuration.UiMode
                            .HasFlag(Android.Content.Res.UiMode.NightYes))
                    {
                        UiHelpers.SetActivityTheme(host.Activity);
                        SeekerApplication.RecreateActivies();
                    }
                });
        }
    }
}
