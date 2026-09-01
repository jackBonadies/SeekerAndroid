# Seeker's modified copy of Soulseek.NET

**This is a modified version of Soulseek.NET.  It is not maintained by, endorsed by, or
affiliated with the Soulseek.NET project or its author(s).**

The unmodified upstream project is at https://github.com/jpdillingham/Soulseek.NET.

## How this folder is maintained

It is a **git subtree** of upstream (squashed), currently at release `10.0.2`
(commit `13a12735`).  Do not update it by copying files — merge upstream through git so the
local modifications below are preserved:

```
git subtree pull --prefix=Seeker/Soulseek.NET-master https://github.com/jpdillingham/Soulseek.NET.git <tag-or-branch> --squash
```

Conflicts appear only where local edits overlap upstream changes.

## Local modifications

All modifications are confined to `src/`.  Every modified file carries a
`Modified:` notice in its header, as required by Section 3(c) of the Additional Terms;
`src/EventArgs/OperatorAddedRemovedEventArgs.cs` is newly created for this modified version.
`tests/`, `examples/`, `bin/` and `docs/` are pristine upstream and nothing in Seeker builds them.

By theme:

- **Latin-1 / mojibake handling** (the largest group): strings are decoded as UTF-8 with a
  Latin-1 fallback, and the flags recording which encoding was used are carried through
  `File`, `Directory` and `SearchResponse` so that outgoing messages re-encode filenames the
  same way the peer sent them.  Touches `MessageBuilder`, `MessageReaderExtensions` and the
  peer request messages.
- **Private room operators**: `OperatorAddedRemovedEventArgs` plus the
  `OperatorInPrivateRoomAddedRemoved` event on `ISoulseekClient` / `IServerMessageHandler` /
  `ServerMessageHandler`.
- **Android networking**: dual-mode IPv6 sockets in `Connection`, a `SoulseekClientOptions.AddressResolver`
  hook (Android cannot always use `Dns.GetHostEntry`), and `SocketException` handling plus
  `GetListeningState()` on `SoulseekClient` — a failed listener is reported through upstream's
  own `Diagnostic` rather than throwing `ListenException` out of `ConnectAsync`.
- **Transfer state**: the additional `TransferStates` values (`UserOffline`, `CannotConnect`,
  `FallenFromQueue`, `SizeMismatch`), a `TransferSizeMismatchException` only when the peer
  reports a non-zero size, and `IsTransferInDownloads()`.
- **Search results**: `SearchResponse.cachedDominantFileType` / `cachedCalcBitRate`, a per-response
  cache the app fills in.  This belongs in Seeker, not here — see the note below.
- **Project file**: `Release IzzySoft` configuration and `InternalsVisibleTo("Common")`.

### Known wart

`SearchResponse.cachedDominantFileType` / `cachedCalcBitRate` are public mutable fields bolted
onto an otherwise immutable upstream model, with 27 call sites in the app.  They are not
persisted (`CustomMessagePackFormatters` ignores them), so moving them out is an app-side change
only — a `ConditionalWeakTable<SearchResponse, …>` in `Common` would remove them from the fork
entirely.

## Client version identifier

Section 5 of the Additional Terms requires Covered Software connecting to the Soulseek network
to transmit a client version identifier unique to it.  Seeker transmits major version 170
(upstream's `Constants.MajorVersion`) with **minor version 128**, set at the single
`new SoulseekClient(128, ...)` call site in `Seeker/SeekerApplication.cs`.  Upstream's own
default minor version is 100, which the library now rejects outright.
