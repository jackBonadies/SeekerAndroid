using AndroidX.AppCompat.App;
using Common;
using Seeker.Helpers;
using Seeker.Transfers;
using Seeker.UPnP;
using System;
using System.Collections.Generic;

namespace Seeker.Settings.Rows
{
    public static class SettingsCatalog
    {
        private static readonly int[] SearchResultPresets = { 25, 50, 100, 250, 500, 1000, 2000 };

        /// <summary>Force a fresh UPnP discovery/mapping (with user feedback) if listening + UPnP
        /// are enabled. Used when the listening port changes or UPnP is toggled on / retoggled.</summary>
        private static void RescanUpnp()
        {
            if (!PreferencesState.ListenerEnabled || !PreferencesState.ListenerUPnpEnabled)
            {
                return;
            }
            UPnpManager.Instance.Feedback = true;
            UPnpManager.Instance.SearchAndSetMapping();
        }

        /// <summary>Maps the UPnP manager's current icon/message to a toggle-row status line.</summary>
        private static SettingStatus UpnpStatus()
        {
            var iconAndMessage = UPnpManager.Instance.GetIconAndMessage();
            var kind = iconAndMessage.Item1 switch
            {
                UPnpManager.ListeningIcon.PendingIcon => SettingStatusKind.Running,
                UPnpManager.ListeningIcon.SuccessIcon => SettingStatusKind.Success,
                UPnpManager.ListeningIcon.ErrorIcon => SettingStatusKind.Failure,
                _ => SettingStatusKind.None,
            };
            return new SettingStatus { Kind = kind, Text = iconAndMessage.Item2 };
        }

        public static List<SettingRow> Build(ISettingsHost host)
        {
            var rows = new List<SettingRow>();

            // ============================== DOWNLOADS ==============================
            rows.Add(new HeaderRow { Id = "h.downloads", TitleRes = Resource.String.section_downloads });

            rows.Add(new ValueRow
            {
                Id = "downloads.folder",
                TitleRes = Resource.String.CurrentDownloadFolder_,
                KeywordsRes = Resource.String.keywords_download_folder,
                IconRes = Resource.Drawable.browse_48dp,
                TrailingIconRes = Resource.Drawable.ic_arrow_outward,
                ValueProvider = ctx => SettingValueFormat.PrettyUri(PreferencesState.SaveDataDirectoryUri),
                OnClick = (h, r) => h.LaunchDownloadFolderPicker(),
            });

            rows.Add(new ToggleRow
            {
                Id = "downloads.create_subfolders",
                TitleRes = Resource.String.CreateCompleteAndIncomplete,
                IconRes = Resource.Drawable.browse_48dp,
                Getter = () => PreferencesState.CreateCompleteAndIncompleteFolders,
                Setter = v => { PreferencesState.CreateCompleteAndIncompleteFolders = v;
                                PreferencesManager.SaveAdditionalDirectorySettings(); },
            });
            rows.Add(new ToggleRow
            {
                Id = "downloads.username_subfolders",
                TitleRes = Resource.String.CreateUsernameSubfolders,
                SubtitleRes = Resource.String.setting_subtitle_username_subfolders,
                IconRes = Resource.Drawable.browse_48dp,
                Getter = () => PreferencesState.CreateUsernameSubfolders,
                Setter = v => { PreferencesState.CreateUsernameSubfolders = v;
                                PreferencesManager.SaveAdditionalDirectorySettings(); },
            });

            rows.Add(new ToggleRow
            {
                Id = "downloads.skip_subfolders_single",
                TitleRes = Resource.String.DoNotCreateSubfoldersForSingleDownloads,
                IconRes = Resource.Drawable.browse_48dp,
                Getter = () => PreferencesState.NoSubfolderForSingle,
                Setter = v => { PreferencesState.NoSubfolderForSingle = v;
                                PreferencesManager.SaveAdditionalDirectorySettings(); },
            });

            rows.Add(new ToggleRow
            {
                Id = "downloads.override_incomplete",
                TitleRes = Resource.String.ManuallySetIncompleteFolder,
                KeywordsRes = Resource.String.keywords_override_incomplete,
                IconRes = Resource.Drawable.browse_48dp,
                Getter = () => PreferencesState.OverrideDefaultIncompleteLocations,
                Setter = v => { PreferencesState.OverrideDefaultIncompleteLocations = v;
                                PreferencesManager.SaveAdditionalDirectorySettings();
                                host.Adapter?.NotifyParentToggled("downloads.override_incomplete"); },
                DependentRowIds = new[] { "downloads.incomplete_path", /*"downloads.incomplete_clear"*/ },
            });
            rows.Add(new ValueRow
            {
                Id = "downloads.incomplete_path",
                ParentId = "downloads.override_incomplete",
                VisiblePredicate = () => PreferencesState.OverrideDefaultIncompleteLocations,
                TitleRes = Resource.String.CurrentIncompleteFolder,
                KeywordsRes = Resource.String.keywords_incomplete_folder,
                TrailingIconRes = Resource.Drawable.ic_arrow_outward,
                ValueProvider = ctx => SettingValueFormat.PrettyUri(PreferencesState.ManualIncompleteDataDirectoryUri),
                OnClick = (h, r) => h.LaunchIncompleteFolderPicker(),
            });
            rows.Add(new ActionRow
            {
                Id = "downloads.incomplete_clear",
                ParentId = "downloads.override_incomplete",
                ButtonTextRes = Resource.String.ClearFolder,
                Destructive = true,
                DisableIfParentDisabled = false,
                OnClick = (h, r) => h.ClearIncompleteFolder(),
            });

            rows.Add(new ToggleRow
            {
                Id = "downloads.file_backed",
                TitleRes = Resource.String.file_backed_downloads,
                SubtitleRes = Resource.String.setting_subtitle_file_backed,
                IconRes = Resource.Drawable.download_material,
                // The original UI exposes the inverse (memory-backed); store as file-backed semantics.
                Getter = () => !PreferencesState.MemoryBackedDownload,
                Setter = v => { PreferencesState.MemoryBackedDownload = !v;
                                PreferenceWriter.WriteBool(KeyConsts.M_MemoryBackedDownload, PreferencesState.MemoryBackedDownload); },
            });

            rows.Add(new ToggleRow
            {
                Id = "downloads.auto_clear",
                TitleRes = Resource.String.auto_clear_complete,
                KeywordsRes = Resource.String.keywords_auto_clear_downloads,
                IconRes = Resource.Drawable.clear_all_28dp,
                Getter = () => PreferencesState.AutoClearCompleteDownloads,
                Setter = v => { PreferencesState.AutoClearCompleteDownloads = v;
                                PreferenceWriter.WriteBool(KeyConsts.M_AutoClearComplete, v); },
            });

            rows.Add(new ToggleRow
            {
                Id = "downloads.auto_retry",
                TitleRes = Resource.String.AutoRetryFailedOfflineOnline,
                IconRes = Resource.Drawable.autorenew_autojoin_30dp,
                Getter = () => PreferencesState.AutoRetryBackOnline,
                Setter = v => { PreferencesState.AutoRetryBackOnline = v;
                                PreferencesManager.SaveAutoRetryBackOnline(); },
            });

            // ============================== SEARCH ==============================
            rows.Add(new HeaderRow { Id = "h.search", TitleRes = Resource.String.section_search });

            rows.Add(new ValueRow
            {
                Id = "search.max_results",
                TitleRes = Resource.String.max_search_results,
                KeywordsRes = Resource.String.keywords_max_results,
                IconRes = Resource.Drawable.baseline_search_white_24,
                ValueProvider = ctx => PreferencesState.NumberSearchResults.ToString(),
                OnClick = (h, r) => OptionPickerBottomSheet.ShowPresetInts(h, r, SearchResultPresets,
                    () => PreferencesState.NumberSearchResults,
                    v => { PreferencesState.NumberSearchResults = v;
                           PreferenceWriter.WriteInt(KeyConsts.M_NumberSearchResults, v); }),
            });

            rows.Add(new ToggleRow
            {
                Id = "search.smart_filter",
                TitleRes = Resource.String.ShowSmartFilters,
                IconRes = Resource.Drawable.ic_filter_list_black_32dp,
                Getter = () => PreferencesState.ShowSmartFilters,
                Setter = v => { PreferencesState.ShowSmartFilters = v;
                                PreferencesManager.SaveShowSmartFilters(); },
                DependentRowIds = new[] { "search.smart_filter_configure" },
            });
            rows.Add(new NavigationRow
            {
                Id = "search.smart_filter_configure",
                ParentId = "search.smart_filter",
                TitleRes = Resource.String.Configure,
                SubtitleProvider = ctx => SettingValueFormat.SmartFilterSummary(ctx),
                OnClick = (h, r) => h.ConfigureSmartFilters(),
            });

            rows.Add(new ToggleRow
            {
                Id = "search.free_slots",
                TitleRes = Resource.String.only_show_free_slots,
                IconRes = Resource.Drawable.bolt_28dp,
                Getter = () => PreferencesState.FreeUploadSlotsOnly,
                Setter = v => { PreferencesState.FreeUploadSlotsOnly = v;
                                PreferenceWriter.WriteBool(KeyConsts.M_OnlyFreeUploadSlots, v); },
            });

            rows.Add(new ToggleRow
            {
                Id = "search.show_locked_search",
                TitleRes = Resource.String.ShowPrivateLockedSearch,
                KeywordsRes = Resource.String.keywords_locked_search,
                IconRes = Resource.Drawable.lock_28dp,
                // Original checkbox represents "show locked" → !HideLocked.
                Getter = () => !PreferencesState.HideLockedResultsInSearch,
                Setter = v => { PreferencesState.HideLockedResultsInSearch = !v;
                                PreferenceWriter.WriteBool(KeyConsts.M_HideLockedSearch, PreferencesState.HideLockedResultsInSearch); },
            });
            rows.Add(new ToggleRow
            {
                Id = "search.show_locked_browse",
                TitleRes = Resource.String.ShowPrivateLockedBrowse,
                KeywordsRes = Resource.String.keywords_locked_browse,
                IconRes = Resource.Drawable.lock_28dp,
                Getter = () => !PreferencesState.HideLockedResultsInBrowse,
                Setter = v => { PreferencesState.HideLockedResultsInBrowse = !v;
                                PreferenceWriter.WriteBool(KeyConsts.M_HideLockedBrowse, PreferencesState.HideLockedResultsInBrowse); },
            });

            rows.Add(new ToggleRow
            {
                Id = "search.remember_history",
                TitleRes = Resource.String.remember_search_history,
                KeywordsRes = Resource.String.keywords_remember_search_history,
                IconRes = Resource.Drawable.history_28dp,
                Getter = () => PreferencesState.RememberSearchHistory,
                Setter = v => { PreferencesState.RememberSearchHistory = v;
                                PreferenceWriter.WriteBool(KeyConsts.M_RememberSearchHistory, v); },
                //DependentRowIds = new[] { "search.clear_history" }, // Clear history is always enabled
            });
            rows.Add(new ActionRow
            {
                Id = "search.clear_history",
                ParentId = "search.remember_history",
                TitleRes = Resource.String.clear_search_history,
                ButtonTextRes = Resource.String.clear_search_history,
                Destructive = true,
                DisableIfParentDisabled = false,
                OnClick = (h, r) => h.ClearSearchHistory(),
            });

            // ============================== GENERAL ==============================
            rows.Add(new HeaderRow { Id = "h.general", TitleRes = Resource.String.section_general });

            rows.Add(new ToggleRow
            {
                Id = "general.start_service",
                TitleRes = Resource.String.start_service_on_startup,
                SubtitleRes = Resource.String.keep_alive_service_subtitle,
                IconRes = Resource.Drawable.power_settings_new_28dp,
                Getter = () => PreferencesState.StartServiceOnStartup,
                Setter = v => { PreferencesState.StartServiceOnStartup = v;
                                PreferencesManager.SaveStartServiceOnStartup(); },
                DependentRowIds = new[] { "general.startstop_service" },
            });
            rows.Add(new ActionRow
            {
                Id = "general.startstop_service",
                ParentId = "general.start_service",
                TitleRes = Resource.String.start_service_on_startup,
                ButtonTextRes = Resource.String.start_service_on_startup,
                ButtonTextProvider = ctx => ctx.GetString(
                     Seeker.Services.ServiceLifecycle.IsStartUpServiceCurrentlyRunning
                         ? Resource.String.stop_service
                         : Resource.String.start_service),
                OnClick = (h, r) => { h.StartStopBackgroundService(); h.NotifyRowChanged(r.Id); },
            });

            rows.Add(new ToggleRow
            {
                Id = "general.allow_private_invites",
                TitleRes = Resource.String.allow_private_room_invitations,
                IconRes = Resource.Drawable.mail_28dp,
                Getter = () => PreferencesState.AllowPrivateRoomInvitations,
                // PreferencesState is updated by ReconfigureOptionsLogic on server success; on
                // failure or not-logged-in, ReconfigureOptions fires NotifyRowChanged to revert
                // the optimistic switch flip done by ToggleViewHolder on click.
                Setter = v => Seeker.Services.SessionService.Instance.ReconfigureOptions(v, null, null),
            });

            rows.Add(new ToggleRow
            {
                Id = "general.notif_folder_complete",
                TitleRes = Resource.String.show_notifs_on_folder_complete,
                KeywordsRes = Resource.String.keywords_notif_folder_complete,
                IconRes = Resource.Drawable.notifications_outline_30dp,
                Getter = () => PreferencesState.NotifyOnFolderCompleted,
                Setter = v => { PreferencesState.NotifyOnFolderCompleted = v;
                                PreferencesManager.SaveNotifyOnFolderCompleted(); },
            });

            rows.Add(new ToggleRow
            {
                Id = "general.notif_toast",
                TitleRes = Resource.String.show_bottom_notifs_complete,
                KeywordsRes = Resource.String.keywords_notif_toast,
                IconRes = Resource.Drawable.notifications_outline_30dp,
                // Original checkbox represents "show toast" → !DisableToastNotification.
                Getter = () => !PreferencesState.DisableDownloadToastNotification,
                Setter = v => { PreferencesState.DisableDownloadToastNotification = !v;
                                PreferenceWriter.WriteBool(KeyConsts.M_DisableToastNotifications, PreferencesState.DisableDownloadToastNotification); },
            });

            rows.Add(new ToggleRow
            {
                Id = "general.remember_recent_users",
                TitleRes = Resource.String.RememberRecent,
                KeywordsRes = Resource.String.keywords_remember_recent_users,
                IconRes = Resource.Drawable.history_28dp,
                Getter = () => PreferencesState.ShowRecentUsers,
                Setter = v => { PreferencesState.ShowRecentUsers = v;
                                PreferenceWriter.WriteBool(KeyConsts.M_RememberUserHistory, v); },
                //DependentRowIds = new[] { "general.clear_recent_users" },
            });
            rows.Add(new ActionRow
            {
                Id = "general.clear_recent_users",
                ParentId = "general.remember_recent_users",
                TitleRes = Resource.String.ClearRecentUsers,
                ButtonTextRes = Resource.String.ClearRecentUsers,
                Destructive = true,
                DisableIfParentDisabled = false,
                OnClick = (h, r) => h.ClearRecentUsers(),
            });

            rows.Add(new ToggleRow
            {
                Id = "general.auto_away",
                TitleRes = Resource.String.AwayOnInactivity,
                KeywordsRes = Resource.String.keywords_auto_away,
                IconRes = Resource.Drawable.schedule_clock_28dp,
                Getter = () => PreferencesState.AutoAwayOnInactivity,
                Setter = v => { PreferencesState.AutoAwayOnInactivity = v;
                                PreferencesManager.SaveAutoAwayOnInactivity(); },
            });

            // ============================== APPEARANCE ==============================
            rows.Add(new HeaderRow { Id = "h.appearance", TitleRes = Resource.String.section_appearance });

            rows.Add(new ValueRow
            {
                Id = "appearance.language",
                TitleRes = Resource.String.SetLanguage,
                IconRes = Resource.Drawable.language_28dp,
                ValueProvider = ctx => SettingValueFormat.LanguageLabel(ctx),
                OnClick = (h, r) => SettingPickers.PickLanguage(h, r),
            });

            rows.Add(new ValueRow
            {
                Id = "appearance.day_night",
                TitleRes = Resource.String.night_mode_options,
                KeywordsRes = Resource.String.keywords_night_mode,
                IconRes = Resource.Drawable.dark_mode_28dp,
                ValueProvider = ctx => SettingValueFormat.DayNightModeLabel(ctx, PreferencesState.DayNightMode),
                OnClick = (h, r) => SettingPickers.PickDayNightMode(h, r),
            });

            rows.Add(new ValueRow
            {
                Id = "appearance.day_variant",
                TitleRes = Resource.String.DayVarient_,
                KeywordsRes = Resource.String.keywords_day_variant,
                IconRes = Resource.Drawable.palette_28dp,
                ValueProvider = ctx => SettingValueFormat.DayVariantLabel(ctx, PreferencesState.DayModeVariant),
                OnClick = (h, r) => SettingPickers.PickDayVariant(h, r),
            });

            rows.Add(new ValueRow
            {
                Id = "appearance.night_variant",
                TitleRes = Resource.String.NightVarient_,
                KeywordsRes = Resource.String.keywords_night_variant,
                IconRes = Resource.Drawable.palette_28dp,
                ValueProvider = ctx => SettingValueFormat.NightVariantLabel(ctx, PreferencesState.NightModeVariant),
                OnClick = (h, r) => SettingPickers.PickNightVariant(h, r),
            });

            // ============================== SHARING ==============================
            rows.Add(new HeaderRow { Id = "h.sharing", TitleRes = Resource.String.section_sharing });

            rows.Add(new ToggleRow
            {
                Id = "sharing.enable",
                TitleRes = Resource.String.enable_sharing,
                KeywordsRes = Resource.String.keywords_enable_sharing,
                IconRes = Resource.Drawable.folder_shared_browse_outline_30dp,
                Getter = () => PreferencesState.SharingOn,
                Setter = v => { PreferencesState.SharingOn = v;
                                PreferenceWriter.WriteBool(KeyConsts.M_SharingOn, v); },
                DependentRowIds = new[] { "sharing.add_folder", "sharing.rescan_browse" },
                StatusProvider = () =>
                {
                    var ctx = host.Activity;
                    if (!PreferencesState.SharingOn)
                    {
                        return default; // None
                    }
                    // TODO refactor so this drives it. right now its a mix.
                    var (sharingState, statusText) = Seeker.Services.SharingService.GetSharingMessageAndIcon(out bool isParsing);
                    if (isParsing)
                    {
                        return new SettingStatus { Kind = SettingStatusKind.Running,
                            Text = SettingValueFormat.ParsingStatusText(ctx) };
                    }
                    if (UploadDirectoryManager.UploadDirectories.Count == 0)
                    {
                        return new SettingStatus { Kind = SettingStatusKind.Failure,
                            Text = ctx.GetString(Resource.String.NoSharedAdd) };
                    }
                    bool allFailed = UploadDirectoryManager.UploadDirectories.Count > 0
                        && UploadDirectoryManager.AreAllFailed();
                    if (Seeker.Services.SharedFileService.ParseStatus.FailedShareParse || allFailed)
                    {
                        return new SettingStatus { Kind = SettingStatusKind.Failure,
                            Text = ctx.GetString(Resource.String.sharing_disabled_failure_parsing) };
                    }
                    if (sharingState == SharingIcons.OffDueToNetwork)
                    {
                        return new SettingStatus { Kind = SettingStatusKind.Failure,
                            Text = statusText };
                    }
                    return new SettingStatus { Kind = SettingStatusKind.Success,
                        Text = SettingValueFormat.SharedAggregateSummary(ctx) };
                },
                AddStatusListener = h => Seeker.Services.SharedFileService.SharingStatusChangedEvent += h,
                RemoveStatusListener = h => Seeker.Services.SharedFileService.SharingStatusChangedEvent -= h,
            });

            // One row per shared folder. Rebuilt via RefreshSharingFolders().
            if (UploadDirectoryManager.UploadDirectories != null)
            {
                foreach (var entry in UploadDirectoryManager.UploadDirectories)
                {
                    if (entry.IsSubdir)
                    {
                        continue;
                    }
                    var captured = entry;
                    rows.Add(new SharedFolderRow
                    {
                        Id = "sharing.folder." + captured.Info.UploadDataDirectoryUri,
                        ParentId = "sharing.enable",
                        KeywordsRes = Resource.String.keywords_manage_shared,
                        TitleProvider = c => captured.GetLastPathSegment(),
                        Entry = captured,
                        OnEdit = (h, r) => h.EditSharedFolder(r.Entry),
                        OnRemove = (h, r) => h.RemoveSharedFolder(r.Entry),
                        Indent = false,
                    });
                }
            }

            rows.Add(new ButtonRow
            {
                Id = "sharing.add_folder",
                ParentId = "sharing.enable",
                TitleRes = Resource.String.add_folder,
                IconRes = Resource.Drawable.ic_add_black_24dp,
                OnClick = (h, r) => h.LaunchAddSharedFolderPicker(),
                FullWidth = true,
                Indent = false,
                VisiblePredicate = () => PreferencesState.SharingOn,
            });

            rows.Add(new ButtonPairRow
            {
                Id = "sharing.rescan_browse",
                ParentId = "sharing.enable",
                LeftTextRes = Resource.String.RescanShares,
                LeftOnClick = (h, r) => h.RescanShares(),
                RightTextRes = Resource.String.browse_yourself,
                RightOnClick = (h, r) => h.BrowseSelf(),
                Indent = false,
                VisiblePredicate = () => PreferencesState.SharingOn && UploadDirectoryManager.UploadDirectories.Count != 0,
            });

            rows.Add(new ToggleRow
            {
                Id = "sharing.metered",
                TitleRes = Resource.String.PauseSharingIfMetered,
                IconRes = Resource.Drawable.android_cell_4_bar_off_28dp,
                Getter = () => !PreferencesState.AllowUploadsOnMetered,
                Setter = v => { PreferencesState.AllowUploadsOnMetered = !v;
                                PreferencesManager.SaveAllowUploadsOnMetered(); },
            });

            rows.Add(new ToggleRow
            {
                Id = "sharing.auto_clear_uploads",
                TitleRes = Resource.String.auto_clear_complete_uploads,
                KeywordsRes = Resource.String.keywords_auto_clear_uploads,
                IconRes = Resource.Drawable.clear_all_28dp,
                Getter = () => PreferencesState.AutoClearCompleteUploads,
                Setter = v => { PreferencesState.AutoClearCompleteUploads = v;
                                PreferenceWriter.WriteBool(KeyConsts.M_AutoClearCompleteUploads, v); },
            });

            // ============================== NETWORK ==============================
            rows.Add(new HeaderRow { Id = "h.network", TitleRes = Resource.String.section_network });

            rows.Add(new ToggleRow
            {
                Id = "network.listening",
                TitleRes = Resource.String.enable_listening,
                SubtitleRes = Resource.String.setting_subtitle_listening,
                IconRes = Resource.Drawable.lan_connect,
                Getter = () => PreferencesState.ListenerEnabled,
                Setter = v => {
                    PreferencesState.ListenerEnabled = v;
                    PreferencesManager.SaveListeningState();
                    host.Adapter?.NotifyParentToggled("network.listening");
                    Seeker.Services.SessionService.Instance.ReconfigureOptions(null, v, null);
                    if (v)
                    {
                        UPnpManager.Instance.Feedback = true;
                        UPnpManager.Instance.SearchAndSetMappingIfRequired();
                    }
                },
                DependentRowIds = new[] { "network.port", "network.port_check", "network.upnp" },
            });

            rows.Add(new ValueRow
            {
                Id = "network.port",
                ParentId = "network.listening",
                TitleRes = Resource.String.change_port,
                KeywordsRes = Resource.String.keywords_port,
                ValueProvider = ctx => PreferencesState.ListenerPort.ToString(),
                OnClick = (h, r) => NumericInputBottomSheet.Show(h, r, 1024, 65535,
                    () => PreferencesState.ListenerPort,
                    v => {
                        bool changed = PreferencesState.ListenerPort != v;
                        PreferencesState.ListenerPort = v;
                        PreferencesManager.SaveListeningState();
                        if (changed)
                        {
                            Seeker.Services.SessionService.Instance.ReconfigureOptions(null, null, v);
                            RescanUpnp();
                        }
                    }),
                VisiblePredicate = () => PreferencesState.ListenerEnabled,
            });
            rows.Add(new ActionRow
            {
                Id = "network.port_check",
                ParentId = "network.listening",
                TitleRes = Resource.String.check_your_port_status,
                ButtonTextRes = Resource.String.check_your_port_status,
                OnClick = (h, r) => h.CheckPortStatus(),
                VisiblePredicate = () => PreferencesState.ListenerEnabled,
            });
            rows.Add(new ToggleRow
            {
                Id = "network.upnp",
                ParentId = "network.listening",
                TitleRes = Resource.String.use_pnp_for_port_forwarding,
                KeywordsRes = Resource.String.keywords_upnp,
                Getter = () => PreferencesState.ListenerUPnpEnabled,
                Setter = v => {
                    PreferencesState.ListenerUPnpEnabled = v;
                    PreferencesManager.SaveListeningState();
                    if (v)
                    {
                        // rerun on enable
                        RescanUpnp();
                    }
                },
                StatusProvider = UpnpStatus,
                AddStatusListener = h => {
                    UPnpManager.Instance.SearchStarted += h;
                    UPnpManager.Instance.SearchFinished += h;
                    UPnpManager.Instance.DeviceSuccessfullyMapped += h;
                },
                RemoveStatusListener = h => {
                    UPnpManager.Instance.SearchStarted -= h;
                    UPnpManager.Instance.SearchFinished -= h;
                    UPnpManager.Instance.DeviceSuccessfullyMapped -= h;
                },
                VisiblePredicate = () => PreferencesState.ListenerEnabled,
            });

            rows.Add(new ValueRow
            {
                Id = "network.download_speed",
                TitleRes = Resource.String.LimitDownloadSpeed,
                KeywordsRes = Resource.String.keywords_download_speed,
                IconRes = Resource.Drawable.speed_28dp,
                ValueProvider = ctx => SettingValueFormat.SpeedLimitSummary(ctx,
                    PreferencesState.SpeedLimitDownloadOn,
                    PreferencesState.SpeedLimitDownloadBytesSec,
                    PreferencesState.SpeedLimitDownloadIsPerTransfer),
                OnClick = (h, r) => SpeedLimitBottomSheet.Show(h, r,
                    () => (PreferencesState.SpeedLimitDownloadOn,
                           PreferencesState.SpeedLimitDownloadBytesSec,
                           PreferencesState.SpeedLimitDownloadIsPerTransfer),
                    result => {
                        PreferencesState.SpeedLimitDownloadOn = result.Enabled;
                        PreferencesState.SpeedLimitDownloadBytesSec = result.BytesPerSec;
                        PreferencesState.SpeedLimitDownloadIsPerTransfer = result.PerTransfer;
                        PreferencesManager.SaveSpeedLimitState();
                    }),
            });

            rows.Add(new ValueRow
            {
                Id = "network.upload_speed",
                TitleRes = Resource.String.LimitUploadSpeed,
                KeywordsRes = Resource.String.keywords_upload_speed,
                IconRes = Resource.Drawable.speed_28dp,
                ValueProvider = ctx => SettingValueFormat.SpeedLimitSummary(ctx,
                    PreferencesState.SpeedLimitUploadOn,
                    PreferencesState.SpeedLimitUploadBytesSec,
                    PreferencesState.SpeedLimitUploadIsPerTransfer),
                OnClick = (h, r) => SpeedLimitBottomSheet.Show(h, r,
                    () => (PreferencesState.SpeedLimitUploadOn,
                           PreferencesState.SpeedLimitUploadBytesSec,
                           PreferencesState.SpeedLimitUploadIsPerTransfer),
                    result => {
                        PreferencesState.SpeedLimitUploadOn = result.Enabled;
                        PreferencesState.SpeedLimitUploadBytesSec = result.BytesPerSec;
                        PreferencesState.SpeedLimitUploadIsPerTransfer = result.PerTransfer;
                        PreferencesManager.SaveSpeedLimitState();
                    }),
            });

            rows.Add(new ValueRow
            {
                Id = "network.concurrent_downloads",
                TitleRes = Resource.String.LimitConcurrentDownloads,
                KeywordsRes = Resource.String.keywords_max_concurrent,
                IconRes = Resource.Drawable.download_material,
                ValueProvider = ctx => SettingValueFormat.ConcurrentLimitSummary(ctx,
                    PreferencesState.LimitSimultaneousDownloads,
                    PreferencesState.MaxSimultaneousLimit),
                OnClick = (h, r) => EnableNumericBottomSheet.Show(h, r, 1, 99,
                    () => (PreferencesState.LimitSimultaneousDownloads,
                           PreferencesState.MaxSimultaneousLimit),
                    result => {
                        bool changed = PreferencesState.LimitSimultaneousDownloads != result.Enabled
                                    || (result.Enabled && PreferencesState.MaxSimultaneousLimit != result.Value);
                        PreferencesState.LimitSimultaneousDownloads = result.Enabled;
                        PreferencesState.MaxSimultaneousLimit = result.Value;
                        PreferencesManager.SaveMaxConcurrentDownloadsSettings(result.Enabled, result.Value);
                        if (changed)
                        {
                            SeekerApplication.Toaster.ShowToastShort(
                                host.Activity.GetString(Resource.String.takes_effect_on_next_startup));
                        }
                    }),
            });

            // ============================== ACCOUNT ==============================
            rows.Add(new HeaderRow { Id = "h.account", TitleRes = Resource.String.section_account });

            rows.Add(new ValueRow
            {
                Id = "account.privileges",
                TitleRes = Resource.String.Privileges,
                KeywordsRes = Resource.String.keywords_privileges,
                IconRes = Resource.Drawable.account_star,
                OnClick = (h, r) => PrivilegesBottomSheet.Show(h),
                StatusProvider = () =>
                {
                    var statusKind = SettingStatusKind.HideDot;
                    if (Seeker.Managers.PrivilegesManager.Instance.IsCheckInProgress)
                    {
                        statusKind = SettingStatusKind.Running;
                    }
                    else if (Seeker.Managers.PrivilegesManager.Instance.IsPrivileged)
                    {
                        statusKind = SettingStatusKind.Success;
                    }
                    return new SettingStatus
                    {
                        Kind = statusKind,
                        Text = Seeker.Managers.PrivilegesManager.Instance.IsCheckInProgress ? 
                            host.Activity.GetString(Resource.String.checking_priv_) : SettingValueFormat.PrivilegesSummary(host.Activity)
                    };
                },
                AddStatusListener = h2 => Seeker.Managers.PrivilegesManager.Instance.CheckStateChanged += h2,
                RemoveStatusListener = h2 => Seeker.Managers.PrivilegesManager.Instance.CheckStateChanged -= h2,
            });

            rows.Add(new ToggleRow
            {
                Id = "account.diagnostics",
                TitleRes = Resource.String.EnableDiagnostics,
                KeywordsRes = Resource.String.keywords_diagnostics,
                IconRes = Resource.Drawable.frame_bug_28dp,
                MoreInfoTextRes = Resource.String.diagnostics_more_info,
                Getter = () => PreferencesState.LogDiagnostics,
                Setter = v => { PreferencesState.LogDiagnostics = v;
                                PreferencesManager.SaveLogDiagnostics(); },
            });

            rows.Add(new NavigationRow
            {
                Id = "account.change_password",
                TitleRes = Resource.String.change_password,
                IconRes = Resource.Drawable.lock_28dp,
                OnClick = (h, r) => h.LaunchChangePassword(),
            });

            rows.Add(new NavigationRow
            {
                Id = "account.edit_user_info",
                TitleRes = Resource.String.edit_your_user_info,
                KeywordsRes = Resource.String.keywords_edit_user_info,
                IconRes = Resource.Drawable.user_edit,
                OnClick = (h, r) => h.LaunchEditUserInfo(),
            });

            rows.Add(new NavigationRow
            {
                Id = "account.import_data",
                TitleRes = Resource.String.ImportExternalClientData,
                SubtitleRes = Resource.String.setting_subtitle_import_client_data,
                IconRes = Resource.Drawable.download_material,
                OnClick = (h, r) => h.LaunchImportClientData(),
            });

            rows.Add(new NavigationRow
            {
                Id = "account.export_data",
                TitleRes = Resource.String.ExportClientData,
                SubtitleRes = Resource.String.setting_subtitle_export_client_data,
                IconRes = Resource.Drawable.upload_material,
                OnClick = (h, r) => h.LaunchExportClientData(),
            });

            rows.Add(new ButtonRow
            {
                Id = "account.force_filesystem",
                TitleRes = Resource.String.ForceFilesystemPermission,
                MoreInfoTextRes = Resource.String.restore_defaults_subtitle,
                KeywordsRes = Resource.String.keywords_force_filesystem,
                IconRes = Resource.Drawable.folder_outline,
                MoreInfoOnClick = (h, r) => ForceFileSystemMoreInfoSheet.Show(h),
#if !IzzySoft
                Enabled = false,
#endif
                OnClick = (h, r) => h.LaunchForceFilesystemPermission(),
            });

            rows.Add(new ButtonRow
            {
                Id = "account.restore_defaults",
                TitleRes = Resource.String.restore_default_settings,
                Destructive = true,
                KeywordsRes = Resource.String.keywords_restore_defaults,
                IconRes = Resource.Drawable.settings_backup_restore_28dp,
                OnClick = (h, r) => h.LaunchRestoreDefaults(),
            });

            return rows;
        }
    }
}
