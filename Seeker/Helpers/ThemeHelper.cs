using Android.Content;
using Common;
using System;

namespace Seeker
{
    public static class ThemeHelper
    {
        public const string ClassicPurple = "Classic Purple";
        public const string Grey = "Grey";
        public const string Blue = "Blue";
        public const string Red = "Red";
        public const string AmoledClassicPurple = "Amoled - Classic Purple";
        public const string AmoledGrey = "Amoled - Grey";

        public static DayThemeType FromDayThemeTypeString(string themeTypeString)
        {
            switch (themeTypeString)
            {
                case ClassicPurple:
                    return DayThemeType.ClassicPurple;
                case Grey:
                    return DayThemeType.Grey;
                case Blue:
                    return DayThemeType.Blue;
                case Red:
                    return DayThemeType.Red;
                default:
                    throw new Exception("unknown");
            }
        }

        public static string ToDayThemeString(DayThemeType dayTheme)
        {
            switch (dayTheme)
            {
                case DayThemeType.ClassicPurple:
                    return ClassicPurple;
                case DayThemeType.Grey:
                    return Grey;
                case DayThemeType.Blue:
                    return Blue;
                case DayThemeType.Red:
                    return Red;
                default:
                    throw new Exception("unknown");
            }
        }

        public static int ToDayThemeProper(DayThemeType dayTheme)
        {
            switch (dayTheme)
            {
                case DayThemeType.ClassicPurple:
                    return Resource.Style.DefaultLight;
                case DayThemeType.Grey:
                    return Resource.Style.DefaultDark_Grey; //TODO
                case DayThemeType.Blue:
                    return Resource.Style.DefaultLight_Blue;
                case DayThemeType.Red:
                    return Resource.Style.DefaultLight_Red;
                default:
                    throw new Exception("unknown");
            }
        }

        public static NightThemeType FromNightThemeTypeString(string themeTypeString)
        {
            switch (themeTypeString)
            {
                case ClassicPurple:
                    return NightThemeType.ClassicPurple;
                case Grey:
                    return NightThemeType.Grey;
                case Blue:
                    return NightThemeType.Blue;
                case Red:
                    return NightThemeType.Red;
                case AmoledClassicPurple:
                    return NightThemeType.ClassicPurple;
                case AmoledGrey:
                    return NightThemeType.AmoledGrey;
                default:
                    throw new Exception("unknown");
            }
        }


        public static string ToNightThemeString(NightThemeType nightTheme)
        {
            switch (nightTheme)
            {
                case NightThemeType.ClassicPurple:
                    return ClassicPurple;
                case NightThemeType.Grey:
                    return Grey;
                case NightThemeType.Blue:
                    return Blue;
                case NightThemeType.Red:
                    return Red;
                case NightThemeType.AmoledClassicPurple:
                    return ClassicPurple;
                case NightThemeType.AmoledGrey:
                    return AmoledGrey;
                default:
                    throw new Exception("unknown");
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
                    return Resource.Style.DefaultDark_Blue; //doesnt exist
                case NightThemeType.AmoledClassicPurple:
                    return Resource.Style.Amoled;
                case NightThemeType.AmoledGrey:
                    return Resource.Style.Amoled_Grey;
                default:
                    throw new Exception("unknown");
            }
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
                            return ThemeHelper.ToDayThemeProper(DayThemeType.ClassicPurple);
                        case NightThemeType.Blue:
                            return ThemeHelper.ToDayThemeProper(DayThemeType.Blue);
                        default:
                            return ThemeHelper.ToDayThemeProper(DayThemeType.ClassicPurple);
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
                        default:
                            return ThemeHelper.ToNightThemeProper(NightThemeType.ClassicPurple);
                    }
                }
            }
        }

    }

    enum DirectoryType : ushort
    {
        Download = 0,
        Upload = 1,
        Incomplete = 2
    }
}
