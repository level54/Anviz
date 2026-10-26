# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

`Anviz.SDK` is a .NET Standard 2.0 library for communicating with Anviz biometric access control devices (fingerprint readers, face recognition terminals, etc.) over TCP. It is distributed as a NuGet package.

The solution contains two projects:
- `Anviz.SDK/` — the main library
- `Sample/` — a console application demonstrating SDK usage

## Build Commands

```bash
# Build the library (netstandard2.0)
dotnet build Anviz.SDK/Anviz.SDK.csproj -c Release

# Tests (net10.0, xunit.v3 on Microsoft.Testing.Platform, selected by global.json): name the project, no bare path
dotnet test --project Anviz.SDK.Tests/Anviz.SDK.Tests.csproj
dotnet test --project Anviz.SDK.Tests/Anviz.SDK.Tests.csproj --filter-class "*TimingTests" --output Detailed

# Pack NuGet package
dotnet pack Anviz.SDK/Anviz.SDK.csproj -c Release -o ./nupkg
```

`Sample/` (old-style net472) does not build: it calls `SetEmployeesData`, which no longer exists. `dotnet build Anviz.sln`
therefore fails in Sample; build the projects above.

`Anviz.SDK.Tests` drives the public `AnvizManager` / `AnvizDevice` against `FakeAnvizDevice`, a fake device on 127.0.0.1
that answers from the protocol document (`Docs/Communication Protocol.pdf`; the markdown copy lost some command codes) and
fails the test on any frame that breaks it. A test pinning today's code where it differs from the document ends `_Today`.
`TimingTests` prints timings and never fails on a number. Tests connect through `FakeAnvizDevice.ConnectAsync`, except the M7 check tests, which connect through the check's relay.

`Anviz.SDK.M7Check` (net10.0 console) runs the owner-approved checks against a real device: `dotnet run --project Anviz.SDK.M7Check -- <host> [port] [--enrol] [--time]`, or `--remove <id>` to remove a left-over test user (only one named "CHR27 TEST"). It talks to the device through `FrameRelay` on 127.0.0.1, which logs every frame and forwards only an allow list (reads; writes for the one test user; the clock within a minute of the PC); anything else closes the connection. Never widen that list without the owner: clear records, delete all users, the "new records" request, reset, reboot, network, device ID, serial number and unlock stay blocked.

## Architecture

### Entry Points

**`AnvizManager`** — creates and manages device connections. Two modes:
- Client mode: `Connect(host, port)` connects to a device
- Server mode: `Listen(port)` accepts incoming device connections
- Returns an `AnvizDevice` instance on successful connection

**`AnvizDevice`** — the public API surface for all device operations. Implemented as ~36 partial class files under `Anviz.SDK/Commands/`, one file per logical command group. Exposes events: `DevicePing`, `ReceivedPacket`, `ReceivedRecord`, `DeviceError`.

**`AnvizStream`** (internal) — owns the TCP socket. One reader task (`ReceiveAsync`, started with `Task.Run`) reads every frame; `SendCommand` runs one command at a time (`_commandLock`), writes under `_writeLock` (the reader's pong uses the same lock) and waits for the answer code (command + `0x80`) with one 10-second limit covering the write and the answer. A device error answer (RET not success) is read to the end and thrown as `AnvizException`; the connection stays open. An unreadable frame, a timeout or a failed write ends the connection (`DeviceConnectionLostException`). `DeviceError` fires only when the reader loses the connection — Chronos treats it as "connection gone" and reconnects, so never raise it for anything else.

### Protocol Layer

Binary TCP protocol with this frame structure:
```
Preamble (0xA5) | DeviceID (4 bytes LE) | CommandCode | RET (opt) | DataLength (2 bytes) | Data | CRC16 (2 bytes)
```

- `Anviz.SDK/Commands/Command.cs` — abstract base class all commands derive from
- `Anviz.SDK/Responses/Response.cs` — binary frame parser
- `Anviz.SDK/Utils/CRC16.cs` — checksum implementation
- `Anviz.SDK/Utils/Bytes.cs` — binary helpers (Split, Read, etc.)

### Command Pattern

Each device operation has a corresponding `Command` subclass (all `internal`) in `Anviz.SDK/Commands/`. The `AnvizDevice` partial class methods construct the command, send it via `AnvizStream`, and parse the typed response. Commands are never exposed publicly — only `AnvizDevice` methods are.

### Data Models

Response DTOs live in `Anviz.SDK/Responses/`: `UserInfo`, `Record`, `BasicSettings`, `AdvancedSettings`, `TcpParameters`, `ScheduledBell`, `TimeZoneInfo`, etc.

### Encoding Notes

- User names: Unicode (20 bytes) or ANSI (10 bytes) — both variants exist for `SetStaffData`/`GetStaffData`
- Fingerprint/face templates: raw binary
- Device ID: 4 bytes, high byte first
- Fingers: 2-byte bitfield (14 fingers, LeftIndex → RightSmall); see `Anviz.SDK/Utils/Fingers.cs`

## Key Conventions

- **Partial classes for organization**: `AnvizDevice` is split into many files, one per command family. Add new device commands by adding a new partial class file and a corresponding `Command` subclass.
- **Async via TaskCompletionSource**: each command waits on a `PendingCall` whose `TaskCompletionSource` runs continuations asynchronously (a caller never resumes on the reader, so `Dispose` cannot deadlock). Frames nobody waits for go to `ReceivedPacket`.
- **Internal vs public**: Keep all `Command` subclasses and `AnvizStream` internal. Only `AnvizManager` and `AnvizDevice` are public.
- **Logging**: `log4net` is the only dependency. Use it for debug/error logging consistent with existing calls.

## Behavioral Guidelines

Principles to reduce common LLM coding mistakes. These bias toward caution over speed — use judgment for trivial tasks.

### Think Before Coding

Don't assume. Don't hide confusion. Surface tradeoffs.

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them — don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### Simplicity First

Minimum code that solves the problem. Nothing speculative.

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### Surgical Changes

Touch only what you must. Clean up only your own mess.

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it — don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

Every changed line should trace directly to the user's request.

### Goal-Driven Execution

Define success criteria. Loop until verified.

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria enable independent looping. Weak criteria ("make it work") require constant clarification.
