/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */
using Android.App;
using Android.Content;
using Android.OS;
using Common;
using System;

namespace Seeker.Helpers
{
    public static class LocaleHelper
    {
        public static bool HasProperPerAppLanguageSupport()
        {
            return OperatingSystem.IsAndroidVersionAtLeast(33);
        }

        public static string GetLegacyLanguageString()
        {
            if (HasProperPerAppLanguageSupport())
            {
                var lm = (LocaleManager)SeekerApplication.ApplicationContext.GetSystemService(Context.LocaleService);
                LocaleList appLocales = lm.ApplicationLocales;
                if (appLocales.IsEmpty)
                {
                    return PreferencesState.FieldLangAuto;
                }
                else
                {
                    Java.Util.Locale locale = appLocales.Get(0);
                    string lang = locale.Language; // ex. fr, uk
                    string country = locale.Country; // ex. BR, PT, CN
                    if (!string.IsNullOrEmpty(country))
                    {
                        return lang + "-r" + country; // e.g. pt-rBR, pt-rPT, zh-rCN
                    }
                    return lang;
                }
            }
            else
            {
                return PreferencesState.Language;
            }
        }

        /// <summary>
        /// converts say "pt-rBR" to "pt-BR"
        /// </summary>
        public static string FormatLocaleFromResourcesToStandard(string locale)
        {
            if (locale.Length == 6 && locale.Contains("-r"))
            {
                return locale.Replace("-r", "-");
            }
            else
            {
                return locale;
            }
        }

        public static Java.Util.Locale LocaleFromString(string localeString)
        {
            Java.Util.Locale locale = null;
            if (localeString.Contains("-r"))
            {
                var parts = localeString.Replace("-r", "-").Split('-');
                locale = new Java.Util.Locale(parts[0], parts[1]);
            }
            else
            {
                locale = new Java.Util.Locale(localeString);
            }
            return locale;
        }

        public static string LocaleToString(Java.Util.Locale locale)
        {
            //"en" ""
            //"pt" "br"
            if (string.IsNullOrEmpty(locale.Variant))
            {
                return locale.Language;
            }
            else
            {
                return locale.Language + "-r" + locale.Variant.ToUpper();
            }
        }

        public static bool AreLocalesSame(Java.Util.Locale locale1, Java.Util.Locale locale2)
        {
            return LocaleToString(locale1) == LocaleToString(locale2);
        }

        public static void SetLanguage(string language)
        {
            if (HasProperPerAppLanguageSupport())
            {
                var lm = (LocaleManager)SeekerApplication.ApplicationContext.GetSystemService(Context.LocaleService);

                if (language == PreferencesState.FieldLangAuto)
                {
                    lm.ApplicationLocales = LocaleList.EmptyLocaleList;
                }
                else
                {
                    lm.ApplicationLocales = LocaleList.ForLanguageTags(FormatLocaleFromResourcesToStandard(language));
                }
            }
            else
            {
                SetLanguageLegacy(PreferencesState.Language, true);
            }
        }

        public static void SetLanguageLegacy(string language, bool changed)
        {
            string localeString = language;
            var app = (Application)SeekerApplication.ApplicationContext;
            var res = app.Resources;
            var config = res.Configuration;
            var displayMetrics = res.DisplayMetrics;

            var currentLocale = config.Locale;

            if (LocaleToString(currentLocale) == language)
            {
                return;
            }

            if (language == PreferencesState.FieldLangAuto && SeekerState.SystemLanguage == LocaleToString(currentLocale))
            {
                return;
            }

            Java.Util.Locale locale = language != PreferencesState.FieldLangAuto ? LocaleFromString(localeString) : LocaleFromString(SeekerState.SystemLanguage);

            Java.Util.Locale.Default = locale;
            config.SetLocale(locale);

            app.BaseContext.Resources.UpdateConfiguration(config, displayMetrics);

            if (changed)
            {
                SeekerApplication.RecreateActivies();
            }
        }
    }
}
