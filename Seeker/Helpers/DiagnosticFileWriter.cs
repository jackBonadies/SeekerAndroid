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
using Seeker.Services;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Seeker.Helpers
{
    public static class DiagnosticFileWriter
    {
        private const string DiagnosticFileName = "seeker_diagnostics.txt";
        private const string DiagnosticDisplayName = "seeker_diagnostics";
        private const int MaxQueuedLines = 10000;

        private static DocumentFile DiagnosticTextFile = null;
        private static System.IO.StreamWriter DiagnosticStreamWriter = null;
        private static bool diagnosticFilesystemErrorShown = false;
        private static readonly object writeLock = new object();

        private static readonly ConcurrentQueue<string> pendingLines = new ConcurrentQueue<string>();
        private static readonly AutoResetEvent wakeEvent = new AutoResetEvent(false);
        private static readonly object startLock = new object();
        private static volatile bool writerStarted = false;

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
            EnqueueLine(CreateMessage(msg));
        }

        private static void SoulseekClient_DiagnosticGenerated(object sender, Soulseek.Diagnostics.DiagnosticEventArgs e)
        {
            EnqueueLine(CreateMessage(e));
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

        private static void EnqueueLine(string line)
        {
            //bounded: if file resolution permanently fails the consumer never drains,
            //so cap the queue to avoid unbounded growth.
            if (pendingLines.Count >= MaxQueuedLines)
            {
                return;
            }
            pendingLines.Enqueue(line);
            EnsureWriterStarted();
            wakeEvent.Set();
        }

        private static void EnsureWriterStarted()
        {
            if (writerStarted)
            {
                return;
            }
            lock (startLock)
            {
                if (writerStarted)
                {
                    return;
                }
                writerStarted = true;
                _ = Task.Run(WriterLoop);
            }
        }

        private static void WriterLoop()
        {
            while (true)
            {
                wakeEvent.WaitOne();
                DrainAndWrite();
            }
        }

        /// <summary>
        /// Synchronously drains any queued lines to disk. Safe to call from any thread;
        /// serialized against the background consumer by <see cref="writeLock"/>.
        /// </summary>
        public static void FlushBlocking()
        {
            DrainAndWrite();
        }

        private static void DrainAndWrite()
        {
            try
            {
                lock (writeLock)
                {
                    if (pendingLines.IsEmpty)
                    {
                        return;
                    }

                    if (DiagnosticTextFile == null)
                    {
                        DiagnosticTextFile = ResolveDiagnosticFile();
                        if (DiagnosticTextFile == null)
                        {
                            //leave lines queued; the next enqueue retries resolution.
                            return;
                        }
                    }

                    if (DiagnosticStreamWriter == null)
                    {
                        DiagnosticStreamWriter = CreateStreamWriter(DiagnosticTextFile);
                        if (DiagnosticStreamWriter == null)
                        {
                            return;
                        }
                    }

                    while (pendingLines.TryDequeue(out string line))
                    {
                        DiagnosticStreamWriter.WriteLine(line);
                    }
                    DiagnosticStreamWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                if (!diagnosticFilesystemErrorShown)
                {
                    Logger.Firebase("failed to write to diagnostic file " + ex.Message + ex.StackTrace);
                    SeekerApplication.Toaster.ShowToast("Failed to write to diagnostic file.", ToastLength.Long);
                    diagnosticFilesystemErrorShown = true;
                }
            }
        }

        private static DocumentFile ResolveDiagnosticFile()
        {
            if (StorageState.RootDocumentFile != null) //i.e. if api > 21 and they set it.
            {
                DocumentFile file = StorageState.RootDocumentFile.FindFile(DiagnosticFileName);
                if (file == null)
                {
                    file = StorageState.RootDocumentFile.CreateFile("text/plain", DiagnosticDisplayName);
                }
                return file;
            }

            if (PlatformInfo.UseLegacyStorage() || !PreferencesState.SaveDataDirectoryUriIsFromTree) //if api < 30 and they did not set it. OR api <= 21 and they did set it.
            {
                //when the directory is unset.
                string fullPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                if (!string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri))
                {
                    fullPath = Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri).Path;
                }

                var containingDir = new Java.IO.File(fullPath);
                var javaDiagFile = new Java.IO.File(fullPath + @"/" + DiagnosticFileName);

                if (javaDiagFile.Exists())
                {
                    return DocumentFile.FromFile(javaDiagFile);
                }

                if (containingDir.CanWrite() && javaDiagFile.CreateNewFile())
                {
                    return DocumentFile.FromFile(javaDiagFile);
                }

                return null;
            }

            return null; //if api >29 and they did not set it. nothing we can do.
        }

        private static System.IO.StreamWriter CreateStreamWriter(DocumentFile file)
        {
            System.IO.Stream outputStream = SeekerApplication.ApplicationContext.ContentResolver.OpenOutputStream(file.Uri, "wa");
            if (outputStream == null)
            {
                return null;
            }
            return new System.IO.StreamWriter(outputStream);
        }
    }
}
