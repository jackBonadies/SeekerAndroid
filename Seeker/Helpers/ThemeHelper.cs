using Android.Content;
using Common;
using System;

namespace Seeker
{
    /// <summary>
    /// Maps the persisted theme preferences (<see cref="DayThemeType"/> /
    /// <see cref="NightThemeType"/> / AppCompatDelegate night mode) onto the
    /// concrete <c>Resource.Style</c> ids defined in Resources/values/themes.xml.
    /// </summary>
    /// <remarks>
    /// The enum values are persisted as ints, so the mapping must stay total:
    /// every declared value maps to a style, and unknown (e.g. corrupted or
    /// future) ints fall back to the modern default instead of throwing.
    /// Theme switching is purely an activity/configuration concern: nothing in
    /// this class (or its callers) may touch the Soulseek client, the services
    /// or the transfer engine.
    /// </remarks>
    public static class ThemeHelper
    {
        public static int ToDayThemeProper(DayThemeType dayTheme)
        {
            switch (dayTheme)
            {
                case DayThemeType.ClassicPurple:
                    return Resource.Style.DefaultLight;
                case DayThemeType.Grey:
                    return Resource.Style.DefaultLight_Grey;
                case DayThemeType.Blue:
                    return Resource.Style.DefaultLight_Blue;
                case DayThemeType.Red:
                    return Resource.Style.DefaultLight_Red;
                case DayThemeType.Modern:
                    return Resource.Style.ModernLight;
                default:
                    return Resource.Style.ModernLight;
            }
        }

        public static int ToNightThemeProper(NightThemeType nightTheme)
        {
            switch (nightTheme)
            {
                case NightThemeType.ClassicPurple:
                    return Resource.Style.DefaultDark;
                case NightThemeType.Grey:
                    return Resource.Style.DefaultDark_Grey;
                case NightThemeType.Blue:
                    return Resource.Style.DefaultDark_Blue;
                case NightThemeType.Red:
                    return Resource.Style.DefaultDark_Red;
                case NightThemeType.AmoledClassicPurple:
                    return Resource.Style.Amoled;
                case NightThemeType.AmoledGrey:
                    return Resource.Style.Amoled_Grey;
                case NightThemeType.Modern:
                    return Resource.Style.ModernDark;
                default:
                    return Resource.Style.ModernDark;
            }
        }

        /// <summary>
        /// Clamps a persisted day variant. Existing values (0..3) are untouched;
        /// unknown values (corrupted prefs, downgrades from a newer build) fall
        /// back to the modern default instead of leaving an invalid enum.
        /// </summary>
        public static DayThemeType NormalizeDayTheme(int storedValue)
        {
            // Reject out-of-range ints before the ushort cast: a cast first would turn
            // e.g. -65536 into 0 and silently select ClassicPurple.
            if (storedValue < 0 || storedValue > ushort.MaxValue)
            {
                return DayThemeType.Modern;
            }
            if (Enum.IsDefined(typeof(DayThemeType), (ushort)storedValue))
            {
                return (DayThemeType)storedValue;
            }
            return DayThemeType.Modern;
        }

        /// <summary>See <see cref="NormalizeDayTheme"/>.</summary>
        public static NightThemeType NormalizeNightTheme(int storedValue)
        {
            if (storedValue < 0 || storedValue > ushort.MaxValue)
            {
                return NightThemeType.Modern;
            }
            if (Enum.IsDefined(typeof(NightThemeType), (ushort)storedValue))
            {
                return (NightThemeType)storedValue;
            }
            return NightThemeType.Modern;
        }

        /// <summary>
        /// True when the context is currently in night (dark) configuration.
        /// This is the app's effective configuration: AppCompatDelegate applies
        /// the "follow system / always light / always dark" preference per app
        /// context, so this is not necessarily the device setting.
        /// </summary>
        public static bool IsNightMode(Context context)
        {
            if (context == null)
            {
                return false;
            }
            return (context.Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask)
                == Android.Content.Res.UiMode.NightYes;
        }

        /// <summary>
        /// The theme id for the current configuration. Used by
        /// <see cref="UiHelpers.SetActivityTheme"/> so that day and night both
        /// honour the corresponding stored variant.
        /// </summary>
        public static int GetActivityTheme(Context context)
        {
            if (IsNightMode(context))
            {
                return ToNightThemeProper(PreferencesState.NightModeVariant);
            }
            return ToDayThemeProper(PreferencesState.DayModeVariant);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="isNightMode">This is for the SYSTEM</param>
        /// <returns></returns>
        public static int GetThemeInChosenDayNightMode(bool isNightMode, Context c)
        {
            Context contextToUse = c == null ? SeekerState.ActiveActivityRef : c;
            if (contextToUse.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes))
            {
                if (isNightMode)
                {
                    return ThemeHelper.ToNightThemeProper(PreferencesState.NightModeVariant);
                }
                else
                {
                    switch (PreferencesState.NightModeVariant)
                    {
                        case NightThemeType.ClassicPurple:
                        case NightThemeType.AmoledClassicPurple:
                            return ThemeHelper.ToDayThemeProper(DayThemeType.ClassicPurple);
                        case NightThemeType.Blue:
                            return ThemeHelper.ToDayThemeProper(DayThemeType.Blue);
                        case NightThemeType.Red:
                            return ThemeHelper.ToDayThemeProper(DayThemeType.Red);
                        case NightThemeType.Grey:
                        case NightThemeType.AmoledGrey:
                            return ThemeHelper.ToDayThemeProper(DayThemeType.Grey);
                        case NightThemeType.Modern:
                            return ThemeHelper.ToDayThemeProper(DayThemeType.Modern);
                        default:
                            return ThemeHelper.ToDayThemeProper(DayThemeType.Modern);
                    }
                }
            }
            else
            {
                if (!isNightMode)
                {
                    return ThemeHelper.ToDayThemeProper(PreferencesState.DayModeVariant);
                }
                else
                {
                    switch (PreferencesState.DayModeVariant)
                    {
                        case DayThemeType.ClassicPurple:
                            return ThemeHelper.ToNightThemeProper(NightThemeType.ClassicPurple);
                        case DayThemeType.Blue:
                            return ThemeHelper.ToNightThemeProper(NightThemeType.Blue);
                        case DayThemeType.Red:
                            return ThemeHelper.ToNightThemeProper(NightThemeType.Red);
                        case DayThemeType.Grey:
                            return ThemeHelper.ToNightThemeProper(NightThemeType.Grey);
                        case DayThemeType.Modern:
                            return ThemeHelper.ToNightThemeProper(NightThemeType.Modern);
                        default:
                            return ThemeHelper.ToNightThemeProper(NightThemeType.Modern);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the theme + system bar icon contrast to an activity that is
        /// already attached (used when the theme changes while an activity is
        /// alive). Callers must recreate the activities afterwards, see
        /// <see cref="UiHelpers.ApplyThemeChange"/>.
        /// </summary>
        public static void ApplyThemeToAttachedActivity(Android.App.Activity activity)
        {
            if (activity == null)
            {
                return;
            }
            UiHelpers.SetActivityTheme(activity);
            UiHelpers.ApplySystemBarIconContrast(activity);
        }
    }

    enum DirectoryType : ushort
    {
        Download = 0,
        Upload = 1,
        Incomplete = 2
    }
}
