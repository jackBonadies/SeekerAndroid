# Seeker UI overhaul — delivery summary

Whole-overhaul delivery record (Phases 2A–6A + final integration + the colour-semantics and
automation-validity repair of §6, plus the final semantic-theme cycle repair). The design
inventory, per-phase detail and every contract are in `docs/UI_REDESIGN.md`; §22 there covers
the last repair passes in line-level detail.

**Status: source, static contracts, unit tests and both requested APK builds verified.
Runtime/visual verification is not possible on this host** (no emulator, no `/dev/kvm`, no
Maestro CLI). No runtime, screenshot or "flows pass" claim is made anywhere in this document.

---

## 1. What the overhaul actually is (architecture)

The app had per-screen duplicated themes, forced light/dark literals and an untokenised palette.
The overhaul replaced that with one layer, and then rebuilt each surface on it.

| Layer | Where | What it does |
| --- | --- | --- |
| Config-dependent base | `Resources/values/styles.xml` (`Seeker.Theme.Base`, the only style redefined in `values-night/`) | one Material3 parent per configuration, so every shared item is written once |
| Token layer | `AppTheme` in `styles.xml` + concrete themes in `themes.xml` | 41 `dsColor*`/`dsShape*` attributes; Material3 roles (`colorPrimary`, `colorSurface`, `colorOutline`, `colorControl*`, `android:textColor*`) are all derived from tokens, so unstyled widgets follow the theme instead of inheriting a light palette |
| Legacy compatibility | same styles | every legacy attribute (`mainPurple`, `cellback`, `cellTextColor*`, `dialog_background`, …) is mapped onto tokens, so 106 existing layouts/drawables follow automatically and stored theme preferences keep their enum values and palettes (legacy light window backgrounds now resolve through a non-recursive token default instead of a self-reference) |
| Primitives | `Resources/drawable/ds_*` (7), `Resources/color/ds_*` (2), `values/dimens.xml` + typography styles | token-driven cell/input/chat/sheet/search-bar backgrounds, cell + navigation + segment state lists, `ds_touch_target_min` 48dp, `TextAppearance.Seeker.*` |
| Themes | `values/themes.xml` | 12 concrete themes: legacy `DefaultLight*`/`DefaultDark*`/`Amoled*` keep their stored preference ints and palettes (behaviour compatibility, not a byte-for-byte freeze: the shared token layer supplies a non-recursive window background), plus `ModernLight`/`ModernDark` (new default, follows the system appearance) |
| Shell | `Seeker/Main/MainNavigation.cs`, `MainActivity.Navigation.cs`, `menu/shell_menu.xml`, `ds_nav_item_tint.xml` | bottom navigation in the fixed order Search · Transfers · Browse · Messages, account/login moved to secondary navigation, page identities pinned (`NAV_PAGE_IDENTITIES`) |
| Shared helpers | `UiHelpers`, `ThemeHelper`, `PreferencesManager` | attribute resolution helpers, enum ↔ theme mapping, persisted `DayNightMode`/`NightVariant`/`DayVariant` + "Follow system" default |

Surfaces rebuilt on the layer (each with its own section in `docs/UI_REDESIGN.md`):

* **Search** (§17, §20, §21) — unified result row variants with a checkbox, chip/filter rows,
  filename + representative-size sorting with a persisted default, cross-result multi-selection
  (`Common/Search/SearchSelection.cs`, `SearchResultMetadata.cs`) and a contextual action mode
  with batch download.
* **Transfers** (§18) — one screen with Downloads/Uploads segmented tabs, unified action wiring,
  throttled progress updates, folder rows and a segmented progress bar.
* **Browse** (§18) — remote browsing header (user/status), 48dp breadcrumb path, row metadata and
  the download affordance hidden while selecting.
* **Messages / Chatrooms / Settings / Login / Users** (§19) — 2-level Messages,
  compose dialog, chatroom overview + inner chat, grouped settings catalog with pickers and value
  formatting, login surface and user info/edit/list.
* **Dialogs, sheets, popups, menus** — themed once through `materialAlertDialogTheme`,
  `bottomSheetDialogTheme`, `popupMenuStyle`, `Seeker.Snackbar.Action` instead of per screen.

## 2. Verification model

Three independent gates; each catches a different class of mistake.

| Gate | Command | Catches |
| --- | --- | --- |
| Static UI contracts | `python3 Misc/test_ui_contracts.py` | unresolved `?attr/`, missing tokens, theme-attribute cycles (including legacy themes), legacy-token resolution, style drift, cross-config duplicates, non-token colours, layout/typography/touch-target rules, C# `Resource.*` ids that do not exist, Maestro command validity, resolved contrast ratios |
| Unit tests | `dotnet test UnitTestCommon/…` | pure logic: search comparables/sorting, selection set semantics, result metadata, transfer presentation, theme/state helpers |
| Builds | `dotnet build Seeker/… -c Debug` and `-c 'Debug Mock'` | aapt2 resource errors (an unresolved `?attr/` is a link error), C# binding errors, APK packaging |

815 static checks, 652 unit tests passing/1 skipped under the gate (668 total, 16
environmentally-gated real-data/secret failures), 2 APK builds. Nothing below claims more than
that.

## 3. Exact reproducible commands

Run from `/home/void/SeekerAndroid`.

Requirements: Python 3 and PyYAML >= 6 (`python3 -m pip install 'PyYAML>=6'`).
The static suite fails explicitly when PyYAML is unavailable; it is not silently skipped.

```bash
python3 Misc/test_ui_contracts.py
git diff --check

PATH="$HOME/.dotnet:$PATH" dotnet test UnitTestCommon/UnitTestCommon.csproj --nologo -v:q \
  --filter 'FullyQualifiedName!~TestRealUserParsingBrowseResponseTestData&FullyQualifiedName!~ImportQTConfig&FullyQualifiedName!~Keywords_RealData'

PATH="$HOME/.dotnet:$PATH" dotnet build Seeker/Seeker.csproj -c Debug \
  -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true --nologo -v:q

PATH="$HOME/.dotnet:$PATH" dotnet build Seeker/Seeker.csproj -c 'Debug Mock' \
  -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true --nologo -v:q
```

Independent final verification (re-run against the delivered working tree, not the originally
recorded numbers):

```text
python3 Misc/test_ui_contracts.py
  -> 815 checks passed, 0 failed

git diff --check
  -> clean (no whitespace errors)

bash -n Misc/run_maestro_theme_matrix.sh
  -> clean (no shell syntax errors)

dotnet test UnitTestCommon/UnitTestCommon.csproj --filter '<non-secret filter above>'
  -> Passed! - Failed: 0, Passed: 651, Skipped: 1, Total: 652

dotnet test UnitTestCommon/UnitTestCommon.csproj   # full, unfiltered
  -> Failed: 16, Passed: 651, Skipped: 1, Total: 668

dotnet build Seeker/Seeker.csproj -c Debug ...
  -> 158 Warning(s), 0 Error(s)

dotnet build Seeker/Seeker.csproj -c 'Debug Mock' ...
  -> 158 Warning(s), 0 Error(s)

adb devices
  -> empty (no device attached)
```

The 16 unfiltered failures are the excluded real-data/encrypted suites (`ImportQTConfig`,
`Keywords_RealData`, `TestRealUserParsingBrowseResponseTestData`). The Browse failure is the
unchanged secret-dependent path; the exact error is
`Encryption key not set. Please set the SeekerEncryptKey environment variable to a non-empty value.`
These failures are environmental (no `SeekerEncryptKey`) and are the reason the reproducible unit
gate applies the filter above; they are not source defects.

Warning counts differ between a clean rebuild and an incremental run: the earlier record re-emitted
all 311 warnings per configuration on a clean build, whereas this independent verification ran
incrementally and re-emitted only 158 warnings per configuration (incremental compilation reuses
unchanged object outputs and does not replay their warnings). Both are 0 errors; the count is a
property of incremental vs. clean compilation, not of the delivered source. No build was re-run for
this bookkeeping pass because a rebuild would change the recorded APK hashes below.

## 4. Built APK metadata (current on-disk artifacts)

Hashes/sizes read directly from the on-disk artifacts delivered by the verified builds (not
regenerated for this bookkeeping pass, so a later rebuild would change them):

| Configuration | APK | Size (bytes) | SHA-256 |
| --- | --- | ---: | --- |
| Debug | `Seeker/bin/Debug/net10.0-android36.0/com.companyname.andriodapp1-Signed.apk` | 156,047,762 | `54b54fcf8ccb0b94ca3402de3c7a002443517aea32b44f8196fd1ab4b91c1d8a` |
| Debug | `Seeker/bin/Debug/net10.0-android36.0/com.companyname.andriodapp1.apk` (unsigned) | 155,790,912 | `384a3a331b8de8ef62ac0d9259dabcf7c6eecb7ea5b91c77bc355661c0124916` |
| Debug Mock | `Seeker/bin/Debug Mock/net10.0-android36.0/com.companyname.andriodapp1-Signed.apk` | 115,607,643 | `3695dba188a1909f8054e733efeefc2faf1364a2081353fae08e37ad458f4298` |
| Debug Mock | `Seeker/bin/Debug Mock/net10.0-android36.0/com.companyname.andriodapp1.apk` (unsigned) | 115,493,936 | `174b8f48bf9aff94de5f35d0673c25a9eaa1cf57e1a4d4aa6527e83d0b492092` |

Manifest/APK identity (unchanged by this pass, read back with
`aapt2 dump badging`): `package=com.companyname.andriodapp1`, `versionCode=144`,
`versionName=3.11.2`, `compileSdk=36`, `minSdk=23`. The APKs were built but **not installed or
launched** (no device/emulator; `adb devices` was empty).

## 5. What this final pass fixed (defects, not cosmetics)

Three concrete defect classes found by review, all now guarded by arithmetic/evidence
instead of comments (the third is this final semantic-theme repair round):

1. **Legacy chrome attribute used as an accent.** `mainPurple` is the app-bar/status-bar/
   navigation background. `ModernLight` resolves it to `#FFFFFF` and `ModernDark` to `#1B222C`,
   so anything painting *with* it painted chrome-on-chrome:
   * `searches.xml` (linear + circular), `chatroom_inner_layout.xml`, `chatroom_overview.xml`
     (x2) and `all_ticker_dialog.xml` had `app:indicatorColor="?attr/mainPurple"` — an invisible
     spinner/progress in both modern themes -> now `?attr/dsColorPrimary`.
   * `SearchFragment` resolved `mainPurple` as the selected-filter background and hard-coded
     `Color.White` as the selected-filter label — white-on-white in `ModernLight` -> now
     `dsColorPrimary` + `dsColorOnPrimary` selected, `dsColorSurface` + `dsColorTextPrimary`
     unselected.
   * `search_tab_pill_active`, `search_tab_row_bg` (day + night) and
     `chip_background_state_color` (day + night) were filled with the chrome colour -> now
     `dsColorPrimary`.
   * `ModernLight.dsColorTextDisabled` `#8C97A3` was 2.97:1 on white (under the 3:1 floor) ->
     `#858F9B` (3.28:1 on surface, 3.06:1 on background).
   * Genuine chrome backgrounds (`settings_layout`, `user_list_activity_layout`,
     `edit_user_info_layout`, `wizard_activity_layout`, `view_user_info_layout`,
     `messages_main_layout`, `chatroom_main_layout`, `wishlist_header`, `downloaddialog` header)
     keep `mainPurple`, paired with `dsColorChromeForeground`. No legacy palette was removed or
     re-tinted: in those themes `mainPurple` really is the accent.
2. **Circular automation claim.** The previous pass asserted that `setDarkMode`/`assertDarkMode`
   were supported and then "verified" it with its own static test. Replaced by evidence:
   * `MaestroTests/schema/maestro_commands.json` — checked-in copy of Maestro's command
     vocabulary (56 keys = the properties of `YamlFluentCommand`, plus the closed value enums)
     with provenance: upstream file URL, docs index URL, per-command doc pages, retrieval date.
     It shows `setDarkMode: enabled|disabled`, `toggleDarkMode`, `assertDarkMode`,
     `assertLightMode` are real, and records `setFontScale`/`setTheme`/`setLocale` as **not**
     Maestro commands.
   * `check_maestro_command_schema` in the static suite parses every flow with PyYAML and
     validates command keys, value grammar, `runFlow` targets and `appId` against the manifest
     package. PyYAML >= 6 is a declared tool dependency: if it is missing the suite fails with
     the install command instead of passing on a regex-only fallback. No runtime claim.
   * `Misc/run_maestro_theme_matrix.sh` — the external-precondition path:
     `adb shell cmd uimode night yes|no` (read back and asserted), then
     `maestro test --test-output-dir … -e IS_NIGHT=… -e PREFIX=…` per appearance, restoring the
     original mode on exit. With `-s SERIAL` the same serial is passed to both `adb -s` and
     `maestro test --device`. Works without any in-flow command.

3. **Legacy light theme background cycle (this semantic-theme repair round).** `AppTheme` set
   `dsColorBackground = ?android:attr/colorBackground` while the same style mapped
   `android:colorBackground = ?attr/dsColorBackground`. `DefaultLight`, `DefaultLight_Blue`,
   `DefaultLight_Red` and `DefaultLight_Grey` never overrode `dsColorBackground`, so every
   stored legacy light choice resolved a self-reference. The base default is now the concrete
   `@color/ds_default_background` (`#FFFBFE` day, `#1C1B1F` night - the values the Material3
   parent already supplied); modern and dark/AMOLED themes still override it. No stored enum
   value or legacy palette entry was removed.
   `themes.cycles` now walks every locally defined style item in all 12 concrete themes
   (normalising `?android:attr/x` to the local `android:x` item) and fails even when the chain
   ends at an external Material attribute; `themes.legacy_tokens` resolves window background,
   primary, surface, text and chrome tokens for all 10 legacy variants. Reintroducing the old
   `?android:attr/colorBackground` default fails `themes.cycles` for all four legacy light
   themes.

New contracts (640 -> 733 checks; the semantic-theme cycle repair above takes the suite to 815)
are listed in `docs/UI_REDESIGN.md` §22.2: chrome-context, control-tint visibility, filter tint
tokens, chrome meaning, resolved WCAG contrast pairs, accent-on-surface and colour-state-list
visibility. They were mutation-checked: reintroducing
`?attr/mainPurple` as an `indicatorColor` fails (`mainPurple` = 1.07:1 in `ModernLight`, 1.16:1 in
`ModernDark`), reintroducing `mainPurple`/`Color.White` in the filter tints fails, adding
`- setFontScale:` to a flow fails.

## 6. Delivered features, per surface (whole overhaul)

| Surface | Delivered |
| --- | --- |
| Design system | token layer (41 tokens), Material3 role derivation, 7 token drawables, 2 token state lists, 48dp/typography scales, dialog/sheet/popup/snackbar/settings overlays |
| Themes | legacy themes preserved, `ModernLight`/`ModernDark` new default, "Follow system" default, persisted variant compatibility |
| Shell | bottom navigation (Search · Transfers · Browse · Messages), secondary navigation for account/login/chatrooms/users/settings, page identity contract, state restoration on recreation |
| Search | unified result rows + checkbox, filter chips, sort (filename/representative size) with persisted default, cross-result selection + action mode + batch download, contextual download affordance |
| Transfers | Downloads/Uploads segmented screen, unified actions, throttled progress, folder rows, segmented progress bar |
| Browse | user/status header, 48dp breadcrumbs, metadata rows, selection-aware download button |
| Messages | 2-level list/inner, compose dialog, sent-state colours from tokens |
| Chatrooms | overview + category items + inner chat, join-pending state |
| Settings | grouped catalog, pickers, value formatting, themed rows/dividers |
| Login / Users | login surface, user list/info/edit, chips and action sheets |
| Accessibility | content descriptions, 48dp touch targets, font-scale-tolerant rows/inputs, "Selected" state announcements |

## 7. Remaining gaps (the real list)

* **No runtime or visual verification anywhere.** No emulator, no `/dev/kvm`, no Maestro CLI on
  this host. No screenshot was produced and no Maestro flow was executed. Colour work is verified
  by resolved contrast arithmetic + resource compilation, not by looking at the app.
* **Contracts cannot see composition.** An accent painted on an accent (two tokens that are each
  individually fine) is invisible to the static suite.
* **Legacy themes keep their palettes but are not contrast-audited.** Their disabled/alpha
  colours (`#80ffffff`, `#ccffffff`, …) are below 4.5:1 by design. Changing them would change
  what users have chosen; only the modern themes are contrast-tested. The legacy light window
  background now resolves through a non-recursive default instead of a cyclic reference.
* **Drawables are not fully tokenised.** 96 vector/shape drawables still carry literal colours
  (layouts are clean). Dead leftovers also remain: `res/color/segmented_button_bg.xml`,
  `drawable/rounded_bottom_{left,right}_purple.xml`, `@color/mainPurple{Transparent,TurnedOff}` —
  nothing inflates them, deleted in a future cleanup pass.
* **Missing encrypted-test secret.** `SeekerEncryptKey` is unset, so `DebugSecrets.SeekerEncryptKey`
  is empty and the encrypted search-capture path is not exercised; the requested unit-test filter
  also excludes the real-data/encrypted suites.
* **Instrumentation tests were not run** (no device).
* **Maestro flows are representative, not proving.** They depend on English labels, the mock
  session and a search that returns results. The theme matrix needs
  `Misc/run_maestro_theme_matrix.sh` on a machine with a device.
* **Maestro schema is a snapshot** (2026-10-25). Re-diff it against upstream when the pinned
  Maestro version changes.
* **Dynamic colour / Material You** is not supported (deliberate: one fixed palette per theme).
* **Crowdin**: new strings are English-only until translations land.

## 8. Dependency / build warnings observed

Not introduced by this work, reported honestly:

* `MessagePack 2.5.198` — `NU1902` moderate advisories (10 IDs) and `NU1903` high advisories
  (`GHSA-hv8m-jj95-wg3x`, `GHSA-vh6j-jc39-fggf`).
* `Xamarin.AndroidX.DataStore*` — `NU1608` out-of-constraint resolved versions (1.2.1 resolved
  where `< 1.2.1` is requested).
* `APT2000` — four string resources removed by aapt2 for lacking a required default value
  (`error_duplicate`, `failed_to_establish_connection_to_peer`, `in_queue`, `not_started`).
* `JAVAC` exploded-module output-directory warning in the Debug Mock build.
* **Tooling dependency (not a package dependency):** `Misc/test_ui_contracts.py` requires
  PyYAML >= 6 for `check_maestro_command_schema` (`python3 -m pip install 'PyYAML>=6'`).
  Without it the suite exits non-zero with that instruction; it does not pass on a fallback.

No dependency upgrades were attempted.
