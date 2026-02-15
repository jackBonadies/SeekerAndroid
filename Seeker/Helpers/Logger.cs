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
using System;
using log = Android.Util.Log;

namespace Seeker.Helpers
{
    public static class Logger
    {
        public const string LogCatTag = "seeker";
        public static bool CrashlyticsEnabled = true;

        public static void Debug(string msg)
        {
            if (SeekerApplication.LOG_DIAGNOSTICS)
            {
                SeekerApplication.AppendMessageToDiagFile(msg);
            }
#if ADB_LOGCAT
            log.Debug(LogCatTag, msg);
#endif
        }

        public static void FirebaseError(string msg, Exception e)
        {
            Firebase($"{msg} msg: {e.Message} stack: {e.StackTrace}");
        }

        public static void Firebase(string msg)
        {
            if (SeekerApplication.LOG_DIAGNOSTICS)
            {
                SeekerApplication.AppendMessageToDiagFile(msg);
            }
#if !IzzySoft
            if (CrashlyticsEnabled)
            {
                global::Firebase.Crashlytics.FirebaseCrashlytics.Instance.RecordException(new Java.Lang.Throwable(msg));
            }
#endif
#if ADB_LOGCAT
            log.Debug(LogCatTag, msg);
#endif
        }

        public static void InfoFirebase(string msg)
        {
            if (SeekerApplication.LOG_DIAGNOSTICS)
            {
                SeekerApplication.AppendMessageToDiagFile(msg);
            }
#if !IzzySoft
            if (CrashlyticsEnabled)
            {
                global::Firebase.Crashlytics.FirebaseCrashlytics.Instance.Log(msg);
            }
#endif
#if ADB_LOGCAT
            log.Debug(LogCatTag, msg);
#endif
        }
    }
}
