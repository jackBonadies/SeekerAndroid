# Sonar PR 129 review

Scope: the 42 live findings in `/tmp/sonar.json` on branch
`upstream-ui-ux-no-ci-20261025`. This is a review/disposition record plus the
bounded cleanup described below; it is not a claim that every rule was fixed.

No device validation and no cloud/Sonar rescan were performed for this change.
The only gate evidence is the local commands listed at the end; no other gate
result is asserted.

## Disposition summary

- Categories: 13 false positive, 22 cosmetic preference, 7 worthwhile maintainability, 0 reliability bug.
- Dispositions: 5 fixed, 1 hardened defensively, 24 deferred, 12 left unchanged.
- No actual reliability bug is demonstrated by this set. The S2259 capture-null
  warning is treated as defensive hardening: `SearchFragment.Instance` is never
  cleared and the foreground caller initializes it, so no reachable null/crash
  was shown. The exact call path is recorded under "Execution-path evidence".
- Valid style-rule reports on public mutable preference fields and static writes
  are **not** reclassified as false positives merely because the current
  architecture intends them; they are low-value cosmetic preferences here.
- `ElementAt` over a `List<T>` uses the optimized `IList` path and already
  throws `ArgumentOutOfRangeException`; the suggestion is cosmetic and makes no
  performance/reliability claim.
- Interface/Android instance members flagged by S2325 are genuine false
  positives. The two S2583 batch guards are also genuine false positives; the
  `SearchActionMode` algorithm was left untouched.
- Analyzer causes below are described as observed rule behavior, not as proven
  root-cause diagnoses, and no unverified upstream issue numbers are cited.

## Changes made in this bounded cleanup

1. `Seeker/Search/SearchFragment.cs` `ClearTab`: tab model clearing and the
   wishlist early-return are unchanged. Inside the current-tab branch the
   fragment captures `SearchFragment.Instance` once, guards the local for null,
   uses it for every UI reset, and uses `recyclerViewSearch?.SetAdapter` for
   partial-initialization safety. `Instance` ownership, lifecycle, navigation,
   filters and `OnDestroyView` are untouched.
2. `Seeker/Search/SearchAdapter.cs`: the four flagged click-handler locals were
   renamed from `position` to `boundPosition`; fields and behavior are unchanged.
3. `Common/Search/SearchResultMetadata.cs`: representative-file metadata
   population was extracted into `PopulateRepresentativeFile`; output, order,
   localization and hidden-locked handling are preserved.
4. Tests: `UnitTestCommon/SearchResultMetadataTests.cs` adds boundary cases
   (empty response, hidden locked-only, locked fallback only when shown). The
   existing `Misc/test_ui_contracts.py` gained a **static** (not runtime) source
   contract for `ClearTab`: it must read `SearchFragment.Instance` exactly once,
   keep that read in the null-guarded local snapshot, and reach every UI reset
   (selection/filter reset, adapter swap, chips, empty state, header) through
   that local, i.e. no additional direct `Instance` access is left in the
   branch.
5. `docs/SONAR_PR129_REVIEW.md`: this record.

## Findings

| # | Key | Rule | Original file:line | Category | Disposition | Rationale |
|---|-----|------|--------------------|----------|-------------|-----------|
| 1 | `AaF2Bw80j5MwrLXystUZ` | S2325 | `Seeker/Main/MainActivity.cs:1246` | False positive | Leave | Android Activity instance member (touches `SupportActionBar`); making it static is not appropriate. |
| 2 | `AaF2Bw80j5MwrLXystUa` | S2325 | `Seeker/Main/MainActivity.cs:1253` | False positive | Leave | Android Activity instance member (touches `SupportActionBar`); making it static is not appropriate. |
| 3 | `AaF2Bw80j5MwrLXystUX` | S2696 | `Seeker/Main/MainActivity.cs:575` | Cosmetic preference | Deferred | Valid style report: static field written from an instance method. Low value given the existing mutable static navigation state; no demonstrated bug. |
| 4 | `AaF2Bw80j5MwrLXystUY` | S2696 | `Seeker/Main/MainActivity.cs:578` | Cosmetic preference | Deferred | Valid style report: static dialog field written from an instance method. The static dialog state is existing architecture; no demonstrated bug. |
| 5 | `AaF2BxKdj5MwrLXystUk` | S2325 | `Seeker/Search/SearchFragment.cs:2346` | False positive | Leave | `DownloadResultFolder(int position)` is a genuine instance member because it resolves localized text through the fragment context: `GetString(Resource.String.nothing_to_download)` and `GetString(Resource.String.error_)`, formatted with `Resource.String.download_enqueue_summary`, toasted via `SeekerApplication.Toaster`. It does not touch `FragmentManager`; the reason it cannot be static is resource/context access. |
| 6 | `AaF2BxKdj5MwrLXystUm` | S6608 | `Seeker/Search/SearchFragment.cs:2352` | Cosmetic preference | Deferred | `List<T>.ElementAt` uses the optimized `IList` indexer path and already throws `ArgumentOutOfRangeException`; no performance or reliability fix is claimed. Current code catches it. |
| 7 | `AaF2BxKdj5MwrLXystUn` | S6608 | `Seeker/Search/SearchFragment.cs:2353` | Cosmetic preference | Deferred | Same as the adjacent `ElementAt` finding: optimized `IList` path, existing `ArgumentOutOfRangeException` handling, no fix claimed. |
| 8 | `AaF2Bx7vj5MwrLXystUz` | S3776 | `Common/Search/SearchResultMetadata.cs:113` | Worthwhile maintainability | Fixed | Extracted representative-file population into `PopulateRepresentativeFile`, preserving output, ordering, localization and hidden-locked behavior. |
| 9 | `AaF2Bx7vj5MwrLXystUy` | S3267 | `Common/Search/SearchResultMetadata.cs:278` | Cosmetic preference | Deferred | Valid `Where` suggestion, but the explicit loop keeps the null/whitespace filtering readable; no behavior or measured performance issue. |
| 10 | `AaF2Bx7xj5MwrLXystU0` | S3267 | `Common/Search/SearchSelection.cs:116` | Cosmetic preference | Deferred | Valid `Where` suggestion, but the explicit loop is clear and avoids unnecessary LINQ/style churn. |
| 11 | `AaF2Bx_zj5MwrLXystU1` | S4136 | `Common/Transfers/TransferPresentation.cs:52` | Cosmetic preference | Deferred | Order-only rule; both overloads are correct and documented, so moving them is churn. |
| 12 | `AaF2BxoOj5MwrLXystUs` | S1104 | `Seeker/Browse/BrowseAdapter.cs:186` | Cosmetic preference | Deferred | Valid style report: public mutable field on a legacy custom view. Encapsulating it would be low-value architecture churn, not a false positive. |
| 13 | `AaF2Bw4Pj5MwrLXystUR` | S2696 | `Seeker/Main/MainActivity.Navigation.cs:19` | Cosmetic preference | Deferred | Valid static-event subscription from an instance lifecycle method; the matching unsubscribe exists in `OnPause`, so no leak is demonstrated. |
| 14 | `AaF2Bw4Pj5MwrLXystUS` | S2696 | `Seeker/Main/MainActivity.Navigation.cs:20` | Cosmetic preference | Deferred | Valid static-event subscription from an instance lifecycle method; the matching unsubscribe exists in `OnPause`, so no leak is demonstrated. |
| 15 | `AaF2Bw4Pj5MwrLXystUT` | S2696 | `Seeker/Main/MainActivity.Navigation.cs:26` | Cosmetic preference | Deferred | Valid static-event unsubscription from an instance lifecycle method; paired with the subscription in `OnResume`. No demonstrated bug. |
| 16 | `AaF2Bw4Pj5MwrLXystUU` | S2696 | `Seeker/Main/MainActivity.Navigation.cs:27` | Cosmetic preference | Deferred | Valid static-event unsubscription from an instance lifecycle method; paired with the subscription in `OnResume`. No demonstrated bug. |
| 17 | `AaF2Bw4Pj5MwrLXystUQ` | S2325 | `Seeker/Main/MainActivity.Navigation.cs:31` | False positive | Leave | Android instance member event callback; uses `RunOnUiThread`, so it cannot be static. |
| 18 | `AaF2Bw4Pj5MwrLXystUV` | S2325 | `Seeker/Main/MainActivity.Navigation.cs:51` | False positive | Leave | Android instance member event callback; uses `RunOnUiThread`, so it cannot be static. |
| 19 | `AaF2Bw4Pj5MwrLXystUW` | S2325 | `Seeker/Main/MainActivity.Navigation.cs:61` | False positive | Leave | Android Activity instance member (`FindViewById`, navigation view); cannot be static. |
| 20 | `AaF2Bxy6j5MwrLXystUw` | S3776 | `Seeker/Messages/MessagesComposeDialog.cs:20` | Worthwhile maintainability | Deferred | One point over the threshold (16 vs 15) in a behavior-sensitive dialog flow; not part of this bounded cleanup. |
| 21 | `AaF2Bxy6j5MwrLXystUx` | S2219 | `Seeker/Messages/MessagesComposeDialog.cs:46` | Cosmetic preference | Deferred | Valid idiom suggestion (`is null` instead of `as` plus null check); behavior is equivalent, no bug. |
| 22 | `AaF2Bxw2j5MwrLXystUt` | S2933 | `Seeker/Messages/MessagesOverviewRecyclerAdapter.cs:35` | Worthwhile maintainability | Deferred | Field is only assigned in the constructor, so `readonly` is safe; unrelated to the requested cleanup. |
| 23 | `AaF2BxFoj5MwrLXystUc` | S2325 | `Seeker/Search/SearchActionMode.cs:131` | Cosmetic preference | Deferred | Helper happens to be static-able, but it is not an Android/interface member and the rule is low value; selection algorithm left untouched per scope. |
| 24 | `AaF2BxFoj5MwrLXystUg` | S2583 | `Seeker/Search/SearchActionMode.cs:174` | False positive | Leave | `if (files.Count != 0)` inside `DownloadSelectedResults` is **not** always false. `searchSelection.Resolve(GetDisplayedResponses())` feeds `selected.GroupBy(response => response.Username)`, `response.GetFiles(hideLocked)` feeds `files.AddRange(...)` on a `new List<Soulseek.File>()`, and a selected response with at least one public file therefore produces a nonempty `List<File>` and this branch adds the `(Username, Files)` tuple to `payloads`. Only a group whose responses all contribute no eligible file leaves `files` empty; that empty-group case is treated here as defensive, and its UI reachability is not shown. Algorithm untouched. |
| 25 | `AaF2BxFoj5MwrLXystUf` | S2583 | `Seeker/Search/SearchActionMode.cs:180` | False positive | Leave | `if (payloads.Count == 0)` is **not** always true. The ordinary nonempty-payload path above (a selected response with at least one public file) makes `payloads` nonempty, so this guard is bypassed and control reaches the existing `SessionService.Instance.RunWithReconnect(Enqueue, silent: false)` enqueue. Empty selections never reach here because `if (selected.Count == 0)` returns earlier with the `nothing_selected_extra` toast. The no-payload case the guard covers (every group resolving to no eligible file, e.g. locked-only while `hideLocked` is true) is kept as defensive handling only; it is not claimed to be user-reachable or proven at runtime. Algorithm untouched. |
| 26 | `AaF2BxFoj5MwrLXystUb` | S1104 | `Seeker/Search/SearchActionMode.cs:203` | Cosmetic preference | Deferred | Valid style report: public callback wiring field in existing action-mode architecture; no behavior issue. |
| 27 | `AaF2BxFoj5MwrLXystUe` | S2325 | `Seeker/Search/SearchActionMode.cs:205` | False positive | Leave | Android `ActionMode.ICallback` instance member; cannot be static. |
| 28 | `AaF2BxFoj5MwrLXystUd` | S2325 | `Seeker/Search/SearchActionMode.cs:212` | False positive | Leave | Android `ActionMode.ICallback` instance member; cannot be static. |
| 29 | `AaF2BxNFj5MwrLXystUp` | S1117 | `Seeker/Search/SearchAdapter.cs:129` | Worthwhile maintainability | Fixed | Renamed the click-handler local to `boundPosition` and updated its references; no field or behavior change. |
| 30 | `AaF2BxNFj5MwrLXystUo` | S1117 | `Seeker/Search/SearchAdapter.cs:152` | Worthwhile maintainability | Fixed | Renamed the click-handler local to `boundPosition` and updated its references; no field or behavior change. |
| 31 | `AaF2BxNFj5MwrLXystUq` | S1117 | `Seeker/Search/SearchAdapter.cs:162` | Worthwhile maintainability | Fixed | Renamed the click-handler local to `boundPosition` and updated its references; no field or behavior change. |
| 32 | `AaF2BxNFj5MwrLXystUr` | S1117 | `Seeker/Search/SearchAdapter.cs:226` | Worthwhile maintainability | Fixed | Renamed the click-handler local to `boundPosition` and updated its references; no field or behavior change. |
| 33 | `AaF2BxKdj5MwrLXystUh` | S2325 | `Seeker/Search/SearchFragment.cs:1658` | False positive | Leave | Event handler calls the instance method `UpdateSortAvailability`; cannot be static. |
| 34 | `AaF2BxKdj5MwrLXystUi` | S2325 | `Seeker/Search/SearchFragment.cs:1663` | False positive | Leave | Event handler calls the instance method `UpdateSortAvailability`; cannot be static. |
| 35 | `AaF2BxKdj5MwrLXystUj` | S2259 | `Seeker/Search/SearchFragment.cs:2054` | False positive | Hardened defensively | No demonstrated reachable null: `Instance` is never cleared and the foreground caller initializes it. `ClearTab` now snapshots it once, null-guards the local and uses it consistently; not claimed as a reliability fix. |
| 36 | `AaF2ByGTj5MwrLXystU2` | S1104 | `Common/PreferencesState.cs:76` | Cosmetic preference | Deferred | Valid style report: public mutable static preference field. This is intentional existing architecture, but the rule report is still valid, not a false positive. |
| 37 | `AaF2ByGTj5MwrLXystU3` | S2223 | `Common/PreferencesState.cs:76` | Cosmetic preference | Deferred | Valid style report: public mutable persisted preference field cannot be `const`/`readonly` without changing persistence. Not a false positive. |
| 38 | `AaF2ByGTj5MwrLXystU4` | S1104 | `Common/PreferencesState.cs:77` | Cosmetic preference | Deferred | Valid style report: public mutable static preference field. Intentional existing architecture, but the report is valid, not a false positive. |
| 39 | `AaF2ByGTj5MwrLXystU5` | S2223 | `Common/PreferencesState.cs:77` | Cosmetic preference | Deferred | Valid style report: public mutable persisted preference field cannot be `const`/`readonly` without changing persistence. Not a false positive. |
| 40 | `AaF2Bxw2j5MwrLXystUu` | S1186 | `Seeker/Messages/MessagesOverviewRecyclerAdapter.cs:188` | Cosmetic preference | Deferred | Legacy empty hook; adding a comment, throwing or removing the dead hook is cleanup, with no runtime impact demonstrated. |
| 41 | `AaF2Bxw2j5MwrLXystUv` | S2325 | `Seeker/Messages/MessagesOverviewRecyclerAdapter.cs:192` | Cosmetic preference | Deferred | Returns the constant `-1` and could be static or removed; low-value dead-code cleanup, not an Android/interface false positive. |
| 42 | `AaF2BxKdj5MwrLXystUl` | S2696 | `Seeker/Search/SearchFragment.cs:2308` | Cosmetic preference | Deferred | Valid style report: guarded static dialog flag written from an instance method. Existing UI state architecture; no demonstrated bug. |

## Execution-path evidence

Static reading of the current source only; nothing below was executed on a
device or under a debugger.

- Assignment: `Seeker/Search/SearchFragment.cs:1014` declares
  `public static volatile SearchFragment Instance = null;`. `OnStart()`
  (line 65, `SearchFragment.Instance = this;`) and `OnCreateView(...)`
  (line 1042, `Instance = this;`) are the only writers.
- Clearing: no assignment of `null` to `SearchFragment.Instance` exists
  anywhere in `Seeker/` or `Common/` (the only occurrences of the field are the
  declaration, those two assignments and read sites), so `Instance` is never
  cleared; it is only replaced by a newer fragment instance.
- Interactive search path: `SubmitSearch(string)`
  (`Seeker/Search/SearchFragment.cs:455`) calls
  `SearchAPI(cancellationToken, searchText, tabId, scope)` (line 490).
  `SearchAPI` (line 2642) runs inside
  `SessionService.Instance.RunWithReconnect(...)` and, for the non-wishlist
  case, marshals with
  `SeekerApplication.RunOnUIThread(() => SearchLogic(...))` (line 2664).
  `SearchLogic(...)` (line 2408) calls `ClearTab(fromTab, fromWishlist)`
  (line 2419) first, so the `Instance` read in `ClearTab` happens on the UI
  thread for this path.
- Wishlist path: `Seeker/Search/WishlistController.cs:179` calls
  `SearchFragment.SearchAPI(..., fromWishlist: true)`. `SearchAPI` then invokes
  `SearchLogic` directly on the reconnect thread, but `ClearTab`
  (`Seeker/Search/SearchFragment.cs:2029`) returns immediately at
  `if (fromWishlist) { return; }` (lines 2031-2034), before any `Instance`
  read.
- Consequence: the `ClearTab` snapshot/null-guard is defensive hardening for a
  state no current caller was shown to reach; no crash was reproduced and no
  analyzer root cause is proven.

## Verification commands

Local only; no device or cloud rescan was run.

```
python3 Misc/test_ui_contracts.py
PATH="$HOME/.dotnet:$PATH" dotnet test UnitTestCommon/UnitTestCommon.csproj --nologo -v:q --filter 'FullyQualifiedName!~TestRealUserParsingBrowseResponseTestData&FullyQualifiedName!~ImportQTConfig&FullyQualifiedName!~Keywords_RealData'
PATH="$HOME/.dotnet:$PATH" dotnet build Seeker/Seeker.csproj -c Debug -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true --nologo -v:q
PATH="$HOME/.dotnet:$PATH" dotnet build Seeker/Seeker.csproj -c 'Debug Mock' -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true --nologo -v:q
git diff --check
```

### Observed local results

From the parent's runs of exactly the commands above; no device or cloud
result is asserted.

- `Misc/test_ui_contracts.py`: 871 pass, 0 fail.
- Filtered `UnitTestCommon` run: 654 passed, 1 skipped, 0 failed (655 total).
- `dotnet build` Debug and Debug Mock: each exited 0 with 150 warnings and
  0 errors. The warnings include the pre-existing MessagePack 2.5.198
  NU1902/NU1903 advisory reports; those advisories are not addressed in this
  scope.
- Inventory: the 42 `/tmp/sonar.json` keys were checked once against this
  table.
- Diff scope: no workflow-file changes relative to base
  `ff1dcc6de55a412e8d6eadaddf36deca54517413`, including the worktree;
  `master` is unchanged at `764fef4d430bb5020a3253935bd99a0b0dac717d`.
