# Seeker UI Redesign — Phases 1–6A + final integration

| Phase | Scope | Status |
| --- | --- | --- |
| 1 | Architecture inventory | **complete** (this document) |
| 2A | Centralised design system + theme behaviour | **complete** (§4, §15) |
| 2B | Navigation shell + real Messages page | **complete** (§16) |
| 3 | Search redesign slice | **complete** (§17) |
| 4 | Transfers + remote browsing redesign | **complete** (§18) |
| 5 | Messaging, chatrooms, settings, login/users, remaining surfaces | **complete** (§19) |
| 6A | Search functional gaps: filename/size sort, cross-result selection, font-scale polish | **implemented** (§20) |
| Final integration/polish | Search mode lifecycle, large-font row/input limits, contextual download action, Maestro regression matrix | **implemented / static+build verified** (§21; runtime unverified — see `docs/UI_REDESIGN_DELIVERY.md`) |
| Colour semantics + automation validity | chrome attrs no longer used as accents/progress, resolved WCAG contrast contracts, Maestro command schema + external-precondition runner | **implemented / static+build verified** (§22; runtime unverified) |
| 6B…12 | Remaining search polish → regression | pending (§12) |

Acceptance gates used so far:

| Phase | Gates |
| --- | --- |
| 1 | `git diff --check` + static inventories |
| 2A | `python3 Misc/test_ui_contracts.py`, `git diff --check`, `dotnet build Seeker/Seeker.csproj -c "Debug Mock" -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true` (all green, see §15) |
| 2B | Same static + build gates (129 assertions, 0 errors) plus updated Maestro shell flow; no device run (see §16) |

Phase 1 was inventory only (no source/layout/resource/build change). Phase 2A changes the
resource/token layer, the theme resolution helpers and the preference default; it does not
restructure any screen (see §15 "Explicitly not in 2A").

Repository facts used throughout (Phase 1 baseline):

| Metric | Value |
| --- | --- |
| Layout XML files | 106 (`Seeker/Resources/layout/`) |
| Option menus | 21 (`Seeker/Resources/menu/`) |
| Vector/shape drawables | ~190 (`Seeker/Resources/drawable/`) + 4 `drawable-night/` |
| Custom theme attributes | 56 legacy + 40 design-system tokens (`Seeker/Resources/values/attrs.xml`) |
| Strings (English source) | 805 (`Seeker/Resources/values/strings.xml`) |
| Translation dirs | 21 (`values-<lang>/`) |
| Activities | 12 (incl. 1 dummy) |
| Fragments | 11 |
| Dialog/sheet classes | 20+ |
| C# files in `Seeker/` | 689 |

---

## 1. Build / test tooling available in this environment

**Installed**

- `git` (repo on `master`, clean before Phase 1)
- Python 3.14.6, `xmllint` (usable for XML well-formedness checks)
- OpenJDK 21.0.8, Gradle 8.7 (sdkman) — irrelevant to the real build, which is MSBuild-driven
- Android SDK at `$ANDROID_HOME=/home/void/Android/Sdk`: `platforms/android-35,36.0,37.0,37.1`,
  `build-tools/35.0.0,36.0.0,37.0.0`, `platform-tools/adb`, `ndk`, `cmake`
- `adb` present, **no device/emulator attached** (`adb devices` empty)
- **.NET SDK 10.0.401 + `android` workload 36.1.69**, installed during Phase 2A with
  `dotnet-install.sh --version 10.0.401 --install-dir ~/.dotnet` followed by
  `dotnet workload install android` (uses the existing Android SDK above; nothing in the
  project or `global.json` was changed). Use `export PATH="$HOME/.dotnet:$PATH"`.
  Consequences now verified locally: `AndroidKeyStore=false` `Debug Mock` build succeeds
  and produces a signed APK, so resource/aapt2 and C# regressions are caught here.

**Still missing**

- No device/emulator, therefore no `-t:Install`/`-t:Run`, no `InstrumentationTests`, and no
  Maestro run (Maestro CLI is not installed either).

**What the project expects (for the next phase, executed on a capable machine)**

```bash
# app build / deploy (DEVELOPMENT.md)
dotnet build ./Seeker/Seeker.csproj -p:AndroidKeyStore=false -c "Debug Mock" -t:Install
dotnet build ./Seeker/Seeker.csproj -p:AndroidKeyStore=false -c "Debug Mock" -t:Run
dotnet publish ./Seeker/Seeker.csproj -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true -c "Debug Mock" -f net10.0-android36.0

# unit tests (CI: .github/workflows/unittests.yml)
dotnet test UnitTestCommon/UnitTestCommon.csproj -c Release --filter TestCategory!=RealData

# instrumentation tests (on device)
adb shell am instrument -w com.seeker.instrumentationtests/instrumentationtests.TestInstrumentation
```

Pinned toolchain: `global.json` → SDK `10.0.401` (`rollForward: latestPatch`),
workloadVersion `10.0.401`; `Seeker.csproj` → `net10.0-android36.0`,
`SupportedOSPlatformVersion 23`; configurations `Debug`, `Debug Mock`, `Release`,
`Release Mock`, `Release IzzySoft`.

Implication: later phases must plan for an environment with the .NET 10 Android workload plus a
device/emulator. Static checks (`python3 Misc/test_ui_contracts.py`, `git diff --check`,
`xmllint`) and a real `dotnet build` are available here; runtime/visual verification is not.

---

## 2. Stack and application structure

- .NET Android (Xamarin.Android-style) app. **No MAUI, no Jetpack Compose, no Jetpack
  Navigation, no data binding, no ViewModel/LiveData architecture.**
- Views: hand-written XML layouts (106) + heavily programmatic custom `View` subclasses;
  AndroidX `AppCompat`, `Fragment`, `RecyclerView`, `ViewPager2`, `ViewPager2.FragmentStateAdapter`,
  `SwipeRefreshLayout`, `DocumentFile`, Material Components M3
  (`Xamarin.Google.Android.Material 1.13.0.1`).
- Projects (`Seeker.sln`): `Seeker` (Android app), `Common` (netstandard2.1 — models, state,
  serialization, services, UI-ish state), `Soulseek` (vendored fork under
  `Seeker/Soulseek.NET-master/src`), `Mono.Nat` (vendored), `UnitTestCommon` (net10.0),
  `InstrumentationTests` (net10.0-android36.0).
- Entry point / composition root: `Seeker/SeekerApplication.cs` (`[Application]`). Wires
  services statically in `OnCreate`, starts `SeekerKeepAliveService`, configures sharing,
  registers network callbacks, initializes `MessageController`, `ChatroomController`,
  `WishlistController`, `ServerMessageService`.
- Base activities: `Seeker/Main/ThemeableActivity.cs` (theme + locale + edge-to-edge),
  `Seeker/Main/SlskLinkMenuActivity.cs` (adds `slsk://` link context-menu plumbing for
  Messages/Chatroom activities).

---

## 3. Navigation and app shell

**Main shell** — `Seeker/Main/MainActivity.cs` (1265 lines) +
`Seeker/Main/MainActivityHelpers.cs`, layout `layout/activity_main.xml`:

- `AppBarLayout` + `Toolbar` (custom action-bar view `layout/custom_menu_layout.xml`
  containing `search_tabs` icon/badge + `SafeMaterialAutoCompleteTextView` `searchHere`),
  `ViewPager2` (`pager`), `BottomNavigationView` (`navigation`), menu `menu/navigation_menu.xml`.
- Pager: `Seeker/Main/TabsPagerAdapter.cs` (`FragmentStateAdapter`, `ItemCount = 5`,
  `OffscreenPageLimit = 4` so all five pages stay resident):
  `0 LoginFragment` (account, hidden from the bar), `1 SearchFragment`, `2 TransfersFragment`,
  `3 BrowseFragment`, `4 MessagesOverviewFragment`.
- Bottom nav item IDs: `navigation_search`, `navigation_transfers`, `navigation_browse`,
  `navigation_messages` (also tapped by Maestro flows). `navigation_home` no longer exists;
  Account/login lives in the secondary action sheet (`action_shell_menu`).
- Fragments are held via **static `Instance` fields** (`SearchFragment.Instance`,
  `BrowseFragment.Instance`, `SeekerState.LoginFragmentRef`, `SeekerState.MainActivityRef`,
  `SeekerState.ActiveActivityRef`), and menu visibility is gated per page in
  `OnPagerPageSelected` via `SetMenuVisibility`.
- Account/global menu is inflated by `LoginFragment` from `menu/account_menu.xml`
  (users, messages, chatrooms, settings, shutdown, about) and handled in
  `MainActivity.OnOptionsItemSelected`.
- Deep-link/intent routing: extras `DownloadForegroundService.FromTransferString`,
  `FolderAlertExtra`, `GoToAccountExtra`, `GoToBrowseExtra`, `GoToSearchExtra`, plus
  `SETTINGS_INTENT`, `SETTINGS_EXTERNAL`; `SearchDialogDummyActivity`
  (`Seeker/Search/SearchDialog.cs`) is the `SEND`/`VIEW` entry that forwards into a search.
- Back handling: `Helpers/GenericOnBackPressedCallback.cs` +
  `MainActivity.RefreshBackCallbackState()` (browse folder stack, transfer folder selection),
  manifest `android:enableOnBackInvokedCallback="true"`.
- IME: bottom nav is faded/hidden on keyboard show (`MainActivity.KeyboardChanged`,
  `KeyBoardVisibilityChanged`).

**Secondary activities (each owns its own Toolbar + menu; no shared nav graph)**

| Activity | File | Layout | Menus |
| --- | --- | --- | --- |
| `SettingsActivity` (+ `.Host.cs`) | `Settings/SettingsActivity.cs`, `Settings/SettingsActivity.Host.cs` | `settings_layout.xml` | `settings_menu.xml` |
| `ImportWizardActivity` | `Settings/ImportWizardActivity.cs` | `wizard_activity_layout.xml`, `import_start_page.xml`, `import_list_layout.xml`, `import_item_view.xml` | — |
| `MessagesActivity` | `Messages/MessagesActivity.cs` | `messages_main_layout.xml` | via fragments |
| `ChatroomActivity` | `Chatroom/ChatroomActivity.cs` | `chatroom_main_layout.xml` | `chatroom_overview_list_menu.xml` |
| `UserListActivity` | `Users/UserListActivity.cs` | `user_list_activity_layout.xml` | `user_list_menu.xml` |
| `ViewUserInfoActivity` | `Users/ViewUserInfoActivity.cs` | `view_user_info_layout.xml` | `view_user_info_menu.xml` |
| `EditUserInfoActivity` | `Users/EditUserInfoActivity.cs` | `edit_user_info_layout.xml` | `edit_user_info_menu.xml` (+ `messages_overview_list_menu.xml` inflated on toolbar) |
| `SearchDialogDummyActivity` | `Search/SearchDialog.cs` | none (finishes) | — |
| `CloseActivity` | `Main/CloseActivity.cs` | none | — |

Background services (UI-visible through notifications):
`Main/DownloadForegroundService.cs`, `Main/UploadForegroundService.cs`,
`Main/SeekerKeepAliveService.cs`, notification layout `simple_custom_notification.xml`,
helpers `Helpers/AppNotifications.cs`.

---

## 4. Theming system

### 4.1 Shape of the layer (after Phase 2A)

```
values/attrs.xml          56 legacy attrs (unchanged names) + 40 design-system tokens
values/colors.xml         static colour resources (day)          ─┐ name-for-name
values-night/colors.xml   static colour resources (night)        ─┘ identical (checked)
values/styles.xml         Seeker.Theme.Base (light parent) + AppTheme (window, token layer,
                          Material3 role mapping) + shapes/typography/touch targets +
                          shared widget styles + theme-aware overlays
values-night/styles.xml   Seeker.Theme.Base only (DayNight parent) – the single config split
values/themes.xml         12 concrete themes: 10 legacy + ModernLight/ModernDark
drawable/ds_*.xml         reusable token driven selectors/shapes (no colour literals)
```

* **10 legacy concrete themes preserved by preference value**: day `DefaultLight`,
  `DefaultLight_Blue`, `DefaultLight_Red`, `DefaultLight_Grey`; night `DefaultDark`,
  `DefaultDark_Blue`, `DefaultDark_Red`, `DefaultDark_Grey`, `Amoled`, `Amoled_Grey`. Their
  stored enum values and palette attributes are unchanged; the shared token layer fills only
  the window/launch background with a non-recursive Material default (a small visual
  alignment, not a byte-for-byte freeze).
* **2 new themes: `ModernLight` / `ModernDark`** – the Phase 2A default. They declare only
  tokens and map the legacy attributes back onto them (see §15 for the palette).
* `AppTheme` inherits `Seeker.Theme.Base`, which is the *only* style redefined in
  `values-night/`. That is what allows every shared item (tokens, Material role mapping,
  dialogs/sheets/popups/inputs/action mode/snackbar styling, typography, touch targets) to
  be written exactly once. Before 2A the night copy of `AppTheme`/`AppTheme.NoActionBar` had
  silently drifted (it was missing `colorSecondaryContainer`, `snackbarButtonStyle`,
  `actionModeStyle`; `MyActionMode`/`MyActionModeTitle` were defined twice in the same
  configuration; `cellStroke` was declared twice in `attrs.xml`).

### 4.2 Token layer

`AppTheme` defines ~40 semantic tokens:

| Group | Tokens |
| --- | --- |
| Surfaces | `dsColorBackground`, `dsColorSurface`, `dsColorSurfaceContainer`, `dsColorSurfaceElevated` |
| Brand | `dsColorPrimary`, `dsColorOnPrimary`, `dsColorPrimaryContainer`, `dsColorOnPrimaryContainer`, `dsColorAccent`, `dsColorOnAccent` |
| Text | `dsColorTextPrimary`, `dsColorTextSecondary`, `dsColorTextDisabled` |
| Lines | `dsColorOutline`, `dsColorOutlineVariant`, `dsColorDivider` |
| Semantics | `dsColorError`, `dsColorOnError`, `dsColorSuccess`, `dsColorWarning`, `dsColorScrim`, `dsColorControlHighlight` |
| Inputs | `dsColorInputBackground`, `dsColorInputStroke` |
| Chrome | `dsColorChromeSurface/Foreground`, `dsColorActionModeBackground/Foreground`, `dsColorNavigation{,Background,Foreground,Muted,Indicator}`, `dsColorLaunchBackground`, `dsColorSystemBar` |
| Chips / snackbar | `dsColorChipBackground`, `dsColorChipText`, `dsColorSnackbarAction` |
| Shapes | `dsShapeCard`, `dsShapeSheet`, `dsShapeInput`, `dsShapeButton` (references) |

All Material3 roles are derived from them in `AppTheme` (`colorPrimary*`, `colorSecondary*`,
`colorSurface*`, `colorOutline*`, `colorError*`, `android:colorBackground`, `colorOnBackground`,
`android:textColorPrimary/Secondary/Tertiary/Hint`, `colorControlActivated/Normal/Highlight`),
plus `materialAlertDialogTheme`, `bottomSheetDialogTheme`, `popupMenuStyle`,
`textInputStyle`, `materialButtonStyle`, `actionModeStyle`. Legacy attributes map onto the
same tokens, so a theme change reaches the ~106 existing layouts without touching them.

Defaults keep the legacy palette: in `AppTheme` most tokens point at the legacy attribute they
came from (`dsColorSurface = ?attr/cellback`, `dsColorPrimary = ?attr/mainPurple`, …), and the
legacy attributes resolve as before. The window background is the exception: `dsColorBackground`
uses the non-recursive `@color/ds_default_background` because `android:colorBackground` is
derived from it - pointing it at `?android:attr/colorBackground` would be a theme-attribute
cycle for the legacy light themes, which do not override that token. `DefaultDark`/`Amoled`
additionally set `dsColorBackground`/`dsColorLaunchBackground` to their window background so
the launch window no longer flashes the Material default before the first frame.

**Typography / spacing / touch targets:** `TextAppearance.Seeker.{Headline,Title,Body,
BodySecondary,Label,Caption,ActionModeTitle}` (token colours, fixed sp scale) and dimens
`ds_space_{xxs,xs,sm,md,lg,xl,xxl}`, `ds_corner_{sm,md,lg,xl}`, `ds_stroke_width`,
`ds_touch_target_min = 48dp`, `ds_touch_target_comfortable = 56dp`, `ds_icon_{sm,md,lg}`.
`values-night/dimens.xml` was removed (all dimens are configuration independent).

**Reusable token drawables:** `drawable/ds_cell_selector.xml`, `ds_input_background.xml`,
`ds_chat_input_background.xml`, `ds_rounded_corner_background.xml`,
`ds_rounded_corner_dialog_background.xml` – these replace the per-theme
`CustomSelector_<hex>.xml` / `rounded_corner_*_{light,dark,amoled}` family for the token
themes (the legacy files stay for the legacy themes).

### 4.3 Theme-aware overlays (no more forced light/dark)

`AppTheme.AppBarOverlay` used to be `ThemeOverlay.Material3.Dark.ActionBar` in `values/` and
`ThemeOverlay.MaterialComponents.Dark.ActionBar` in `values-night/` (i.e. forced dark in both,
giving white-on-**light** chrome for any light variant); `AppTheme.PopupOverlay` was forced
Light in `values/` and Dark in `values-night/`. All three overlays (`AppTheme.AppBarOverlay`,
`AppTheme.PopupOverlay`, `AppPopupOverlay`) are now single definitions with a neutral
`ThemeOverlay.Material3` parent whose visible colours (`colorOnSurface`, `colorOnSurfaceVariant`,
`android:textColorPrimary/Secondary`, `colorControlNormal`, `actionMenuTextColor`) come from
`dsColorChromeForeground` / `dsColorTextPrimary` / `dsColorTextSecondary`. The bottom
navigation item tint (previously literal `#ffffffff` in `activity_main.xml`) now uses
`?attr/dsColorNavigationForeground`, so light chrome gets a dark foreground.

### 4.4 Theme selection / persisted state

* `Common/ThemeEnums.cs`: `DayThemeType {ClassicPurple=0, Red=1, Blue=2, Grey=3, Modern=4}`,
  `NightThemeType {ClassicPurple=0, Grey=1, Blue=2, Red=3, AmoledClassicPurple=4, AmoledGrey=5,
  Modern=6}`. Values are persisted as ints, so new variants are **appended only** – a stored
  0/1/2/3/4/5 keeps meaning the legacy theme it always meant.
* `Seeker/Helpers/ThemeHelper.cs`: enum → `Resource.Style`, total (no `throw` for unknown
  values), `NormalizeDayTheme`/`NormalizeNightTheme` clamp corrupted stored ints to `Modern`,
  `IsNightMode(Context)` (effective/app configuration) and `GetActivityTheme(Context)`.
* `Common/PreferencesState.cs` defaults `DayNightMode = -1` (follow system) and both variants
to `Modern`; `PreferencesManager.RestoreUIPreferences` uses the same defaults and keeps the
existing keys (`Momento_DayNightMode`, `Momento_DayModeVarient`, `Momento_NightModeVarient`).
* `UiHelpers.SetActivityTheme(Activity)` (called from `ThemeableActivity.OnCreate`) now
delegates to `ThemeHelper.GetActivityTheme`; `UiHelpers.ApplySystemBarIconContrast` sets the
status/navigation bar icon contrast from the theme (`windowLightStatusBar` per concrete theme);
`UiHelpers.ApplyThemeChange` = re-apply + recreate activities only.
* Theme switching (`SettingPickers.PickDayVariant/PickNightVariant`, the system/light/dark
  picker) only persists the preference, re-applies the theme and recreates activities. It never
  touches `SoulseekClient`, `SessionService`, the foreground services or the transfer engine,
  and `ThemeableActivity.OnDestroy` skips the "last activity" `TransferPersistenceWrapper.SaveTransferItems()`
  for `IsChangingConfigurations`, so a theme switch is not treated as going to background.

### 4.5 Known theming weak points still open

* 96 vector/shape drawables still contain hard-coded colours for legacy themes. No *layout*
  contains a colour literal any more (`Misc/test_ui_contracts.py` -> `chrome.literals`), but the
  drawable set was only replaced where it is shared chrome (`ds_*` primitives, `rounded_button`,
  the cell/input/chat/sheet backgrounds, the cell selector).
* `res/color/segmented_button_bg.xml` is now dead (unreferenced); the search filter tint is built
  at runtime from tokens in `SearchFragment.GetSegmentedButton{Bg,Text}Tint`. Likewise
  `drawable/rounded_bottom_{left,right}_purple.xml` (unreferenced chrome-filled shapes) and the
  `@color/mainPurple{Transparent,TurnedOff}` literals. They are leftovers, not rendering bugs:
  nothing inflates them. Deleting unused resources is deferred to a resource-cleanup pass.
* Dynamic colour / Material You is not supported (fixed palette per theme on purpose).

---

## 5. Screen inventory

### 5.1 Account/login page (pager page 0, secondary navigation)

- `Seeker/Login/LoginFragment.cs` (603); layout `login_viewflipper.xml`.
- `ViewFlipper` children: 0 login form (`etUsername`, `etPassword`, `buttonLogin`,
  `noAccount`), 1 connecting (`loggingInLoadingCircle`, `connectingToServer`, `pleaseWait`),
  2 logged-in dashboard (`userNameView`, `connectionStatusDot/Text/Chip`, `serverMessageCard`
  with dismiss/text/time, menu rows `menuSetUpSharing`, `menuManageUserList`,
  `menuMessages` + unread badge, `menuSettings`, `menuLogout`, `mustSelectDirectory`).
- Menu: `account_menu.xml` (also drives MainActivity navigation to other activities).
- Interactions: login/logout, connection-state chip, server-message dismiss, unread-badge,
  navigation to Settings/Users/Messages/Chatrooms.
- Coupling: `SoulseekClient.StateChanged`, `SessionService.LoginStarted/LoginCompleted`,
  `MessageController.MessageReceived`, `MessagesBroadcastReceiver.MarkAsReadFromNotification`,
  `ServerMessageService.Changed`; writes `SeekerState.LoginFragmentRef`.

### 5.2 Search tab (`navigation_search`)

- `Seeker/Search/SearchFragment.cs` (2503; partial classes in
  `SearchBottomSheet.cs`, `SearchItemViewsUnified.cs`, others); layout `searches.xml`.
- Structure: `appBar` + `wishlistBanner` + `linearProgressIndicator` + `recyclerViewChips`
  (search-term chips) + `searchResultsHeader` + `recyclerViewSearches` +
  `searchEmptyStateFlipper` (loading / `noResultsView` / `allFilteredView` /
  `noSearchesView` / `searchFailedView`) + collapsible `bottomSheet` filter panel
  (text filter, `formatToggleGroup` Any/Lossless/Lossy, `bitrateToggleGroup`
  Any/320/256/192/128, `toggleSmartFilters`, `stickyFilterCheckbox`).
- Item views/adapters: `SearchAdapter.cs`, `ChipsAdapter.cs`, `GroupedChipsAdapter.cs`,
  `SearchTabAdapter.cs`, `SearchItemViewsUnified.cs`
  (`SearchItemViewSimpleTop/SimpleBottom/ModernTop/ModernBottom/Compact`), layouts
  `search_result_simple_top/bottom`, `search_result_modern_top/bottom`,
  `search_result_compact`, `chip_item_view`, `grouped_chip_item_view`, `search_tab_layout`,
  `tab_page_item`, `search_results_header`, `wishlist_header`.
- Dialogs/sheets: `SearchDialog.cs` (`search_intent_dialog.xml` + dummy activity),
  `SearchTabDialog.cs` (`search_tab_layout`), `DownloadDialog.cs`
  (`downloaddialog.xml`, `download_row.xml`), `CategoryFilterBottomSheet.cs`
  (`category_filter_bottom_sheet.xml`, `category_filter_bottom_sheet_row.xml`),
  `BottomSheetDialogFragmentMenu` (`search_result_style_selection.xml`),
  `SmartFiltersConfig` (`smart_filter_config_layout.xml`, `smart_filter_config_item.xml`,
  drag-drop via `DragDropItemTouchHelper`), sort dialog (`changeresultsortorder.xml`),
  target dialog (`changeusertarget.xml`), `UiHelpers.ShowSimpleDialog` variants.
- Menus: `search_menu.xml` (search, change target, change result style, expand all,
  grouped chips toggle, sort, add to wishlist).
- State: `SearchTabHelper.cs` (search tabs + wishlist tabs, `TabType`, sorting, text filter,
  `SearchResponses`, unseen counts, disk persistence), `WishlistController.cs`,
  `Common/Search/SearchTab.cs`, `Common/SearchEnums.cs`
  (`SearchResultStyleEnum` Simple/Modern/BitrateTop/Expandable/Compact).
- Interactions: submit/again search, chip add/remove, tab add/remove/switch, wishlist
  add/remove + banner, filter toggles, result style switching, expand/collapse rows,
  long-press result context menu (`UiHelpers.HandleCommonContextMenuActions`),
  download dialog with batch selection + folder browsing, swipe-to-refresh (see
  `SwipeRefreshLayout` in `DownloadDialog`/fragment).

### 5.3 Transfers tab (`navigation_transfers`)

- `Seeker/Transfers/TransfersFragment.cs` (1276) + partials
  `TransferAdapters.cs`, `TransferItemViews.cs`, `TransferActionMode.cs`;
  layout `transfers.xml` (`recyclerView1`, `noTransfersView` with icon/header/subtitle,
  `setUpSharing` CTA).
- Item views: `TransferItemViewFolder`, `TransferItemViewDetails`
  (`transfer_folder_item.xml`, `transfer_single_item.xml`, `transfer_row.xml`),
  `TransferViewHelper`, `SegmentedProgressBar.cs` (custom drawn progress),
  `TransferDebouncer.cs`.
- Menus: `transfers_menu.xml` (toggle downloads/uploads, group-by, clear/abort/pause/resume,
  retry failed, batch select), `transfers_options.xml` (show size/speed/time-remaining),
  `transfers_menu_batch.xml` (resume/pause/retry selected, invert, select all).
- Interactions: per-item + per-folder context menus (`TransferContextMenuItem` enum),
  batch action mode, pause/resume/abort/clear, retry, queue-position refresh, play file,
  browse-at-location, ignore/unshare user, sort/group by folder, downloads↔uploads switch,
  swipe-to-refresh, empty-state CTA to sharing settings.
- State: `Common/Transfers/TransfersViewState.cs` (singleton, current folder, DL/UL mode),
  `TransferUIState.cs`, `TransferItems/TransferItemManager*`, `TransferPersistence.cs`
  (`TransferPersistenceWrapper` save/restore), `TransferEventRouter.cs`
  (`StateChangedForItem`, `ProgressUpdated`, `ItemRemoved`), `Common/Transfers/*`.

### 5.4 Browse tab (`navigation_browse`)

- `Seeker/Browse/BrowseFragment.cs` (1340) + `BrowseActionMode.cs`, `BrowseAdapter.cs`,
  `TreePathRecyclerAdapter.cs`, `BrowseService.cs`, `BrowseResponseEvent.cs`;
  layout `browse.xml` (`recyclerViewHorizontalPath` breadcrumb, `listViewDirectories`,
  `noBrowseView`, quick-filter `bottomSheet` with `filterText`, `bsbutton` actions).
- Item views: `BrowseResponseItemView` (`browse_response_item.xml`),
  `TreePathItemView` (`tree_path_item_view.xml`), `IconMenuAdapter` +
  `icon_menu_item.xml` (sort/filter icon dropdowns).
- Menus: `browse_menu_full.xml` (up directory, browse user, download folder,
  user options, more options incl. queue-as-paused, folder info, copy URL),
  `browse_menu_empty.xml`, `browse_menu_batch.xml`.
- Interactions: breadcrumb navigation, folder/file click, batch action mode,
  file/folder context menus, filter text, sort/filter dropdowns, download folder/files,
  copy slsk link, browse-at-location from transfers, swipe-to-refresh.
- State: `Common/Browse/BrowseState.cs`, `Common/Browse/BrowseModels.cs`,
  `Common/Browse/BrowseUtils.cs`, `BrowseService.BrowseResponseReceived` wired in
  `SeekerApplication`; `BrowseFragment.Instance` static.

### 5.5 Messages (separate activity, 2-level)

- `Seeker/Messages/MessagesActivity.cs` (427), `MessagesOverviewFragment.cs`,
  `MessagesOverviewRecyclerAdapter.cs`, `MessagesOverviewActionMode.cs`,
  `MessagesInnerFragment.cs`, `MessagesInnerRecyclerAdapter.cs`, `MessageController.cs`.
- Layouts: `messages_main_layout.xml` (toolbar + `content_frame`), `messages_overview.xml`
  (`recyclerViewOverview`, `noMessagesView`, FAB `fabNewMessage`),
  `messages_inner_layout.xml` (RecyclerView + chat box: `edit_gchat_message`,
  `button_gchat_send`), item layouts `message_overview_item.xml`,
  `messages_inner_item_toMe.xml`, `messages_inner_item_fromMe.xml`,
  `group_messages_inner_item_toMe.xml`, `user_status_update_item.xml`,
  `autocomplete_user_dialog_content.xml`, `simple_custom_notification.xml`.
- Menus: `messages_overview_list_menu.xml` (mark all read, delete all),
  `messages_overview_batch_menu.xml` (delete/mark read/select all/invert/user actions),
  `messages_inner_list_menu.xml` (user actions + delete messages),
  `message_long_press_popup.xml` (copy text).
- Interactions: overview list, searchable new-message dialog (autocomplete of recent
  users), inner thread with send button + IME Send + Enter, long-press copy, batch select,
  notification mark-as-read broadcast (`MessagesBroadcastReceiver`).
- State: `MessageController` (static, disk persistence), `Common/Messages/*`
  (`Message`, `MessageModels`, `Events`), `Helpers/UserStatusDeduplicator.cs`.

### 5.6 Chatrooms (separate activity, 2-level)

- `Seeker/Chatroom/ChatroomActivity.cs` (446), `ChatroomOverviewFragment.cs`,
  `ChatroomInnerFragment.cs`, `ChatroomAdapters.cs`, `ChatroomController.cs`,
  `RoomUserListDialog.cs`, `RoomUserListRecyclerAdapter.cs`, `RoomTickersDialog.cs`.
- Layouts: `chatroom_main_layout.xml`, `chatroom_overview.xml` (SearchView
  `filterChatroom`, `recyclerViewOverview`), `chatroom_overview_item.xml`,
  `chatroom_overview_joined_item.xml`, `chatroom_overview_category_item.xml`,
  `chatroom_inner_layout.xml` (message list, statuses view, ticker view,
  `fabScrollToNewest`, input + send, `joinFailedRetry`), `room_users_dialog.xml`,
  `room_user_list_item.xml`, `all_ticker_dialog.xml`, `ticker_row.xml`,
  `create_chatroom_dialog.xml`, `change_sort_room_user_list_dialog.xml`,
  `chatroom_connect_disconnect_item.xml`.
- Menus: `chatroom_overview_list_menu.xml` (create room, refresh room list),
  `chatroom_inner_menu.xml` (custom `action_chatroom_overflow` action view).
- Interactions: room list with category filter, join/leave, create room (name + private),
  room users dialog (sort + batch + user action sheet), all-tickers dialog, set ticker,
  auto-join toggle, statuses/tickers toggles, scroll-to-newest FAB, message send,
  overflow menu implemented with **`AnchoredMenuPopup`** (`Helpers/AnchoredMenu/*`).

### 5.7 Settings

- `Seeker/Settings/SettingsActivity.cs` (1475) + `SettingsActivity.Host.cs`
  (RecyclerView host, inline search, section scroll), `Rows/SettingsCatalog.cs` (768 —
  data definitions), `Rows/SettingsAdapter.cs`, `Rows/SettingRow.cs` (row types:
  `HeaderRow`, `ToggleRow`, `ValueRow`, `ActionRow`, `ButtonRow`, `ButtonPairRow`,
  `SharedFolderRow`, `NavigationRow`, `EmptyStateRow`), `Rows/SettingPickers.cs`,
  `Rows/SettingValueFormat.cs`, `Rows/SettingsDividerDecoration.cs`, `Rows/ISettingsHost.cs`.
- Layouts: `settings_layout.xml`, `row_setting_*.xml` (header, toggle, value, nav, action,
  button_card, button_pair, shared_folder, empty), `row_about_link.xml`, `choose_setting.xml`,
  `SelectDialogItem.xml`, `SimpleDropDownItem1Line.xml`, plus sheets below.
- Bottom sheets / dialogs: `Rows/AboutBottomSheet.cs` (`about_bottom_sheet.xml`),
  `Rows/EnableNumericBottomSheet.cs`, `Rows/NumericInputBottomSheet.cs`,
  `Rows/OptionPickerBottomSheet.cs` (`bottom_sheet_option_picker*.xml`),
  `Rows/SpeedLimitBottomSheet.cs`, `Rows/UploadFolderOptionsBottomSheet.cs`,
  `Rows/PrivilegesBottomSheet.cs` (`privileges_bottom_sheet.xml`),
  `Rows/MoreInfoBottomSheet.cs`, `Rows/ForceFileSystemMoreInfoSheet.cs`,
  `edit_text_password_dialog_content.xml`, `file/folder pickers`
  (`InternalDirectoryPickerLowAPI.cs`), `ImportWizardActivity`.
- Sections (ids in `SettingsCatalog`): `h.downloads`, `h.search`, `h.general`,
  `h.appearance`, `h.sharing`, `h.network`, `h.account`; rows include download folder,
  complete/incomplete split, username subfolders, single-download subfolder,
  manual incomplete folder, file-backed downloads, auto-clear, auto-retry, max results,
  smart filters + configure, free slots only, locked results in search/browse,
  remember/clear search history, startup service, private room invites, folder-complete
  notifications, toasts, recent users, auto-away, language, night mode, day/night variant,
  sharing on/off + shared folders + metered/VPN gating + auto-clear uploads, listener/port
  + port check + UPnP, speed limits (DW/UP), concurrency limits, privileges, diagnostics,
  change password, edit own user info, import/export, force filesystem permission,
  restore defaults.
- Menu: `settings_menu.xml` (search settings — wired to the inline `SearchView`).
- Interactions: inline keyword search/filter, dependent row enable/disable
  (`DependentRowIds`), live status text/dot (`SettingStatus`), file/folder pickers via
  SAF (`StartActivityForResult`), import/export wizard, scroll-to-sharing intent extra.

### 5.8 Users

- `Users/UserListActivity.cs` (397) — layout `user_list_activity_layout.xml`
  (`user_list_toolbar`, `userList` RecyclerView, `noUsersView`), adapter
  `RecyclerUserListAdapter.cs` (+ `user_row.xml`), menu `user_list_menu.xml`
  (add user, sort, add to ignored, settings), sort dialog
  `change_sort_order_dialog.xml`, add-user dialog
  `autocomplete_user_dialog_content.xml`, long-press → **ActionSheet**
  (`Helpers/ActionSheet/*`), tabs friends/ignored.
- `Users/ViewUserInfoActivity.cs` — `view_user_info_layout.xml` (stats card:
  upload slots/queued/speed/files/dirs/free-slots + shimmers, picture/bio, no-picture
  fallback), menu `view_user_info_menu.xml` (add, browse files, search files, message).
- `Users/EditUserInfoActivity.cs` — `edit_user_info_layout.xml` (bio, picture pick/clear,
  unsaved-changes dialog), menu `edit_user_info_menu.xml`.
- Supporting: `PrivilegesManager.cs`, `RecentUserManager.cs`, `RequestedUserInfoHelper.cs`,
  `UserInfoResponder.cs`, `UserListService.cs`, `UserMetadataService.cs`,
  `Common/Users/*` (`UserListItem`, `UserListUtils`, `IUserListService`, `UserRole`).

### 5.9 Global UI helpers / primitives

- `Helpers/UiHelpers.cs` — theme color/drawable lookup, dialogs (`ShowSimpleDialog`,
  `ShowReportErrorDialog`, `ShowSimpleAlertDialog`, `ShowEditAddNoteDialog`,
  `ShowGivePrilegesDialog`), action sheet bridge, snackbar host,
  recent-user autocomplete, message text spans, menu title helpers.
- `Helpers/ActionSheet/*` — `ActionSheetConfig`, `ActionSheetDialog`,
  `ActionSheetActions` (user/ignored-user action sections, room-admin context), layouts
  `action_sheet_dialog.xml`, `action_sheet_row.xml`, `action_sheet_section_header.xml`,
  `action_sheet_divider.xml`.
- `Helpers/AnchoredMenu/*` — `AnchoredMenuConfig` (plain/checkable/submenu rows),
  `AnchoredMenuPopup`, layouts `anchored_menu_*.xml`. Used by chatroom inner overflow.
- `Helpers/AppNotifications.cs`, `Helpers/AndroidToaster.cs` (`IToaster`),
  `Helpers/SharingIcons.cs`, `Helpers/CrossFadeDrawable.cs`,
  `Helpers/BackNavigationHelpers.cs`, `Helpers/GenericOnBackPressedCallback.cs`,
  `Helpers/BottomMarginInsetsListener.cs`, `Helpers/BottomOnlyInsetsListener.cs`,
  `Helpers/HorizontalInsetsListener.cs`, `Helpers/SafeMaterialAutoCompleteTextView.cs`,
  `Helpers/SlskLinkClickableSpan.cs`, `Helpers/MagnetLinkClickableSpan.cs`,
  `Helpers/ThemeHelper.cs`, `Helpers/LocaleHelper.cs`, `Helpers/PreferencesManager.cs`.

---

## 6. Dialog / sheet inventory (by mechanism)

| Mechanism | Classes / files | Layouts |
| --- | --- | --- |
| `DialogFragment` | `SearchDialog`, `SearchTabDialog` | `search_intent_dialog`, `search_tab_layout` |
| `BottomSheetDialogFragment` | `DownloadDialog`, `CategoryFilterBottomSheet`, `BottomSheetDialogFragmentMenu` (in `SearchBottomSheet.cs`), `RoomUserListDialog`, `RoomTickersDialog`, `AboutBottomSheet`, `MoreInfoBottomSheet`, `ForceFileSystemMoreInfoSheet`, `EnableNumericBottomSheet`, `NumericInputBottomSheet`, `OptionPickerBottomSheet`, `SpeedLimitBottomSheet`, `UploadFolderOptionsBottomSheet`, `PrivilegesBottomSheet` | `downloaddialog`, `category_filter_bottom_sheet`, `search_result_style_selection`, `room_users_dialog`, `all_ticker_dialog`, `about_bottom_sheet`, `more_info_bottom_sheet`, `force_filesystem_more_info_sheet`, `bottom_sheet_*`, `privileges_bottom_sheet` |
| `MaterialAlertDialogBuilder` (imperative) | `SearchFragment.ShowChangeSortOrderDialog/ShowChangeTargetDialog`, `UserListActivity.ShowSortDialog/ShowAddUserDialog`, `MessagesActivity.ShowEditTextMessageUserDialog`, `ChatroomActivity` create/set-ticker, `UiHelpers.ShowSimpleDialog` & friends, `SettingsActivity` pickers | `change*order*.xml`, `changeusertarget`, `autocomplete_user_dialog_content`, `create_chatroom_dialog`, `edit_text_dialog_content`, `edit_text_password_dialog_content`, `error_dialog_layout`, `user_note_dialog`, `give_privileges_layout` |
| Custom action sheet | `Helpers/ActionSheet/ActionSheetDialog` (user long-press actions in user list + room user list + chatroom adapters) | `action_sheet_*.xml` |
| Custom anchored popup | `Helpers/AnchoredMenu/AnchoredMenuPopup` (chatroom inner overflow, checkable + submenu) | `anchored_menu_*.xml` |
| `PopupMenu` | `DownloadDialog`, `TransfersFragment` (`transfers_options`, `transfers_menu_batch`), fragment context menus | `download_diag_options`, `transfers_options` |

Error/empty/loading surfaces: `error_dialog_layout.xml`, `search_no_results.xml`,
`chat_error96dp.xml`, `check_circle_96dp.xml`, `filter_list_96dp.xml`,
`file_stacks_drawable*`, `download_search_drawable*`, plus the empty-state blocks inside
`searches.xml`, `transfers.xml`, `browse.xml`, `messages_overview.xml`.

---

## 7. Menu inventory (all 21)

| Menu | Owner |
| --- | --- |
| `navigation_menu.xml` | `MainActivity` bottom nav (4 tabs) |
| `account_menu.xml` | `LoginFragment` (users/messages/chatrooms/settings/shutdown/about) |
| `menu_main.xml` | legacy/inflate commented out in `MainActivity.OnCreateOptionsMenu` (dead) |
| `search_menu.xml` | `SearchFragment` |
| `download_diag_options.xml` | `DownloadDialog` popup |
| `transfers_menu.xml`, `transfers_options.xml`, `transfers_menu_batch.xml` | `TransfersFragment` |
| `browse_menu_full.xml`, `browse_menu_empty.xml`, `browse_menu_batch.xml` | `BrowseFragment` |
| `messages_overview_list_menu.xml` | `MessagesOverviewFragment`, also inflated by `EditUserInfoActivity` |
| `messages_overview_batch_menu.xml` | `MessagesOverviewActionMode` |
| `messages_inner_list_menu.xml` | `MessagesInnerFragment` |
| `message_long_press_popup.xml` | `UiHelpers.ShowCopyMessageTextPopup` |
| `chatroom_overview_list_menu.xml` | `ChatroomActivity` + `ChatroomOverviewFragment` |
| `chatroom_inner_menu.xml` | `ChatroomInnerFragment` (overflow action view) |
| `settings_menu.xml` | `SettingsActivity` |
| `user_list_menu.xml` | `UserListActivity` |
| `view_user_info_menu.xml` | `ViewUserInfoActivity` |
| `edit_user_info_menu.xml` | `EditUserInfoActivity` |

Menus are inflated either via `OnCreateOptionsMenu` (fragments with `HasOptionsMenu`) or
`AndroidX.Core.View.IMenuProvider` + `Activity.AddMenuProvider` (messages/chatroom
fragments). Titles/user-dependent items are rewritten in `OnPrepareMenu` /
`UiHelpers.SetMenuTitles`.

---

## 8. Interaction inventory (grouped)

- **Navigation**: bottom nav (4), account overflow menu, in-activity toolbars,
  breadcrumbs (browse), back stack (browse folder / messages thread / chatroom room),
  deep links (`slsk://`, search intents), notification taps, section scroll intent.
- **Selection / batch**: action modes in transfers (`ActionModeCallback`), browse
  (`BrowseActionModeCallback`), messages overview (`MessagesOverviewActionMode`),
  download dialog selection, room user list batch; select-all / invert-selection.
- **Long-press**: user rows (ActionSheet), transfers (context menu), browse files/folders
  (popup), messages (copy-text popup), search results (context menu).
- **Pull-to-refresh**: search `DownloadDialog` folder contents, browse, transfers.
- **Text input**: global search field (`searchHere`), inline filter panels (search/browse),
  chat/message composers (send button + IME action + Enter), settings inline search,
  chatroom list filter.
- **Toggle chips / segmented buttons**: search format & bitrate filters
  (`MaterialButtonToggleGroup`), smart filters on/off + config drag order, result-style
  selection sheet, transfer display toggles.
- **Tabs / tabs-within-tab**: search tabs + wishlist tabs (`SearchTabDialog`), transfers
  downloads/uploads, user list friends/ignored, settings sections.
- **Progress / status**: `linearProgressIndicator`, `SegmentedProgressBar`,
  connection-status chip/dot, transfer status chip/dot, settings status dot,
  shimmer placeholders in user info.
- **Feedback**: Snackbar (`UiHelpers.GetViewForSnackbar`), toasts (`AndroidToaster`),
  `AppNotifications` notifications, error dialogs, empty states.
- **Keyboard/insets**: bottom nav hide on IME, edge-to-edge (`WindowCompat.SetDecorFitsSystemWindows(false)`),
  `BottomOnlyInsetsListener`/`BottomMarginInsetsListener`/`HorizontalInsetsListener`.

---

## 9. App state / service boundaries

**Static/global UI state (implicit singletons — the main refactor hazard)**

- `Seeker/Main/SeekerState.cs`: `SoulseekClient`, `MainActivityRef`, `ActiveActivityRef`,
  `LoginFragmentRef`, `InDarkModeCache`, `OurCurrentStatusIsAway`,
  `PendingStatusChangeToAwayOnline`, `ManualResetEvent`, `SharedPreferences`.
- `Common/CommonState.cs`: `UserList`, `IgnoreUserList`.
- `Common/PreferencesState.cs` (283 lines): credentials, directories, search prefs,
  theme/language, transfer display prefs, speed limits, sharing, listener/UPnP,
  chatroom view prefs, smart filters, diagnostics — persisted through
  `Helpers/PreferencesManager.cs` (1030 lines) + `Common/KeyConsts.cs`.
- `Common/Transfers/TransfersViewState.cs` + `TransferUIState.cs` (view mode, folder,
  sort/group), `Common/Browse/BrowseState.cs` (per-user browse tree),
  `SearchTabHelper` (tabs, responses, sorting, unseen), `SearchFragment.Instance`,
  `BrowseFragment.Instance`, `WishlistController`, `MessageController`,
  `ChatroomController` (all static/instanced singletons).
- `SeekerApplication`: `ApplicationContext`, `Toaster`, wake/wifi locks,
  `CurrentLoginTask`, `ActiveUploadCount/ActiveDownloadCount`, DNS lookup status.

**Service layer (`Seeker/Services/`)**

| Service | Role |
| --- | --- |
| `SessionService.cs` | login/logout orchestration, `LoginStarted`/`LoginCompleted` |
| `ReconnectService.cs` | reconnect loop / network handoff |
| `FileSystemService.cs` | `IFileSystemService` (SAF streams) |
| `MainThreadRunner.cs` | `IMainThreadRunner` (UI marshalling) |
| `AndroidToaster.cs` (`Helpers/`) | `IToaster` |
| `AndroidNetworkStatus.cs` | `INetworkStatus` |
| `NetworkStateService.cs`, `StorageState.cs`, `ServiceLifecycle.cs` | platform state / service bookkeeping |
| `SharedFileService.cs`, `SharingService.cs`, `UploadService.cs` | sharing + upload queues |
| `ServerMessageService.cs` | server broadcast banner |
| `UserInfoPictureCacheService.cs` | avatar cache |
| `ParsingStatusState.cs` | share-parsing progress for settings |
| `Common/Services/DownloadService.cs` | download engine (`TransferItemChanged`, `DownloadAddedUINotify`, …) |
| `UPnP/UPnPManager.cs` (+ `UPnPDiagStatus`, `UPnPRunningStatus`) | port mapping |
| `Transfers/TransferEventRouter.cs`, `TransferPersistence.cs`, `TransferCleanup.cs` | transfer event → UI + persistence |
| `Browse/BrowseService.cs`, `Users/UserListService.cs`, `Messages/MessageController.cs`, `Chatroom/ChatroomController.cs` | feature controllers |
| `Serialization/*` + `Common/Serialization/*` | MessagePack cache providers/migration |

**Boundary characteristics**

- UI subscribes directly to network/client events and static controllers; there is no
  presenter/ViewModel indirection. Screens mutate `PreferencesState`/`SearchTabHelper` and
  read them back.
- Services are constructed by hand in `Application.OnCreate`; interfaces exist only for
  testability seams (`IToaster`, `IMainThreadRunner`, `IFileSystemService`,
  `INetworkStatus`, `IUserListService`, `ISessionService`, `ICacheDataProvider`).
- Persistence: `SharedPreferences` via `PreferencesManager`, MessagePack caches under the
  user-chosen SAF data directory, `TransferPersistenceWrapper` on last-activity destroy.

---

## 10. Test frameworks present

| Suite | Project | Framework | Scope | Runs how |
| --- | --- | --- | --- | --- |
| Unit tests | `UnitTestCommon/UnitTestCommon.csproj` (`net10.0`) | NUnit 4.3.2 + NUnit3TestAdapter 5 + Verify.NUnit 28 + coverlet | search parsing/comparison/filtering, browse tree/state, transfer item managers/persistence/upload queue, import/export, serialization/backward-compat, micro-tag reader, shared file cache, wishlist, download failure classifier, **StringResourceTests** (string key parity, placeholders, escaping, XML validity) | `dotnet test … --filter TestCategory!=RealData` (`.github/workflows/unittests.yml`) |
| Instrumentation | `InstrumentationTests/` (`net10.0-android36.0`) | NUnit 4.2.2 + custom `TestInstrumentation` runner writing to logcat | `OpenOutputStreamTests`, `ChunkedWriteBenchmarkTests` | `adb shell am instrument -w com.seeker.instrumentationtests/instrumentationtests.TestInstrumentation` |
| UI/end-to-end | `MaestroTests/` | Maestro YAML flows (external CLI) | `screenshots_core.yaml`, `screenshots_light_dark.yaml`, `login_logout_stress_test.yaml`, `search_selection_sort.yaml`, `shell_smoke.yaml`, `follow_system_theme.yaml`, `recreation_navigation.yaml`, `large_font_sample.yaml`, shared `common/` flows (`optional_login`, `change_theme`, `set_dark_light`, `optional_set_download_dir`, `back_to_main`) — tap by resource-id, screenshot capture | Maestro CLI against a device |
| Vendor tests | `Seeker/Soulseek.NET-master/tests/` | NUnit | protocol/library (not in `Seeker.sln`) | not part of app CI |

No Compose/Espresso/Robolectric tests. No screenshot-golden diffing beyond Maestro
screenshots. **Maestro flows depend on resource IDs** (`navigation_search/transfers/browse/messages`,
`searchHere`, `menuSettings`, `menuLogout`, `toggleSmartFilters`, `buttonLogin`,
`etUsername`, `etPassword`, `dayVarientSpinner`, `nightModeSpinner`,
`nightVarientSpinner`, `buttonDownload`, `action_search`, `action_browse_user`,
`action_toggle_download_upload`, `chosenUserEditText`, `bsbutton`) — renaming IDs breaks
the only end-to-end safety net.

---

## 11. Independently replaceable components

Ordered from most to least decoupled (best redesign targets first):

1. **Theme/token layer** — `values/themes.xml`, `styles.xml`(+night), `attrs.xml`,
   `colors.xml`(+night), `dimens.xml`, `drawable/` shape+selector set,
   `Helpers/ThemeHelper.cs`, `UiHelpers.SetActivityTheme`. Replaceable behind the existing
   `?attr/*` and `Resource.Style.*` names.
2. **Reusable dialog/menu primitives** — `Helpers/ActionSheet/*` (self-contained config →
   dialog), `Helpers/AnchoredMenu/*`, `UiHelpers.ShowSimpleDialog` family. ~5 call sites
   for ActionSheet, 1 for AnchoredMenu.
3. **Settings screen** — `Settings/Rows/*` model is already data-driven
   (`SettingsCatalog` → `SettingsAdapter` → `row_setting_*.xml`), so visual redesign is
   mostly new row layouts + adapter; `ISettingsHost` isolates activity callbacks.
4. **Search result item views** — `SearchItemViewsUnified.cs` + 5 item layouts +
   `SearchAdapter.cs`; style enum already selects a family. High visual payoff, contained
   blast radius.
5. **Search filter/bottom sheet** — `searches.xml` bottom-sheet block,
   `SearchBottomSheet.cs`, `CategoryFilterBottomSheet.cs`, `SmartFiltersConfig.cs`.
6. **Transfer rows** — `TransferItemViews.cs`, `TransferViewHelper`,
   `SegmentedProgressBar`, `transfer_*_item.xml`; but coupled to
   `TransfersViewState`/`TransferItemManager`.
7. **Browse rows + breadcrumb** — `BrowseAdapter.cs`, `TreePathRecyclerAdapter.cs`,
   `browse_response_item.xml`, `tree_path_item_view.xml`.
8. **Login dashboard** — `LoginFragment.cs` + `login_viewflipper.xml` (self-contained
   states, but bound to session/login tasks).
9. **User info screens** — `ViewUserInfoActivity`, `EditUserInfoActivity`
   (+ their layouts/menus) are isolated activities.
10. **Messages thread/overview UI** — item layouts/adapters are separable from
    `MessageController`.
11. **Chatroom UI** — adapters + inner/overview fragments; overflow already isolated in
    `AnchoredMenuConfig`.
12. **App shell / navigation** — `MainActivity` + `TabsPagerAdapter` + `activity_main.xml` +
    `custom_menu_layout.xml` (highest risk: statics, service routing, Maestro IDs).

Not cleanly replaceable: `SeekerApplication` wiring, `SeekerState` statics, transfer
engine/`DownloadService`, `SearchTabHelper` persistence, `SharedFileService` (all contain
UI-affecting behavior).

---

## 12. Phased implementation map

Phase 1 — **inventory only**. Deliverable: this document. Verification: `git diff --check`;
no source change. **Status: complete.**

Phase 2A — **Design tokens & theming foundation** (no screen changes). **Status: complete,
see §15.** Files: `values/attrs.xml`, `themes.xml`, `styles.xml`(+night), `colors.xml`(+night),
`dimens.xml`, `drawable/ds_*.xml`, `Helpers/ThemeHelper.cs`, `Helpers/UiHelpers.cs`,
`Main/ThemeableActivity.cs`, `Common/ThemeEnums.cs`, `Common/PreferencesState.cs`,
`Helpers/PreferencesManager.cs`, `Settings/Rows/SettingPickers.cs`,
`Settings/Rows/SettingValueFormat.cs`, `Settings/SettingsActivity.Host.cs`,
`layout/activity_main.xml` (tint attributes only), `Misc/test_ui_contracts.py`.
Acceptance: all 12 themes resolve every custom attribute; no orphan/duplicate styles; static
contract test + `Debug Mock` build green.

Phase 2B (was "Phase 2") — **remaining foundation work**: migrate individual layouts/drawables
to the tokens (hard-coded colours, `drawable-night` consolidation, per-screen `?attr/`
cleanup). Only tokens/themes are done in 2A.

Phase 3 — **Shared primitives** (`ActionSheet`, `AnchoredMenu`, simple dialogs,
empty-state blocks, snackbar host, status chips/dots, `SegmentedProgressBar`).
Acceptance: user list / room user list / chatroom overflow / settings sheets behave
identically (manual + Maestro `change_theme`, `back_to_main` flows).

Phase 4 — **Settings screen** (data-driven rows + sheets), lowest-risk full screen.
Phase 5 — **Login/home dashboard + app shell chrome** (toolbar, bottom nav, search field).
Phase 6 — **Search** (result item views, filter sheet, search tabs/wishlist, download dialog).
Phase 7 — **Transfers** (rows, folder group, batch action mode, options popup).
Phase 8 — **Browse** (breadcrumb, rows, filters, batch mode, context menus).
Phase 9 — **Users / User info / Edit user info**.
Phase 10 — **Messages** (overview + thread) and **Chatroom** (overview + room).
Phase 11 — **Accessibility / RTL / larger screens / dynamic color**; fix
`android:supportsRtl="false"`, add `contentDescription`s, min touch targets.
Phase 12 — **Full regression**: `dotnet test UnitTestCommon`, instrumentation tests,
Maestro `screenshots_light_dark` + `screenshots_core` (update screenshots intentionally),
manual smoke of both `Debug` and `Debug Mock` configurations.

Each phase should end with: `git diff --check`, `xmllint --noout` on touched XML,
and (where a .NET SDK exists) `dotnet build … -c "Debug Mock"` + `dotnet test UnitTestCommon`.

> **Execution note.** As with Phase 3 (search), Phase 4 was implemented as the
> **Transfers + remote browsing** slice ahead of the map above (originally split
> across the Phase 7/8 rows). No service, protocol/networking, persistence or transfer
> scheduling code was changed. See §18.
>
> Phase 5 was implemented as the **messaging / chatrooms / settings / login + users**
> slice, including the remaining layout and programmatic palette audit. It also folds in
> the originally separate Phase 9 (users) and Phase 10 (messages/chatroom) visual work.
> No protocol, service, persistence or scrobble/transfer behaviour changed. See §19.

---

## 13. Risks

1. **Runtime UI verification is unavailable** (no device/emulator, no Maestro CLI). The .NET 10
   SDK + android workload were installed during 2A, so resource compilation, C# compilation and
   the static contracts are checked; visual regressions still need a device run.
   Mitigation: `Misc/test_ui_contracts.py` (tokens, attr coverage, duplicates, contrast,
   preference compatibility, theme-switch isolation) + `dotnet build`.
2. **Static global state** (`SeekerState`, `CommonState`, `PreferencesState`,
   `TransfersViewState`, fragment `Instance` fields, `ActiveActivityRef`) makes screens
   non-reentrant, leak-prone and order-dependent. Introducing ViewModels/repositories
   would be a large behavioural change.
3. **Broadcast to `SeekerApplication.Activities` / `ActiveActivityRef`**: dialogs, snackbars
   and toasts assume a live activity; touching lifecycle risks
   `WindowManager$BadTokenException` and crashes from background callbacks.
4. **Theme attr sprawl**: 56 legacy attributes + 40 tokens × 12 themes; a missing attr yields
   inflate-time crashes. Mitigated in 2A: the token layer + `AppTheme` mapping means a theme only
   declares tokens, and `Misc/test_ui_contracts.py` fails if any declared attribute is not
   resolved by a theme. Hard-coded colours in individual layouts still bypass the layer.
5. **Oversized classes**: `SearchFragment` 2503, `SettingsActivity` 1475, `BrowseFragment`
   1340, `TransfersFragment` 1276, `MainActivity` 1265, `PreferencesManager` 1030.
   Restyling touches fields/methods interleaved with business logic.
6. **No Navigation component / no single-action back model**: manual fragment transactions,
   custom back callbacks, `enableOnBackInvokedCallback` — shell redesign risks
   double-handling or losing state.
7. **Five search result layouts + 5 style variants + chip/grouped-chip paths** — easy to
   leave an unthemed path (e.g. grouped chips, compact style).
8. **Localization**: 805 strings, 21 locales via Crowdin; `StringResourceTests` enforce key
   parity/placeholders/escaping. New or renamed string keys need translations and will fail
   those tests if keys drift. Avoid hard-coded user-visible text.
9. **RTL disabled** (`android:supportsRtl="false"`) even though `ar` is a supported locale;
   the redesign should not assume LTR-only.
10. **Maestro is the only E2E safety net and it taps resource IDs**; renaming ids
    (`navigation_*`, `searchHere`, `buttonLogin`, `toggleSmartFilters`, spinners) silently
    breaks flows. Keep ids stable or update flows in the same change.
11. **Settings behavior is data-driven with live status tickers, dependent rows and
    SAF pickers**; a new adapter must preserve `Id`s (e.g. `h.sharing` scroll target) and
    `SettingStatus`.
12. **Menu inflation has two mechanisms** (`OnCreateOptionsMenu` vs `IMenuProvider`) plus
    toolbar-level `InflateMenu` in activities and a custom action view
    (`action_chatroom_overflow`); a menu redesign must cover all three.
13. **Notification/foreground-service UI** (`simple_custom_notification.xml`,
    `DownloadForegroundService`, `UploadForegroundService`) is part of the visible UI and is
    constrained by Android notification APIs; restyling it is not a pure layout change.
14. **Large-screen/inset handling is ad-hoc** (`HorizontalInsetsListener`,
    `BottomOnlyInsetsListener`, `SetDecorFitsSystemWindows(false)`); tablet/foldable support
    needs deliberate layout-container work.
15. **Drawable/vector inventory (~190)** has duplicated assets (`search.xml` vs
    `baseline_search_white_24`, `download*.xml` families); a redesign should consolidate
    cautiously because names are referenced from code (`Resource.Drawable.*`), including in
    `AnchoredMenuConfig`/`ActionSheetConfig` icon fields and settings catalog.

---

## 14. Key files to inspect for the next phase

Shell / theming:
- `Seeker/Resources/layout/activity_main.xml`, `custom_menu_layout.xml`
- `Seeker/Resources/menu/navigation_menu.xml`, `account_menu.xml`
- `Seeker/Resources/values/themes.xml`, `styles.xml`, `attrs.xml`, `colors.xml`,
  `values-night/styles.xml`, `values-night/colors.xml`
- `Seeker/Main/MainActivity.cs`, `MainActivityHelpers.cs`, `TabsPagerAdapter.cs`,
  `ThemeableActivity.cs`, `Seeker/Main/SeekerState.cs`
- `Seeker/Helpers/ThemeHelper.cs`, `Helpers/UiHelpers.cs`, `Helpers/LocaleHelper.cs`
- `Common/PreferencesState.cs`, `Common/ThemeEnums.cs`, `Seeker/Helpers/PreferencesManager.cs`

Screens (highest-value first):
- `Seeker/Settings/Rows/SettingsCatalog.cs`, `SettingsActivity.cs`,
  `SettingsActivity.Host.cs`, `Rows/SettingRow.cs`, `Rows/SettingsAdapter.cs`
- `Seeker/Login/LoginFragment.cs` + `layout/login_viewflipper.xml`
- `Seeker/Search/SearchFragment.cs`, `SearchItemViewsUnified.cs`, `SearchAdapter.cs`,
  `SearchBottomSheet.cs`, `SearchTabDialog.cs`, `DownloadDialog.cs`,
  `CategoryFilterBottomSheet.cs` + `layout/searches.xml`, `search_result_*.xml`
- `Seeker/Transfers/TransfersFragment.cs`, `TransferItemViews.cs`, `TransferAdapters.cs`,
  `SegmentedProgressBar.cs` + `layout/transfers.xml`, `transfer_*_item.xml`
- `Seeker/Browse/BrowseFragment.cs`, `BrowseAdapter.cs`, `TreePathRecyclerAdapter.cs` +
  `layout/browse.xml`, `browse_response_item.xml`, `tree_path_item_view.xml`
- `Seeker/Messages/MessagesActivity.cs`, `MessagesOverviewFragment.cs`,
  `MessagesInnerFragment.cs`, `Messages*RecyclerAdapter.cs`
- `Seeker/Chatroom/ChatroomActivity.cs`, `ChatroomOverviewFragment.cs`,
  `ChatroomInnerFragment.cs`, `ChatroomAdapters.cs`
- `Seeker/Users/UserListActivity.cs`, `ViewUserInfoActivity.cs`, `EditUserInfoActivity.cs`

Primitives / plumbing:
- `Seeker/Helpers/ActionSheet/*`, `Seeker/Helpers/AnchoredMenu/*`
- `Seeker/SeekerApplication.cs`, `Seeker/Services/*`, `Seeker/Transfers/TransferEventRouter.cs`
- `Common/Search/SearchTab.cs`, `Common/Transfers/TransfersViewState.cs`,
  `Common/Browse/BrowseState.cs`

Verification assets:
- `UnitTestCommon/StringResourceTests.cs`, `.github/workflows/unittests.yml`
- `MaestroTests/*.yaml`, `MaestroTests/common/*.yaml`
- `InstrumentationTests/TestRunner.cs`

---

## 15. Phase 2A — implemented vs pending

### 15.1 Implemented

**Centralised design system.** A semantic token layer (40 attributes) plus the mapping of all 56
legacy custom attributes and the Material3 colour roles onto it. Themes now declare tokens only;
screens keep using the attribute names they already used.

| Palette | Dark (as specified) | Light (designed to match) |
| --- | --- | --- |
| background | `#101318` | `#F5F7FA` |
| surface | `#1B222C` | `#FFFFFF` |
| elevated / container | `#26313D` | `#FFFFFF` / `#EDF1F5` |
| accent (primary) | `#5EE0C0` | `#0F7A68` (5.2:1 on white) |
| text primary | `#F0F3F6` | `#101318` |
| text secondary | `#A8B3BF` | `#52606E` (6.0:1 on background) |
| outline | `#34404D` | `#C4CDD6` |
| on-primary | `#06231E` | `#FFFFFF` |

Contrast pairs (text/background, text/surface, secondary/background, on-primary/primary) are
asserted ≥ 4.5:1 by the contract test for both modern themes.

**Theme behaviour**

* New default theme `ModernLight`/`ModernDark` (appended enum values `DayThemeType.Modern = 4`,
  `NightThemeType.Modern = 6`); existing stored choices (0..5) still resolve to the same legacy
  styles, so no migration is required and nothing is destroyed.
* "Follow system" stays the default day/night mode (`-1`); the always-light / always-dark
  overrides are untouched; the AMOLED variants (`Amoled`, `Amoled_Grey`) are retained.
* `ThemeHelper` mapping is total (no `throw`), and corrupted stored ints are clamped to `Modern`.
* Theme switching re-applies the theme and recreates activities only – no client/service/transfer
  call anywhere in the path, and a configuration change no longer triggers a transfer-state save.

**Fixed inheritance / chrome**

* Launch window: `android:windowBackground` (and `windowSplashScreenBackground` on 31+) resolve to
  `dsColorLaunchBackground`, so dark/AMOLED themes no longer flash the Material light window.
* System bars: transparent with `windowLightStatusBar`/`windowLightNavigationBar` per concrete
  theme, plus `UiHelpers.ApplySystemBarIconContrast` for runtime changes.
* Dialogs / sheets / popups / inputs / buttons / snackbar / action mode are themed through
  `materialAlertDialogTheme`, `bottomSheetDialogTheme`, `popupMenuStyle`, `textInputStyle`,
  `materialButtonStyle`, `actionModeStyle` + token colours, replacing the per-configuration
  duplicated styles.
* Forced overlays replaced: `AppTheme.AppBarOverlay` / `AppTheme.PopupOverlay` / `AppPopupOverlay`
  are single token-driven definitions (no forced `Light`/`Dark` parent).
* Bottom navigation item tint/text/background and the app bar now read chrome tokens instead of
  literal `#ffffffff` (only colour attributes in `activity_main.xml` changed – no navigation or
  screen change).

**Reusable resources**

* Typography: `TextAppearance.Seeker.*` (7 styles).
* Spacing/corners/touch targets: `ds_space_*`, `ds_corner_*`, `ds_stroke_width`,
  `ds_touch_target_min` (48dp), `ds_icon_*`.
* Shapes: `ShapeAppearance.Seeker.{Card,Sheet,Input,Button}` via `dsShape*` tokens.
* Drawables/selectors: `ds_cell_selector`, `ds_input_background`, `ds_chat_input_background`,
  `ds_rounded_corner_background`, `ds_rounded_corner_dialog_background`, token-based
  `rounded_button`.
* Colour resources: `values/colors.xml` and `values-night/colors.xml` are now name-for-name
  identical (a reference can no longer silently fall back to a day value in dark mode).

**Duplicates removed (they were real, not cosmetic)**

* `MyActionMode` / `MyActionModeTitle` were defined twice in the same configuration
  (`values/styles.xml` *and* `values/themes.xml`) – now once, token driven.
* `cellStroke` was declared twice in `values/attrs.xml`.
* The whole `values-night/styles.xml` copy of `AppTheme`/`AppTheme.NoActionBar` and the component
  styles (which had drifted: missing `colorSecondaryContainer`, `snackbarButtonStyle`,
  `actionModeStyle`; missing `snackbarButtonStyle`/`Seeker.Snackbar.Action` in night) – now a
  single `Seeker.Theme.Base` config split.
* `values-night/dimens.xml` (identical duplicate of the one dimens entry) removed.

**Automated static checks** — `Misc/test_ui_contracts.py` (93 assertions at the time of 2A;
the current suite also requires PyYAML for the Maestro schema, see §22.3):

* every resource XML parses (362 files),
* no duplicate style/attr/colour definitions in a configuration; cross-config duplicates only
  when listed in `BASELINE_EXCLUSIONS` (currently only `Seeker.Theme.Base`, with the reason),
* every declared custom attribute is resolved by all 12 concrete themes (in the configuration the
  theme is used in),
* token completeness + exact requested dark palette + day/night difference + WCAG contrast for
  both modern themes,
* `ThemeHelper` maps every enum member and every `Resource.Style` it returns exists,
* persisted preference keys/values unchanged, fresh install = follow system + modern variants,
* theme-switch paths do not reference the client/services/transfers and skip the transfer save on
  configuration change,
* overlays are token driven and not force-light/force-dark; `activity_main.xml` chrome has no
  colour literals,
* reusable `ds_*` drawables exist, contain no colour literals and are used by the modern themes,
* `Resource.Style/Drawable/Color/Attribute` ids used from C# all resolve (excluding
  `Android.Resource.*` and the documented library attrs).

### 15.2 Explicitly not in 2A (pending)

* No screen, navigation, layout-structure or menu change (§5–§7 inventories still apply), except
  the colour attributes of the bottom navigation in `activity_main.xml`.
* Existing layouts/drawables that hard-code colours are not migrated; the ~90 legacy vectors keep
  their per-theme assets. `drawable-night/` (4 files) is untouched.
* Legacy themes keep their exact colours (deliberate): e.g. `DefaultLight` still uses
  `@color/colorPrimary` rather than the modern accent.
* Notification/foreground-service UI, `SegmentedProgressBar`, action sheets and anchored menus not
  restyled (Phase 3).
* Accessibility/RTL (`supportsRtl="false"`), large screens, dynamic colour: Phase 11.

### 15.3 Verification performed for 2A

```text
python3 Misc/test_ui_contracts.py                 -> 93 checks passed, 0 failed
git diff --check                                  -> clean
dotnet build Seeker/Seeker.csproj -c "Debug Mock" \
    -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true
                                                  -> 0 Error(s), build + signed APK OK
```

`.NET SDK 10.0.401` and the `android` workload (36.1.69) were installed locally into `~/.dotnet`
for this verification; `global.json`, `Seeker.csproj` (TFM/supported OS/package versions) were
**not** changed.

### 15.4 Known limitations / follow-ups

* No device or emulator is attached, so the themes were verified by resource compilation
  (`aapt2 dump resources` shows the resolved `ModernLight`/`ModernDark` token values) and by the
  static contract test – **not** visually.
* `MaestroTests/common/change_theme.yaml` and `set_dark_light.yaml` select theme options **by
  list index**; the variant lists now start with "Modern", so those two flows need their indices
  (or a text-based selector) updated before they are used again.
* `theme_modern` exists only in the English source (translation parity test is
  `[Ignore]`d and Crowdin will pick it up).
* `Seeker.Theme.Base` is the one style intentionally defined in both `values/` and
  `values-night/`; it must stay a *parent-only* style.
* Legacy dark themes now use their own surface colour (`#ff303030`) as the window/launch
  background instead of the Material default; this is intentional (no light flash) but is a
  visible change on first launch.


---

## 16. Phase 2B — navigation shell

### 16.1 Implemented

**Pager identities (unchanged, messages appended).** `Seeker/Main/MainNavigation.cs` is now the
single contract for the shell: page 0 = account/login (still real, still stateful), 1 = Search,
2 = Transfers, 3 = Browse (all existing notification/deep-link indices preserved), and 4 =
Messages. `TabsPagerAdapter` has five pages, `OffscreenPageLimit = 4` keeps every page resident,
and `UserInputEnabled = false` stops a swipe reaching the hidden account page.

**Bottom navigation** is Search | Transfers | Browse | Messages. `navigation_home` is gone;
`navigation_search/transfers/browse` keep their ids and `navigation_messages` is new. The
selected/unselected/disabled item tint is `res/color/ds_nav_item_tint.xml`, built from
`dsColorNavigationForeground`, `dsColorNavigationForegroundMuted` and `dsColorTextDisabled`.
`SyncToPage` maps by page identity (never by menu index), clears every check first, and leaves no
item selected on the hidden account page. Unread messages are mirrored to a bottom-nav badge from
`MessageController.GetTotalUnreadCount()`.

**Account and secondary navigation.** A fresh activity lands on Search (deep links still override
on a fresh launch, saved pages win on restore). A toolbar `action_shell_menu` opens the existing
ActionSheet primitive with Account/sign-in, Users, Wishlist, Chatrooms, Settings, About and
Shutdown. Settings also has an explicit `account.open_account` row. Back from account returns to
the last primary destination (Search by default). Page + last-primary page are saved in
`onSaveInstanceState` and normalized on restore.

**Messages page.** Page 4 is the real `MessagesOverviewFragment`, not a tile. The fragment, its
adapter and action mode are adapted behind `IMessagesOverviewHost`
(`ShowNewMessageDialog` / `OpenMessagesConversation`), implemented by **both** `MainActivity` and
`MessagesActivity`. The compose dialog is shared (`MessagesComposeDialog`); opening a conversation
from the shell still uses the existing `MessagesActivity` extras. Click, long-press batch
selection, select/invert, mark read, delete, delete-all and both undo paths work in either host.
The host only adds/removes the page menu; account/other pages hide it.

**Chrome.** App bar and toolbar use `dsColorChromeSurface` / `dsColorChromeForeground`, are compact
(`ds_touch_target_comfortable` = 56dp, insets handled by the existing app bar), and the search
field uses the new token drawable `ds_search_bar_background`. Hard-coded white tints/text in
`activity_main.xml` and `custom_menu_layout.xml` were removed.

**Theme fixes.**
* `UiHelpers.ApplySystemBarIconContrast` now resolves the concrete theme's
  `android:windowLightStatusBar` / `windowLightNavigationBar` values through the theme rather than
  deriving them from `!IsNightMode`. Legacy light themes (dark chrome) therefore get light system
  bar icons; ModernLight gets dark icons.
* `ThemeHelper.NormalizeDayTheme/NormalizeNightTheme` reject values outside `0..ushort.MaxValue`
  before casting, so a corrupted negative int can no longer wrap to a valid theme.

**Maestro.** `navigation_home` references are gone; flows use `action_shell_menu` plus the
“Account & sign in” row. Theme pickers select by label (`Modern`, `Always light/dark`) instead of
list indexes. `MaestroTests/shell_smoke.yaml` covers the four bottom destinations, the Messages
page/FAB, secondary navigation/account and back-to-Search.

### 16.2 Static contracts added

`Misc/test_ui_contracts.py` now asserts page identities, bottom-menu order, account-hidden,
pager count/messages creation, default landing/restoration, swipe disabled, selection mapping,
account back behaviour, secondary-sheet destinations, both message hosts, nav selector tokens,
system-bar `windowLight*` resolution and the normalize range guard.

### 16.3 Verification

```text
python3 Misc/test_ui_contracts.py                  -> 129 checks passed, 0 failed
git diff --check                                   -> clean
dotnet build Seeker/Seeker.csproj -c "Debug Mock"   -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true
                                                   -> 0 Error(s), build + signed APK OK
```

No device/emulator or Maestro CLI is available, so the new shell flow and runtime appearance were
not executed here. Remaining risks: Maestro depends on the sheet row text “Account & sign in”
(English source), and the offscreen Messages fragment relies on ViewPager2's RESUMED lifecycle to
keep its `IMenuProvider` hidden while another page is selected (verified by code reading, not a
device).


---

## 17. Phase 3 — Search redesign (implemented slice)

The search slice of the phase map (§12, originally listed with Phase 6) was implemented ahead of
the shared-primitives phase so the highest-traffic screen could be validated first. It reuses the
existing search/history/sort/filter/multiselect/backend operations; no networking, persistence or
sorting algorithms changed.

### 17.1 Implemented

* **Prominent search field** — `custom_menu_layout.xml` keeps `@id/searchHere`, its history
  adapter, hint and `ds_search_bar_background`. It is now at least `ds_touch_target_min` (48dp)
  tall with spacing tokens and an accessibility description; the search-tabs affordance is 48dp.
* **Search actions** — the toolbar search/stop toggle keeps `action_search`, updates its title
  between “Search” and “Stop search”, and the advanced-filter entry is available both from the
  tool bar overflow (`action_advanced_search`) and from the existing FAB (now described for
  TalkBack). Both open the existing filter bottom sheet.
* **Filter header** — `search_results_header.xml` keeps the result count and adds a compact
  active-filter summary (text/format/min-bitrate/smart chips) plus a sort button that opens the
  existing sort dialog. All four backend sort options (availability, speed, folder name, bitrate)
  remain available.
* **States** — the existing empty/loading/no-results/all-filtered/no-searches/failed flipper is
  retained; the failed title now distinguishes signed-out and disconnected sessions from a
  generic failure using the state the search pipeline already holds.
* **Dense result rows** — every supported variant (Modern top/bottom, Simple top/bottom, Compact)
  shows the file name with two lines of room, a format/bitrate chip, peer, speed, file count,
  queue/slot text, and a new compact size • duration • bitrate line. A visible 48dp per-row
  download action calls the existing `ShowDownloadDialog` flow; long-press opens the same dialog,
  whose file multi-select and batch download are the existing batch actions. The whole row keeps
  a generated content description (file, folder, peer).
* **Metadata formatter** — `Common/Search/SearchResultMetadata.cs` is a pure, Android-free
  projection (name/folder/format/bitrate/size/duration/peer/speed/slots/file count) using the
  existing dominant-type and human-readable helpers. It is bound once per row; no new
  per-incoming-result O(n) work was added to the throttled `ApplySearchResults` path.
* **Messages freshness** — the reused `MessagesOverviewFragment` refreshes the adapter in
  `OnResume`, so read markers/deletions made in a thread are visible when returning to the shell
  page. Swipe-delete snackbars now anchor through the fragment (`SnackbarAnchor`) instead of the
  host activity content view.

### 17.2 Tests and contracts

* `UnitTestCommon/SearchResultMetadataTests.cs` covers the pure formatter (name/format/bitrate/
  size/duration/peer/speed/slots, locked-only and hide-locked cases, no-bitrate fallback, null
  response).
* `UnitTestCommon/SearchResultComparableTest.cs` adds a 10,000-result `Fastest` sort test; it
  asserts ordering and does not claim a performance measurement.
* `Misc/test_ui_contracts.py` now asserts the search field/history, 48dp action targets, menu
  entries, empty-state IDs, header sort/filter entries, every result variant's metadata line and
  download action, binding symbols, all sort options, the new strings, and the Messages
  `OnResume`/`SnackbarAnchor` bindings.

### 17.3 Verification

```text
python3 Misc/test_ui_contracts.py                  -> 225 checks passed, 0 failed
git diff --check                                   -> clean
dotnet build Seeker/Seeker.csproj -c "Debug Mock"   -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true
                                                   -> 0 Error(s), build succeeded
dotnet test UnitTestCommon/UnitTestCommon.csproj   -> new metadata/sorting tests pass
                                                      (full run: 16 pre-existing environment failures,
                                                       all require the unavailable SeekerEncryptKey)
```

### 17.4 Pending / limitations

* No device/emulator is available, so the redesigned rows and states were verified by resource
  compilation, static contracts and unit tests, not visually.
* Long-press reuses the existing download dialog (file selection + batch download) rather than
  adding a second result-level action mode; no batch logic was duplicated.
* The result rows show the file name as the primary line (folder remains in the metadata model
  and the row content description); a user-facing folder toggle was not added.
* New strings are English-only until Crowdin picks them up; the translation parity test tolerates
  missing keys.

---

## 18. Phase 4 — Transfers + remote browsing redesign (implemented slice)

The Transfers and Browse screens from §5.3 / §5.4 were reskinned together, reusing the
existing transfer engine, browse service, action modes and context menus. No service,
protocol/networking, persistence or transfer-scheduling code changed; the work is layouts,
view bindings and two narrow presenters.

### 18.1 Transfers — unified Downloads / Uploads

* **Clearly labelled tabs.** `layout/transfers.xml` now has a two-segment control
  (`transfersTabs` → `transfersTabDownloads` / `transfersTabUploads`) above the list. The tabs
  are 48dp targets, use `drawable/ds_cell_selector` (selected = `dsColorPrimaryContainer`) and
  `color/ds_segment_text` (selected `dsColorOnPrimaryContainer`, unselected
  `dsColorTextSecondary`). They call the same persisted switch as the toolbar item
  (`TransfersFragment.SetUploadsMode` → `ViewState.InUploadsMode`, `SaveTransferViewInUploadsMode`,
  `RefreshForModeSwitch`), so the tabs and `action_toggle_download_upload` can never disagree.
* **Compact rows.** `transfer_single_item.xml` / `transfer_folder_item.xml` keep
  name (and peer), status/queue, transferred-of-total size, progress bar, speed and ETA, and add
  an explicit `textViewPercent` label. ETA is shown *only* when meaningful: the engine's
  `GetRemainingTime()` is additionally gated by `TransferPresentation.MeaningfulRemainingTime`
  (in progress + finite, positive estimate), so queued/paused/finished rows show no countdown.
* **Visible per-row action.** Both row layouts add a 48dp `rowActionButton`
  (accessible `transfer_row_actions`). `TransferAdapterRecyclerVersion.WireRowAction` selects the
  row and opens the *existing* context menu (`ShowContextMenu` → `TransferViewHolder.OnCreateContextMenu`),
  so pause/resume, retry/resume, cancel-and-remove, refresh queue, play, and the user submenu
  (browse / browse at location / message / note / privileges …) stay in one implementation. The
  button is hidden in batch mode.
* **Batch selection preserved.** `IsInBatchSelectMode`, the selection checkbox, action mode,
  `ToggleItemBatchSelect` and the batch menus are unchanged.
* **No new whole-list work on progress.** The existing `TransferEventRouter.ProgressUpdated`
  throttle (`THROTTLE_PROGRESS_UPDATED_RATE`), per-row `refreshItemProgress` path and
  `TransferDebouncer` are retained; the percent label is updated on the same single-row refresh.
  Status/chip/percent colours continue to resolve through the (day/night) transfer colour
  resources and the row/percent/accent chrome now uses `dsColor*` tokens.

### 18.2 Browse — remote browsing

* **Username/status header.** `layout/browse.xml` adds `browseUserHeader` with
  `browseUserHeaderName` (username) and `browseUserHeaderStatus` (`browse_header_stats`,
  folders/files from the existing `BrowseState.Stats`), updated from the state the browse
  response already holds — no extra network/disk work.
* **48dp breadcrumb.** `tree_path_item_view.xml` nodes are `ds_touch_target_min` high/wide,
  clickable and token-tinted; `recyclerViewHorizontalPath` reserves the 48dp height. Breadcrumb
  click → existing `GoUpDirectory`, back → existing `BrowseFragment.BackButton`, and the folder
  state/scroll restore are unchanged.
* **Distinctive rows + metadata.** `browse_response_item.xml` rows are ≥48dp; files keep the
  existing size • duration • bitrate line, and folders now show a cheap secondary line
  (`browse_folder_meta`: immediate file count + child folder count) from fields already in the
  response. No recursive per-row walk, so large folders stay a RecyclerView.
* **Obvious download affordance.** Each row adds a 48dp `downloadButton` (`browse_download_action`).
  Files enqueue directly through the existing `DownloadService.EnqueueFilesFireAndForget`; folders
  call the existing `DownloadUserFilesEntry(false, false, …)`, so the existing subfolder/filter
  safety prompt is the only dialog added. The existing file/folder overflow menus (download,
  queue-as-paused, folder info, copy URL) and the peer actions are retained.
* **Multi-select preserved.** Long-press batch mode, `BrowseActionMode`, `ToggleBatchSelect`,
  `DownloadSelectedFiles` and the batch menu are unchanged; download/overflow act as selection
  toggles while in batch mode.

### 18.3 Tests and contracts

* `Common/Transfers/TransferPresentation.cs` is a narrow, Android-free presenter
  (`ProgressPercent`, `MeaningfulRemainingTime`, `ShouldShowEta`) bound by the row views.
* `UnitTestCommon/TransferPresentationTests.cs` (9 tests) covers byte-derived / terminal /
  clamped / unknown-size percent and the ETA in-progress / not-in-progress / zero / negative /
  null cases.
* `Misc/test_ui_contracts.py` adds `check_transfers_ui` and `check_browse_ui`: tab IDs/labels/
  48dp, row percent + 48dp action IDs and descriptions, binding symbols, retained throttle/
  router/debouncer, browse header IDs, 48dp breadcrumb/rows/download, folder metadata, and the
  preserved peer/batch actions and strings.

### 18.4 Verification

```text
python3 Misc/test_ui_contracts.py                  -> 365 checks passed, 0 failed
git diff --check                                   -> clean
dotnet test UnitTestCommon/UnitTestCommon.csproj   -> 622 passed, 0 failed, 1 skipped
                                                      (filter excludes RealUserParsing/ImportQT/keywords)
dotnet build Seeker/Seeker.csproj -c "Debug Mock"   -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true
                                                   -> 0 Error(s), build succeeded
```

### 18.5 Pending / limitations

* No device/emulator is available, so the tabs, rows, breadcrumbs and download affordance were
  verified by resource compilation, static contracts and unit tests — **not** visually.
* The per-row action opens the existing context menu rather than a bespoke action sheet; that
  keeps the supported operations in one place but the menu is presented as a platform context
  menu at the row.
* Folders show immediate file/child-folder counts (cheap), not a recursive size/duration sum.
* New strings (`transfer_*`, `browse_header_stats`, `browse_download_action`, `browse_row_actions`,
  `browse_folder_meta`, `transfers_tabs_description`) are English-only until Crowdin picks them up.

---

## 19. Phase 5 — Messaging, chatrooms, settings and remaining surfaces (implemented slice)

### 19.1 Implemented

* **Messages — conversation list** (`messages_overview.xml`, `message_overview_item.xml`,
  `MessagesOverviewRecyclerAdapter.cs`): compact two-line hierarchy (peer on top; last message
  below), right-aligned timestamp, unread badge, cached presence dot (only when the cached user
  list knows it), 48dp row target and an accessible peer summary (peer, time, message, unread
  count, presence). Swipe-to-delete now paints the semantic `dsColorError` instead of a literal
  red. Existing single-tap/long-tap, batch selection, unread refresh and swipe undo are unchanged.
* **Messages — thread** (`messages_inner_item_fromMe/toMe`, `group_messages_inner_item_toMe`,
  `messages_inner_layout.xml`, `MessagesInnerRecyclerAdapter.cs`): themed incoming/outgoing
  bubbles (`dsColorSurfaceContainer` / `dsColorPrimaryContainer`), token text/timestamp colours,
  failed sends using `dsColorError`/`dsColorOnError`, and 48dp compose field + send button.
  Links/auto-link, long-press copy/menu and the single send path (button, IME Send, Enter) are
  retained. `WindowSoftInputMode=AdjustResize` plus the existing `BottomOnlyInsetsListener`
  (which already reads `WindowInsetsCompat.Type.Ime()`) keep the composer above the keyboard.
* **Chatrooms** (`chatroom_inner_layout.xml`, `chatroom_overview_*`, `room_user_list_item.xml`,
  `room_users_dialog.xml`, `ChatroomAdapters.cs`): same bubbles/composer tokens and 48dp targets,
  themed category/header, readable room rows, accessible leave/member/status controls. Join,
  create, leave/ownership, member list/sort/search, tickers and the overflow room actions are all
  preserved (verified by static contracts).
* **Settings** (`SettingsCatalog.cs`, `SettingsActivity.Host.cs`, `row_setting_*.xml`): the
  searchable/data-driven catalog is regrouped into **Account & connection, Downloads, Uploads &
  sharing, Search & wishlist, Notifications, Appearance, Privacy, Advanced, About** while keeping
  every row id, preference setter, `DependentRowIds`, live status ticker and SAF picker. Technical
  rows gained concise secondary text; rows/buttons are 48dp+; the About category opens the
  existing About sheet. The sharing deep-link scrolls to the new `h.uploads_sharing` header.
  The theme selector still offers Follow system / Always light / Always dark and Modern remains
  the per-theme default (unchanged defaults are asserted by the existing contracts).
* **Login / users / sharing dialogs** (`login_viewflipper.xml`, `view_user_info_layout.xml`,
  `edit_user_info_layout.xml`, `user_row.xml`, `user_note_dialog.xml`, `give_privileges_layout.xml`,
  `RecyclerUserListAdapter.cs`, `ViewUserInfoActivity.cs`): dashboard/status colours now come from
  `dsColorPrimary`/`dsColorSuccess`/`dsColorError`; dialog inputs use `Seeker.TextInputLayout`;
  user rows have clearer type hierarchy and a 48dp target; presence/status icons expose content
  descriptions. No sharing behaviour changed.
* **Remaining UI literals**: every `layout/*.xml` now has zero `#RRGGBB` literals. The download
  dialog header and wishlist header use chrome/on-primary tokens (fixing white-on-white in
  ModernLight), FABs use `dsColorPrimary`/`dsColorOnPrimary`, and the chatroom category uses
  on-primary. Notification-builder black/white defaults were intentionally left (Android
  notification constraint); brand/vector assets were not recoloured.
* **Browse / transfer follow-ups**: the browse header now appends the *cached* user presence
  (Online/Away/Offline) when `CommonState.UserList` knows it and never probes the network; the
  header shows totals only when presence is unknown. Browse tap handlers validate positions against
  the adapter's current `localDataSet.Count`. `TransferItemViews` passes the explicit
  `showTimeRemaining` in-progress state to `TransferPresentation.MeaningfulRemainingTime`.

### 19.2 Static contracts and unit tests

Added to `Misc/test_ui_contracts.py`: settings category header order/ids, preserved row ids,
dependent rows/status tickers/SAF pickers, 48dp row minimums, message list/bubble/compose tokens,
IME handling, preserved chatroom actions, zero layout colour literals, scoped C# literal-colour
scan (notification exception documented), browse cached-status + bounded positions and the
explicit transfer ETA state. `TransferPresentationTests` gained a default-overload `inProgress`
test.

### 19.3 Verification (this phase)

```text
python3 Misc/test_ui_contracts.py                  -> 494 checks passed, 0 failed
git diff --check                                   -> clean
dotnet test UnitTestCommon/...                     -> 623 passed, 1 skipped, 0 failed
dotnet build Seeker/Seeker.csproj -c "Debug Mock"  -> 0 Error(s), build succeeded
```

### 19.4 Pending / limitations

* No device/emulator is available. All changes were verified by resource compilation, static
  contracts and unit tests — **not** visually. Bubble contrast, keyboard animation and two-line
  row clipping still need a device pass.
* Presence in the browse header and message list is shown only when the cached user list has it;
  an uncached peer shows no status rather than a guessed one.
* Notification colours remain Android notification-compatible literals by design.
* New English-only string keys introduced in this phase (English fallback until Crowdin):
  `section_account_connection`, `section_uploads_sharing`, `section_search_wishlist`,
  `section_privacy`, `section_advanced`, `setting_subtitle_subfolders`,
  `setting_subtitle_no_single_subfolder`, `setting_subtitle_override_incomplete`,
  `setting_subtitle_auto_retry`, `setting_subtitle_port`, `setting_subtitle_upnp`,
  `setting_subtitle_metered`, `setting_subtitle_vpn`, `setting_subtitle_auto_away`,
  `setting_subtitle_private_invites`, `setting_subtitle_concurrent`,
  `setting_subtitle_diagnostics`, `setting_subtitle_day_night`, `setting_subtitle_about`,
  `browse_header_status`, `leave_room_desc`, `room_unread_desc`.
* The About *category* is a real navigation row into the existing About sheet; it does not
  duplicate version/source/translation rows in the settings list.



---

## 20. Phase 6A — Search functional gaps and targeted accessibility polish (implemented slice)

### 20.1 Sorting — filename and representative-file size

* `Common/SearchEnums.cs` appends `SearchResultSorting.Filename = 4` and
  `SearchResultSorting.FileSize = 5`. The existing ids (`Available = 0`, `Fastest = 1`,
  `FolderAlphabetical = 2`, `BitRate = 3`) are unchanged, so a persisted
  `DefaultSearchResultSortAlgorithm` value keeps its meaning.
* `Common/Search/SearchResultComparable.cs` keeps the existing availability tie-break for both
  new options. **Representative semantics** (documented in the comparer and tests):
  * the representative file is the first **unlocked** file, falling back to the first locked
    file; this is independent of the hide-locked preference because it is only an ordering key;
  * `Filename` compares only the representative file's file name (not its folder path), is
    case-insensitive first, then uses an ordinal tie-break so the order is deterministic;
  * `FileSize` sorts descending by the first representative file's size. It is **not** a group
    aggregate and computes no sum, so each comparison is O(1); a row with no file sorts last.
  * Empty responses, mixed locked/unlocked rows and equal keys fall through to the existing
    availability chain (unlocked, free slot, queue, speed, representative path, username).
* The in-search sort dialog (`changeresultsortorder.xml` → `fileName`, `fileSize`), the result
  header (`SearchSortLabels`), and the new settings row `search.default_sort`
  (`SettingsCatalog`/`SettingPickers`, all six options) share one label source. The header
  therefore shows the selected option instead of falling back to “Availability”.
* `UnitTestCommon/SearchResultComparableTest.cs` adds filename/size tests for path-vs-name,
  case, locked fallback, mixed rows, empty rows, first-file size semantics and 10,000-result
  ordering. The 10,000 tests assert order, not a measured benchmark.

### 20.2 Cross-result multi-selection

* `Common/Search/SearchSelection.cs` is an Android-free selection state keyed by
  `Username + unit separator + representative filename` (`SearchResultIdentity`), never by
  adapter position. `Toggle`, `SelectAll`, `PruneTo`, `Resolve` and `Clear` are covered by
  `UnitTestCommon/SearchSelectionTests.cs` (reorder stability, locked fallback, prune, visible
  resolve, 10,000 identities).
* Long-press on a result row enters the contextual `SearchActionModeCallback`
  (`Seeker/Search/SearchActionMode.cs`, menu `search_batch_menu.xml`). While active, row taps
  toggle the row, every row shows a check-circle indicator, and the action mode title shows the
  selected count. `select_all` selects the current visible list. **Download selected** / queue
  paused use the existing `DownloadService.EnqueueFilesFireAndForget` via
  `SessionService.RunWithReconnect`, grouping selected rows by each result's own username and
  honouring `PreferencesState.HideLockedResultsInSearch`. The batch path reuses
  `SearchResponse.GetFiles` + `BrowseUtils.GetFullFileInfos`, the same helpers as the per-result
  `DownloadDialog`; no second file picker or enqueue implementation was added.
* Lifecycle: selection is pruned when a filter hides rows, preserved across sorting and ordinary
  streaming additions, and cleared with the action mode on tab switch or search replacement
  (`ClearTab`). Back/cancel in the action mode clears it. The per-result download dialog,
  row download button, peer actions and Existing stream update strategy are unchanged.
* The row bind only applies the already-computed checkbox state and appends “Selected” to the
  accessibility description; no per-arrival O(n) scan was added.

### 20.3 Accessibility / font-scale polish in touched layouts

* All five result variants gained a 24dp `selectionCheckbox` inside a 48dp-tall primary row;
  primary row text remains 14sp (file/peer identity) and metadata remains 12sp, so larger font
  scales can wrap (no fixed row height). The sort dialog uses wrapping radio rows. No fixed
  48dp text input was introduced; existing search/filter inputs already use
  `minHeight=ds_touch_target_min` + `wrap_content`.
* The selected state is both visual (filled check circle) and announced (“…, Selected”).
* `Misc/test_ui_contracts.py` now asserts the persisted enum ids, representative sort semantics,
  shared labels/default-sort settings row, selection state symbols, action-mode menu/bindings,
  clear/prune lifecycle, row indicators and the new Maestro flow.

### 20.4 Verification (this slice)

```text
python3 Misc/test_ui_contracts.py                  -> 580 checks passed, 0 failed
git diff --check                                   -> clean
dotnet test UnitTestCommon/UnitTestCommon.csproj --filter '...'
                                                   -> 651 passed, 1 skipped, 0 failed
dotnet build Seeker/Seeker.csproj -c "Debug Mock"   -> 0 Error(s), build + signed APK OK
```

### 20.5 Pending / limitations

* No emulator/Maestro CLI is available, so `MaestroTests/search_selection_sort.yaml` and the
  expanded rows were **not executed**; the flow requires a live search that returns results.
* `File size` means the **representative first file**, not the sum of the group. That is
  intentional (O(1), no cached aggregate invalidation) and is stated in the sort label/docs;
  an aggregate size column would need a cached per-result sum.
* Filename sorting uses the file name only, so two rows with the same representative file name
  are ordered by the availability chain (then path/username), not by folder name.
* New strings (`Filename`, `FileSize`, `default_search_sort`,
  `keywords_default_search_sort`, `search_result_selected_state`) are English-only until
  Crowdin picks them up.
* Maestro selection uses the English action-mode text (“1 selected”, “Download selected”,
  “More options”); it will need translated selectors or ids if run in another locale.

---

## 21. Final integration/polish (this task)

Implemented and source/build verified; runtime appearance and Maestro execution remain
unverified because this host has no emulator, no `/dev/kvm` and no Maestro CLI. The complete
checklist, APK metadata and reproducible commands live in `docs/UI_REDESIGN_DELIVERY.md`.

* **Search action-mode ownership.** `MainActivity.OnPagerPageSelected` now finishes
  `SearchSelection` whenever the pager leaves Search (including page 0 account and deep-link
  page switches), alongside the existing Transfers/Browse/Messages finishes.
  `SearchFragment.OnDestroyView` clears the selection and drops the action-mode reference so a
  destroyed view cannot own a mode. Ordinary sorting (`UpdateSortAvailability`) and streaming
  (`ApplySearchResults`) do not call the clear path; a new search (`ClearTab`) and tab switches
  still do.
* **Contextual download action.** Search rows hide `downloadActionButton` while selecting and a
  stale tap toggles the row instead of opening the per-result download dialog (Browse already
  hid it and toggled).
* **Large-font row/input limits.** Touched search/transfer/message-timestamp/browse rows keep
  primary text >= 14sp and metadata/timestamp text >= 12sp. Message and chatroom text inputs
  use `wrap_content` + `minHeight=48dp` (send icons stay fixed 48dp). The
  `messages_inner_layout` message recycler is `0dp` + weight 1 above the compose bar.
* **Static/Maestro contracts.** `Misc/test_ui_contracts.py` adds font-size, input-height,
  recycler-weight, search-lifecycle, contextual-action and Maestro-flow contracts. Maestro
  coverage now includes the existing light+dark screenshot matrix (Search/Transfers/Browse/
  Messages/Settings/account/Chatrooms/User info), follow-system via `setDarkMode`/
  `assertDarkMode`, activity recreation/background/tab preservation via `setOrientation` and
  `launchApp: stopApp: false`, and a large-font sample. The sample uses only supported Maestro
  commands; Maestro has no font-scale command, so the device font scale is an external
  documented precondition. No flow was executed here.


---

## 22. Colour-semantics repair and Maestro automation validity (final review pass)

Two defect classes were found in the previous pass and fixed here; both are now guarded by
contracts rather than by comments.

### 22.1 The bug: legacy chrome attrs used as accents

`mainPurple` is **chrome** (app-bar / status-bar / bottom-navigation background). The modern
themes resolve it to a surface: `ModernLight` -> `?attr/dsColorChromeSurface` = `#FFFFFF`,
`ModernDark` -> `#1B222C`. Anything that painted *with* `mainPurple` in the modern themes
therefore painted chrome-on-chrome:

| Site | Before | After |
| --- | --- | --- |
| `layout/searches.xml` (linear + circular), `layout/chatroom_inner_layout.xml`, `layout/chatroom_overview.xml` (x2), `layout/all_ticker_dialog.xml` | `app:indicatorColor="?attr/mainPurple"` -> invisible spinner/progress in both modern themes | `app:indicatorColor="?attr/dsColorPrimary"` |
| `SearchFragment.GetSegmentedButtonBgTint` | selected = `mainPurple` (white in ModernLight), unselected = `dialog_background` | selected = `dsColorPrimary`, unselected = `dsColorSurface` |
| `SearchFragment.GetSegmentedButtonTextTint` | selected = literal `Color.White` -> white-on-white selected filter in ModernLight | selected = `dsColorOnPrimary`, unselected = `dsColorTextPrimary` |
| `drawable/search_tab_pill_active.xml`, `drawable*/search_tab_row_bg.xml` | active-tab pill / row highlight filled with the chrome colour (invisible in ModernLight) | `dsColorPrimary` |
| `drawable*/chip_background_state_color.xml` | checked state = chrome colour | `dsColorPrimary` |
| `ModernLight.dsColorTextDisabled` | `#8C97A3` (2.97:1 on white, below the 3:1 floor) | `#858F9B` (3.28:1 on surface, 3.06:1 on background) |

Retained on purpose (genuine chrome backgrounds, paired with `dsColorChromeForeground`):
`settings_layout`, `user_list_activity_layout`, `edit_user_info_layout`,
`wizard_activity_layout`, `view_user_info_layout`, `messages_main_layout`,
`chatroom_main_layout`, `wishlist_header`, `downloaddialog` (sheet header tint). The legacy
palettes (`DefaultLight*`, `DefaultDark*`, `Amoled*`) were **not** touched: there `mainPurple`
*is* the accent, and their palette values are preserved.

### 22.2 The contracts added

`Misc/test_ui_contracts.py` gained two check groups (640 -> 733 checks; the semantic-theme
cycle repair in §22.5 takes the suite to 815):

* `colour.chrome_context` / `colour.chrome_drawable` / `colour.chrome_code` — a chrome attr may
  only be a layout `background`/`backgroundTint` or a shape `solid`/`stroke`; never a
  `textColor`/`tint`/`indicatorColor`/`trackColor`, never a state-list item, and never resolved
  from C# at all.
* `colour.control_visible` — every attr referenced by `indicatorColor`/`trackColor`/
  `progressTint`/`indeterminateTint`/`buttonTint` in a layout is resolved through the theme and must
  reach 3:1 against `dsColorBackground` in both modern themes (`mainPurple` scores 1.07:1 in
  `ModernLight` and 1.16:1 in `ModernDark` - the bug is caught by arithmetic, not by a name).
* `colour.filter_selected` — the two tint method bodies must reference
  `dsColorPrimary`/`dsColorSurface` and `dsColorOnPrimary`/`dsColorTextPrimary` and no chrome
  attribute / literal white.
* `colour.chrome_meaning` — records that `mainPurple` *is* the chrome surface in the modern
  themes (otherwise "chrome on chrome" would silently become acceptable again).
* `colour.contrast` — real WCAG ratios for pairs **resolved through the theme** (`?attr/` chains
  and `@color/` references are followed to concrete RGB, so a token that silently becomes a
  surface colour fails): primary/on-primary, text/surface, text/background, text-secondary/
  surface, disabled/surface (>= 3:1), selected filter label/background, unselected filter
  label/background, chrome foreground/surface, action-mode foreground/background, navigation
  selected item, primary indicator on background and surface, error/surface.
* `colour.accent_on_surface` + `colour.selector` — every accent attribute
  (`dsColorPrimary`, `colorPrimary`, `colorAccent`, `colorControlActivated`, `slskAccent`,
  `searchTabButtonColor`, `mainTextColor`, `checkBoxColor`, `swipeToRefreshProgress`,
  `dsColorAccent`) and every item of every `res/color/` selector stays at >= 3:1 on the surface
  and on the background in both modern themes.

### 22.3 Maestro: evidence instead of assumption

The previous pass claimed `setDarkMode`/`assertDarkMode` were "supported Maestro commands"
without evidence, and the static test then asserted its own claim (circular). Replaced by:

* `MaestroTests/schema/maestro_commands.json` — a checked-in copy of Maestro's command vocabulary
  with provenance: the 56 keys are the properties of `YamlFluentCommand` in
  `maestro-orchestra/src/main/java/maestro/orchestra/yaml/YamlFluentCommand.kt`
  (`val setDarkMode`, `val assertDarkMode`, `val assertLightMode`, ...), the value enums come
  from `DarkModeValue`/`AirplaneValue`/`DeviceOrientation`, and the record cites
  `https://docs.maestro.dev/llms.txt`, the per-command doc pages and the retrieval date.
  Conclusion: `setDarkMode: enabled|disabled`, `toggleDarkMode`, `assertDarkMode`,
  `assertLightMode` are real and are what `follow_system_theme.yaml` uses; `setFontScale`,
  `setTheme` and `setLocale` are **not** Maestro commands and are recorded as such.
* `check_maestro_command_schema` — parses every `MaestroTests/**/*.yaml` with PyYAML, then
  validates each command key, the value grammar of the closed-value commands, `runFlow` target
  existence and that every flow's `appId` is the manifest package. PyYAML >= 6 is a declared
  tool dependency (`python3 -m pip install 'PyYAML>=6'`); when it is unavailable the suite fails
  with that command instead of passing on a regex-only fallback. No runtime claim.
* `Misc/run_maestro_theme_matrix.sh` — the external-precondition path:
  `adb shell cmd uimode night yes|no` (verified by reading the mode back), then
  `maestro test --test-output-dir … -e IS_NIGHT=… -e PREFIX=…` in each appearance, restoring the
  original mode on exit. Supplying `-s SERIAL` forwards the serial to both `adb -s` and
  `maestro test --device`. This path needs no in-flow command at all.

### 22.4 Verification (this pass)

```text
python3 Misc/test_ui_contracts.py                  -> 815 checks passed, 0 failed
git diff --check                                   -> clean
dotnet test UnitTestCommon --filter '...'           -> 651 passed, 1 skipped, 0 failed
dotnet build Seeker/Seeker.csproj -c Debug          -> 311 Warning(s), 0 Error(s)
dotnet build Seeker/Seeker.csproj -c 'Debug Mock'   -> 311 Warning(s), 0 Error(s)
```

Mutation-checked (each defect reintroduced on purpose, then reverted): reverting one
`indicatorColor` to `?attr/mainPurple` fails `colour.chrome_context` and both
`colour.control_visible` checks; reintroducing `mainPurple`/`Color.White` in the filter tints
fails `colour.chrome_code` and `colour.filter_selected`; adding `- setFontScale: 1.3` to a flow
fails `maestro.schema`.

### 22.5 Legacy light theme background cycle (semantic-theme repair round)

`AppTheme.dsColorBackground` used to be `?android:attr/colorBackground`, while the same style
mapped `android:colorBackground` from `?attr/dsColorBackground`. `ModernLight`/`ModernDark` and
`DefaultDark`/`Amoled` override the token, but the four legacy light themes (`DefaultLight`,
`DefaultLight_Blue`, `DefaultLight_Red`, `DefaultLight_Grey`) do not, so every saved legacy
light choice inherited a self-reference. The base default is now the concrete
`@color/ds_default_background` (`#FFFBFE` day, `#1C1B1F` night - the values the Material3 parent
already supplied); the legacy palettes and stored preference ints are unchanged.

Contracts added (733 -> 815 checks):

* `themes.cycles` — walks every locally defined style item in all 12 concrete themes and fails on
  any cycle, including when the chain would otherwise end at an external Material/framework
  attribute. `?android:attr/x` is normalised to the local `android:x` item, so the historic
  `dsColorBackground -> android:colorBackground -> dsColorBackground` cycle is caught even though
  `android:colorBackground` itself is not a custom attr.
* `themes.legacy_tokens` — resolves the window background plus primary, surface, text and chrome
  tokens (`android:windowBackground`, `dsColorBackground`, `dsColorLaunchBackground`,
  `dsColorPrimary`, `dsColorSurface`, `dsColorTextPrimary`, `dsColorChromeSurface`,
  `dsColorChromeForeground`) for each of the 10 legacy variants, not just `ModernLight`/
  `ModernDark`.

Mutation-checked: restoring `?android:attr/colorBackground` as the base `dsColorBackground`
fails `themes.cycles` for all four legacy light themes. Reverting the runner's `-s` handling
fails no check (it is a device-run script); `bash -n Misc/run_maestro_theme_matrix.sh` and
`git diff --check` are clean.

### 22.6 Remaining gaps (honest list)

* **Nothing here is runtime verified.** No emulator, no `/dev/kvm`, no Maestro CLI on this host:
  no screenshot was produced, no flow was executed. The colour work is verified by resolved
  contrast arithmetic and resource compilation, not by looking at the app.
* The contracts cannot see *composition*: an accent painted on an accent (two tokens that are
  each fine alone) is still invisible to them.
* Legacy-theme visual parity is asserted only for structure, not for contrast: their disabled /
  alpha colours (`#80ffffff` etc.) are below 4.5:1 by design and were deliberately frozen.
* 96 drawables keep literal colours (see §4.5); `res/color/segmented_button_bg.xml`,
  `drawable/rounded_bottom_{left,right}_purple.xml` and `@color/mainPurple{Transparent,TurnedOff}`
  are dead leftovers.
* The Maestro schema is a snapshot: it has to be re-diffed against upstream whenever the pinned
  Maestro version changes (the check says so in its failure message, the schema records the URLs).
