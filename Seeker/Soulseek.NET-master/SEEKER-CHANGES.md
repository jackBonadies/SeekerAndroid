**This is a modified version of Soulseek.NET.  It is not maintained by, endorsed by, or affiliated with the Soulseek.NET project or its author(s).**

The unmodified upstream project is at https://github.com/jpdillingham/Soulseek.NET.

This folder is a `git subtree` of upstream.  Pull latest with:

```
git subtree pull --prefix=Seeker/Soulseek.NET-master https://github.com/jpdillingham/Soulseek.NET.git master --squash
```

## Local modifications

All modifications are confined to `src/`.  Every modified file carries a `Modified:` notice in its header.

Changes:

- **Latin-1 / mojibake handling**: strings are decoded as UTF-8 with a Latin-1 fallback, 
  and the flags recording which encoding was used are carried through `File`, `Directory` 
  and `SearchResponse` so that outgoing messages re-encode filenames the same way the peer sent them.  
  Also includes falling back to Latin-1 for transfer retries.
- **Private room operators**: `OperatorAddedRemovedEventArgs` plus the
  `OperatorInPrivateRoomAddedRemoved` event on `ISoulseekClient` / `IServerMessageHandler` /
  `ServerMessageHandler`.
- **Android networking**: `Connection` uses dual-mode IPv6 socket (an IPv4-only socket cannot
  connect at all on the IPv6-only networks mobile carriers use), a `SoulseekClientOptions.AddressResolver` hook 
  (`Dns.GetHostEntry` fails sometimes on Android); and `GetListeningState()` on `SoulseekClient`
  (we dont fail if listener fails)
- **Concurrency**: Fixes race condition.
- **Transfer state**: the additional `TransferStates` values (`UserOffline`, `CannotConnect`,
  `FallenFromQueue`, `SizeMismatch`), a `TransferSizeMismatchException` only when the peer
  reports a non-zero size, and `IsTransferInDownloads()`.
- **Search results**: `SearchResponse.cachedDominantFileType` / `cachedCalcBitRate`, a per-response
  cache the app fills in for performance.
- **Project file**: `InternalsVisibleTo("Common")`.