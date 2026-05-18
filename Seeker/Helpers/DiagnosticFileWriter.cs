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
using Android.Widget;
using AndroidX.DocumentFile.Provider;
using Common;
using System;

namespace Seeker.Helpers
{
    public static class DiagnosticFileWriter
    {
        private static DocumentFile DiagnosticTextFile = null;
        private static System.IO.StreamWriter DiagnosticStreamWriter = null;
        private static bool diagnosticFilesystemErrorShown = false;

        public static void Subscribe()
        {
            SeekerState.SoulseekClient.DiagnosticGenerated += SoulseekClient_DiagnosticGenerated;
        }

        public static void Unsubscribe()
        {
            SeekerState.SoulseekClient.DiagnosticGenerated -= SoulseekClient_DiagnosticGenerated;
        }

        public static void AppendIfEnabled(string msg)
        {
            if (!PreferencesState.LogDiagnostics)
            {
                return;
            }
            Append(msg);
        }

        public static void Append(string msg)
        {
            AppendLine(CreateMessage(msg));
        }

        private static void SoulseekClient_DiagnosticGenerated(object sender, Soulseek.Diagnostics.DiagnosticEventArgs e)
        {
            AppendLine(CreateMessage(e));
        }

        private static string CreateMessage(Soulseek.Diagnostics.DiagnosticEventArgs e)
        {
            string timestamp = e.Timestamp.ToString("[MM_dd-hh:mm:ss] ");
            string body = null;
            if (e.IncludesException)
            {
                body = e.Message + System.Environment.NewLine + e.Exception.Message + System.Environment.NewLine + e.Exception.StackTrace;
            }
            else
            {
                body = e.Message;
            }
            return timestamp + body;
        }

        private static string CreateMessage(string line)
        {
            string timestamp = DateTime.UtcNow.ToString("[MM_dd-hh:mm:ss] ");
            return timestamp + line;
        }

        private static void AppendLine(string line)
        {
            try
            {
                if (DiagnosticTextFile == null)
                {
                    if (SeekerState.RootDocumentFile != null) //i.e. if api > 21 and they set it.
                    {
                        DiagnosticTextFile = SeekerState.RootDocumentFile.FindFile("seeker_diagnostics.txt");
                        if (DiagnosticTextFile == null)
                        {
                            DiagnosticTextFile = SeekerState.RootDocumentFile.CreateFile("text/plain", "seeker_diagnostics");
                            if (DiagnosticTextFile == null)
                            {
                                return;
                            }
                        }
                    }
                    else if (SeekerState.UseLegacyStorage() || !PreferencesState.SaveDataDirectoryUriIsFromTree) //if api < 30 and they did not set it. OR api <= 21 and they did set it.
                    {
                        //when the directory is unset.
                        string fullPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                        if (!string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri))
                        {
                            fullPath = Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri).Path;
                        }

                        var containingDir = new Java.IO.File(fullPath);

                        var javaDiagFile = new Java.IO.File(fullPath + @"/" + "seeker_diagnostics.txt");
                        DocumentFile rootDir = DocumentFile.FromFile(new Java.IO.File(fullPath + @"/" + "seeker_diagnostics.txt"));
                        if (!javaDiagFile.Exists())
                        {
                            if (containingDir.CanWrite())
                            {
                                bool success = javaDiagFile.CreateNewFile();
                                if (success)
                                {
                                    DiagnosticTextFile = rootDir;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            DiagnosticTextFile = rootDir;
                        }
                    }
                    else //if api >29 and they did not set it. nothing we can do.
                    {
                        return;
                    }
                }

                if (DiagnosticStreamWriter == null)
                {
                    System.IO.Stream outputStream = SeekerApplication.ApplicationContext.ContentResolver.OpenOutputStream(DiagnosticTextFile.Uri, "wa");
                    if (outputStream == null)
                    {
                        return;
                    }
                    DiagnosticStreamWriter = new System.IO.StreamWriter(outputStream);
                    if (DiagnosticStreamWriter == null)
                    {
                        return;
                    }
                }

                DiagnosticStreamWriter.WriteLine(line);
                DiagnosticStreamWriter.Flush();
            }
            catch (Exception ex)
            {
                if (!diagnosticFilesystemErrorShown)
                {
                    Logger.Firebase("failed to write to diagnostic file " + ex.Message + line + ex.StackTrace);
                    SeekerApplication.Toaster.ShowToast("Failed to write to diagnostic file.", ToastLength.Long);
                    diagnosticFilesystemErrorShown = true;
                }
            }
        }
    }
}
