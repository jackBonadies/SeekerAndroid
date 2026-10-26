# UX overhaul implementation checkpoint

## Implemented

- Search result download buttons now queue the entire result folder directly; tapping the result still opens the existing detailed partial-selection sheet.
- Download enqueueing exposes a structured accepted/already-present/failed summary, and the direct path reports it to the user without weakening duplicate protection.
- Download-dialog selections persist by stable file identity across activity recreation.
- Download-dialog display guarding is released if dialog creation fails.
- Main activity uses one guarded routing path for cold and warm intents, including shares, search/room, browse, wishlist, account, uploads, transfers, and completed-folder notifications. Completed-folder notifications select the matching transfer group when available.
- Downloads/uploads mode switching clears incompatible folder/batch/action-mode state.
- Chatroom filters restore both filter state and visible query. Messaging preserves reading position for new messages and exposes an indicator; user-list updates ignore absent rows.
- Encrypted fixture tests are correctly marked `RealData`; the UI contract suite can be run locally.
- Global account/users/wishlist/chat/settings navigation now uses a left hamburger at root destinations; the redundant shell three-dot menu is no longer inflated.

## Files affected

Core changes are in `Common/Services/DownloadService.cs`, `Seeker/Search/`, `Seeker/Main/MainActivity.cs`, `Seeker/Transfers/`, `Seeker/Messages/`, `Seeker/Chatroom/`, `Seeker/Users/`, resources, and tests.

## Verification

- `dotnet clean Seeker/Seeker.csproj -v:q && dotnet build Seeker/Seeker.csproj --no-restore -v:q` — completed source/resource compilation; a later incremental plain build exits nonzero on existing NuGet warnings despite reporting 0 errors.
- `python3 Misc/test_ui_contracts.py` — 867 passed, 0 failed.
- `dotnet test UnitTestCommon/UnitTestCommon.csproj --no-restore --nologo -v:q --filter 'TestCategory!=RealData'` — 651 passed, 1 skipped, 0 failed.
- Debug Mock standalone APK published: `Seeker/bin/Debug Mock/net10.0-android36.0/com.companyname.andriodapp1-Signed.apk`.

## Known blockers

- No ADB device was attached at validation time, so no live-device workflow testing was performed.
- The standalone Debug Mock publish completes successfully. Plain incremental builds can exit nonzero solely on existing NuGet dependency/security warnings (0 compiler errors reported).
