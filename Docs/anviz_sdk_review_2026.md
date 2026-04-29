# Anviz SDK Protocol Review 2026

**Date**: 2026-04-28  
**Reviewer**: Claude (claude-sonnet-4-6)  
**Scope**: `Anviz.SDK` .NET Standard 2.0 library vs. TC_B V2 binary protocol and CrossChex SDK API

---

## Overview

This document is a formal conformance review of the `Anviz.SDK` codebase against two protocol reference documents:

- `Docs/communication_protocol.md` — TC_B V2 binary wire protocol (frame structure, command codes, response codes, CRC)
- `Docs/crosschex_new_sdk_api_20180710.md` — CrossChex SDK API (higher-level C API, data structures, field layouts)

Findings are grouped by severity: **Critical**, **High**, **Medium**, and **Low**.

---

## Section 1 — Wire Frame Protocol Conformance

### 1.1 Frame Structure — Mostly Correct

The TC_B V2 wire frame structure:

```
Command:  [STX:0xA5][DeviceID:4][CMD:1][LEN:2][DATA:0-N][CRC:2]
Response: [STX:0xA5][DeviceID:4][ACK:1][RET:1][LEN:2][DATA:0-N][CRC:2]
```

`Command.cs` and `Response.cs` implement this correctly in the following respects:

| Aspect | Status |
|--------|--------|
| STX = 0xA5 | ✓ Correct |
| DeviceID as 4 bytes, MSB-first (big-endian) | ✓ Correct |
| ACK = CMD + 0x80 response matching | ✓ Correct |
| CRC16 stored as little-endian (CRCL then CRCH) | ✓ Correct |
| Ping (0x7F) auto-responded with Pong (0xFF) | ✓ Correct |
| RET byte only present when ACK ≥ 0x80 | ✓ Correct |
| CRC16 calculated over entire frame | ✓ Correct |

### 1.2 LEN Field Endianness — ✅ VERIFIED (M7 2026-04-29)

**Status**: No bug. Code is correct.

**Evidence**: Fingerprint template download (CMD=0x44) logged `[01 52]` big-endian=338, CRC passed. The device sends and accepts LEN in **big-endian** order. The TC_B V2 document's "little-endian" guidance does not apply to this field for the M7 device.

**Files**: `Anviz.SDK/Commands/Command.cs`, `Anviz.SDK/Responses/Response.cs` — no changes needed.

### 1.3 CRC16 Algorithm — Correct

`Anviz.SDK/Utils/CRC16.cs` implements table-driven CRC16 with initial value 0xFFFF. This is consistent with the Modbus CRC-16-IBM variant referenced in the protocol. No action required.

---

## Section 2 — Command Code Discrepancies (Critical / High)

Several commands use codes that do not appear in the TC_B V2 protocol document, and two use codes that conflict with documented commands.

### 2.1 Serial Number Commands — ✅ VERIFIED (M7 2026-04-29)

**Status**: No bug. Command 0x24 is a valid M7 device-specific extension.

**Evidence**: `GetDeviceSN()` sent CMD=0x24, device responded ACK=0xA4 (=0x24+0x80), DATA=16 bytes, CRC OK. The device correctly handles 0x24/0xA4 as a Get S/N exchange. Code 0x46 from the TC_B V2 document may apply to other device families; for M7, 0x24 is the correct code.

| File | Code in SDK | Result on M7 |
|------|-------------|--------------|
| `GetDeviceSNCommand.cs` | 0x24 | ✅ ACK=0xA4, 16 bytes, CRC OK |
| `SetDeviceSNCommand.cs` | 0x25 | Not tested — assumed symmetric |

**Files**: `Anviz.SDK/Commands/GetDeviceSNCommand.cs`, `Anviz.SDK/Commands/SetDeviceSNCommand.cs` — no changes needed.

### 2.2 Undocumented Commands (Medium)

The following commands exist in the SDK but have no corresponding entry in the TC_B V2 protocol. They may be device-model-specific extensions or internal commands:

| File | Code | Notes |
|------|------|-------|
| `RebootDeviceCommand.cs` | 0x2E | Device reboot; not in TC_B V2 |
| `SetConnectionPassword.cs` | 0x04 | Auth password; not in TC_B V2 |
| `InquireCardCommand.cs` | 0x7E | Card inquiry; not in TC_B V2 |

**Action**: Document these as device-specific extensions with a comment referencing the device model(s) they apply to.

---

## Section 3 — Epoch / Date Encoding — ✅ VERIFIED CORRECT (M7 2026-04-29)

**Status**: No bug. The review finding was incorrect. Jan 2 epoch is correct for the M7.

**File**: `Anviz.SDK/Utils/DateConversions.cs`

**Evidence**: During the 2026-04-29 test session, two records were downloaded at approximately 17:44:10–17:44:12 local time. The raw date bytes `[31 83 AE 6A]` decoded as:

- Jan 2 epoch → `2026-04-29 17:44:10` ✅ matches live test timestamp
- Jan 1 epoch → `2026-04-28 17:44:10` ✗ one day in the past

The TC_B V2 document specifies Jan 1, but the M7 device firmware itself uses Jan 2 as its internal epoch. Changing to Jan 1 would introduce a 1-day error on M7 hardware.

**No action required** for M7 targets. If targeting other Anviz device families, verify against their firmware before changing.

---

## Section 4 — Data Model / Field Layout Issues

### 4.1 AdvancedSettings — MemoryFullAlarm Field Width — ✅ VERIFIED (M7 2026-04-29)

**Status**: No bug. 3-byte field is correct for M7. Code is correct.

**File**: `Anviz.SDK/Responses/AdvancedSettings.cs`

**Evidence**: The 0x32 response from M7 returned DATA=15 bytes total. With `MemoryFullAlarm(3B)=100` (plausible: alarm at 100% full) vs `MemoryFullAlarm(4B)=25600` (implausible), the 3-byte interpretation is clearly correct. The struct total of 15 bytes matches the current implementation.

**No action required.**

### 4.2 BasicSettings — Date/Time Format Byte — ✅ VERIFIED (M7 2026-04-29)

**Status**: No bug. Combined single-byte parsing is correct for M7.

**File**: `Anviz.SDK/Responses/BasicSettings.cs`

**Evidence**: The clock toggle test compared two 0x30 responses with 12-hour and 24-hour clock settings. Byte 14 changed (0x10 → 0x11), byte 15 was unchanged. Bit 0 of byte 14 is the clock format flag, exactly as the current code parses it. Byte 15 is correctly identified as the Attendance field.

**No action required.**

### 4.3 Record — Date Field Byte Order — ✅ VERIFIED (M7 2026-04-29)

**Status**: No bug. Big-endian byte order is correct for M7.

**File**: `Anviz.SDK/Responses/Record.cs`

**Evidence**: Two records downloaded on 2026-04-29. Raw bytes `[31 83 AE 6A]` and `[31 83 AE 6C]`:

| Interpretation | Seconds | DateTime |
|----------------|---------|---------|
| Big-endian (current) | 830,713,450 | 2026-04-29 17:44:10 ✅ |
| Little-endian | 1,789,821,745 | 2056-09-19 12:42:25 ✗ |

Big-endian produces the correct live timestamp. **No action required.**

### 4.4 TimeZone — Missing Sunday in ToArray() — 🐛 CONFIRMED BUG (M7 2026-04-29)

**Status**: Bug confirmed. `ToArray()` writes only 6 days; device sends 7 days.

**File**: `Anviz.SDK/Responses/AnvizTimeZone.cs`

**Evidence**: The 0x50 response returned DATA=28 bytes (7 days × 4 bytes). The constructor already loops 7 days (correct). However `ToArray()` loops `day < 6`, writing only 6 days (24 bytes), silently dropping Sunday when writing a timezone back to the device.

**Fix** (one line in `ToArray()`):
```csharp
// Before:
for (var day = 0; day < 6; day++)
// After:
for (var day = 0; day < 7; day++)
```

**File**: `Anviz.SDK/Responses/AnvizTimeZone.cs` — `ToArray()` method.

### 4.5 Password — PWDH8 High Byte Not Used (Low)

**File**: `Anviz.SDK/Utils/Bytes.cs`

`PasswordRead` / `PasswordWrite` encode the password as 20 bits (high 4 bits of byte 0 = length; remaining 20 bits = value). `UserInfo.cs` also reads a `PWDH8` byte (offset 27) that stores additional high bits for extended passwords. The current implementation ignores `PWDH8`, so passwords with values > 20 bits (> 1,048,575) are silently truncated.

**Action**: Low priority. Verify maximum password value in practice. If it can exceed 20 bits, include `PWDH8` in encoding/decoding.

---

## Section 5 — Error Handling Gaps (High)

**File**: `Anviz.SDK/AnvizStream.cs`, `Anviz.SDK/Responses/Response.cs`

The current code throws a generic exception on any non-SUCCESS `RET` byte. The protocol defines 7 distinct error codes that callers cannot distinguish without parsing the exception message text:

| Code | Name | Meaning |
|------|------|---------|
| 0x01 | ACK_FAIL | General failure |
| 0x04 | ACK_FULL | User memory full |
| 0x05 | ACK_EMPTY | User memory empty |
| 0x06 | ACK_NO_USER | User does not exist |
| 0x08 | ACK_TIME_OUT | Fingerprint capture timeout |
| 0x0A | ACK_USER_OCCUPIED | User already exists |
| 0x0B | ACK_FINGER_OCCUPIED | Fingerprint already registered |

**Action**: Create a typed `AnvizException` class with an `AnvizReturnCode` enum property. Throw this in place of bare `Exception` from `Response.FromStream`. This is a breaking API change for existing consumers; consider a minor version bump.

---

## Section 6 — Concurrency and Architecture Issues

### 6.1 AnvizStream Is Not Thread-Safe (Medium)

**File**: `Anviz.SDK/AnvizStream.cs`

`AnvizStream` holds a single `ResponseCode` field and a single `TaskCompletionSource` for the current in-flight command. If two callers concurrently await different commands, the second overrides `ResponseCode`, the first awaiter never completes (times out after 10 seconds), and the second may receive the wrong response.

**Action**: Add a `SemaphoreSlim(1, 1)` to `AnvizStream.SendAsync` to serialize all command invocations. Document `AnvizDevice` as requiring sequential calls (or move the semaphore there).

### 6.2 Background Thread Blocking on Async I/O (Low)

**File**: `Anviz.SDK/AnvizStream.cs`

The receiver runs as a `Thread` calling `async` methods via `.GetAwaiter().GetResult()`. This blocks the thread during async I/O rather than yielding, negating most of the async benefit. Consider replacing the background thread with a pure `async` receive loop started with `Task.Run`.

### 6.3 Ambiguous Connection-Lost vs. Timeout (Low)

**File**: `Anviz.SDK/AnvizStream.cs`

A clean TCP EOF (null response from `Response.FromStream`) and a 10-second command timeout are both handled by throwing `DeviceConnectionLostException`. They represent different conditions (graceful disconnect vs. unresponsive device) and should produce distinct exceptions or at minimum distinct log messages.

---

## Section 7 — Unimplemented Protocol Commands

The following TC_B V2 commands are absent from the SDK. They are grouped by priority.

### Priority A — Commonly needed

| CMD | Code | Description |
|-----|------|-------------|
| Clear Admin Flag | 0x3D | Clear all administrator flags; simple no-payload command |
| Get Device Capacity | 0x5D | Query max employee, FP, and record capacity |
| Get Factory Info | 0x4A | Manufacturer info code (10B ANSI / 20B Unicode) |
| Set Factory Info | 0x4B | |
| Get Comm Device ID | 0x74 | Communication device ID (separate from S/N) |
| Set Comm Device ID | 0x75 | |

### Priority B — Access control features

| CMD | Code | Description |
|-----|------|-------------|
| Get Group Info | 0x52 | Time zone groups 0–16, 4 timezone refs each |
| Set Group Info | 0x53 | |
| Get T&A State Table | 0x5A | 16 state labels (10B ANSI / 20B Unicode each) |
| Set T&A State Table | 0x5B | |
| Get Custom T&A Table | 0x70 | Full custom attendance state (161B ANSI / 321B Unicode) |
| Set Custom T&A Table | 0x71 | |

### Priority C — Messaging

| CMD | Code | Description |
|-----|------|-------------|
| Get Short Message | 0x56 | Retrieve message by index (59B ANSI / 107B Unicode) |
| Add Short Message | 0x57 | Add message with user ID, date range, content |
| Read All Message Headers | 0x58 | 50 messages × 11 bytes = 550 bytes |
| Delete Short Message | 0x59 | Delete by index (0xFF = delete all) |

### Priority D — Advanced / device-specific

| CMD | Code | Description |
|-----|------|-------------|
| Get Employee Enroll Timestamp | 0x3E | Seconds since 2000-01-01 |
| Set Employee Enroll Timestamp | 0x3F | |
| Read Random Number | 0x76 | 4-byte random number for encryption handshake |
| Encrypt Device Type / Language | 0x77 | 9-byte encrypted payload |
| OA3000 Messages | 0x26–0x29 | Larger message format (461B), 200 slots; OA3000 only |
| T&A State Auto Switch | 0x20/0x21 | Time-based T&A switching; OA1000/OA3000 only |
| Extended Staff (761 platform) | 0x22/0x23 | 84-byte record, max 6 per request; 761 platform only |

---

## Section 8 — Code Quality Improvements

### 8.1 `Bytes.Read` Has No Endianness Hint (Low)

**File**: `Anviz.SDK/Utils/Bytes.cs`

`Bytes.Read(byte[])` reverses the array before conversion, making it big-endian, but the name gives no indication of this. Several call sites may need a little-endian version once byte orders are confirmed.

**Action**: Rename to `ReadBigEndian` and add a `ReadLittleEndian` overload. Update all call sites.

### 8.2 GetStaffData/SetStaffData Use Extended Commands Only (Low)

The SDK uses 0x72/0x73 (extended format, max 8 per request, Unicode) exclusively. The basic commands 0x42/0x43 (max 12 per request, ANSI, 27 bytes) are not implemented. This may cause `ACK_FAIL` on older firmware that does not support the extended commands.

**Action**: Document this design decision. Consider falling back to 0x42/0x43 on failure from 0x72/0x73.

### 8.3 BiometricType String Lookup Is Fragile (Low)

**File**: `Anviz.SDK/BiometricType.cs`

`DecodeBiometricType` maps device type strings to biometric type via `StartsWith` comparisons. New device models require code changes.

**Action**: Move the mapping to a dictionary constant to ease future additions.

---

## Section 9 — Prioritized Implementation Roadmap

Items marked ✅ were verified against M7 hardware on 2026-04-29 and require no code changes.

| Priority | Item | File(s) | Status |
|----------|------|---------|--------|
| **High** | Fix `ToArray()` Sunday bug: `day < 6` → `day < 7` | `Responses/AnvizTimeZone.cs` | 🐛 Fix needed |
| **High** | Typed exceptions for 7 RET error codes (`AnvizException`) | `AnvizStream.cs`, `Responses/Response.cs` | Needed — confirmed by SetBasicSettings FAIL scenario |
| **Medium** | Thread-safety: `SemaphoreSlim` in `AnvizStream.SendAsync` | `AnvizStream.cs` | Unverified |
| **Medium** | Implement Group Info commands (0x52/0x53) | New partial class + commands | Unverified |
| **Medium** | Implement Device Capacity query (0x5D) | New partial class + command | Unverified |
| **Low** | Implement Clear Admin Flag (0x3D) | New partial class + command | Unverified |
| **Low** | Implement Factory Info (0x4A/0x4B) | New partial class + commands | Unverified |
| **Low** | Short message commands (0x56–0x59) | New partial class + commands | Unverified |
| **Low** | T&A State tables (0x5A/0x5B, 0x70/0x71) | New partial class + commands | Unverified |
| ~~Critical~~ | ~~Fix epoch: `2000-01-02` → `2000-01-01`~~ | `Utils/DateConversions.cs` | ✅ Jan 2 is correct for M7 |
| ~~Critical~~ | ~~Verify/fix SN command codes (0x24/0x25 vs 0x46/0x47)~~ | `Commands/GetDeviceSNCommand.cs` | ✅ 0x24 works on M7 |
| ~~High~~ | ~~Verify LEN endianness; fix if little-endian~~ | `Commands/Command.cs`, `Responses/Response.cs` | ✅ Big-endian confirmed |
| ~~High~~ | ~~Verify AdvancedSettings MemoryFullAlarm 3B vs 4B~~ | `Responses/AdvancedSettings.cs` | ✅ 3B confirmed, 15-byte total |
| ~~High~~ | ~~Verify Record Date byte order~~ | `Responses/Record.cs` | ✅ Big-endian confirmed |
| ~~Medium~~ | ~~Verify timezone 7-day layout~~ | `Responses/AnvizTimeZone.cs` | ✅ 28 bytes confirmed (see §4.4 bug) |
| ~~Medium~~ | ~~Verify BasicSettings date/time byte split~~ | `Responses/BasicSettings.cs` | ✅ Combined byte confirmed |

---

## Section 10 — Verification Steps

For remaining open items:

1. **Build**: `dotnet build Anviz.SDK/Anviz.SDK.csproj -c Release` — zero errors, zero warnings
2. **AnvizTimeZone fix**: After changing `day < 6` to `day < 7` in `ToArray()`, call `SetTimeZoneInfo()` with a timezone that includes Sunday hours and verify the device stores Sunday correctly.
3. **AnvizException**: After implementing typed exceptions, confirm that `SetBasicSettings` returning `RET=FAIL` throws a catchable `AnvizException` with `ReturnCode = AnvizReturnCode.Fail` rather than terminating the session.

---

## Section 11 — M7 Hardware Verification Results (2026-04-29)

All verification performed against an Anviz M7 fingerprint terminal via TCP on port 5010. Diagnostic logging was added to `Anviz.SDK` under a dedicated git branch to capture raw byte values from every response.

### Summary Table

| § | Issue | Result | Evidence |
|---|-------|--------|---------|
| 1.2 | LEN field endianness | ✅ **No bug** — big-endian confirmed | FP template 0x44: `[01 52]` = 338 bytes, CRC OK |
| 2.1 | SN command codes 0x24 vs 0x46 | ✅ **No bug** — 0x24 valid M7 extension | `GetDeviceSN`: CMD=0x24 → ACK=0xA4, 16 bytes, CRC OK |
| 3 | Epoch Jan 1 vs Jan 2 | ✅ **Review was wrong** — Jan 2 is correct | Records decoded to exact live timestamp; Jan 1 gives yesterday |
| 4.1 | MemoryFullAlarm 3B vs 4B | ✅ **No bug** — 3B correct, 15-byte total | `GetAdvancedSettings`: DATA=15 bytes; 3B=100 (plausible), 4B=25600 (implausible) |
| 4.2 | BasicSettings date/clock byte split | ✅ **No bug** — combined byte confirmed | Clock toggle: byte 14 changed (0x10→0x11), byte 15 unchanged |
| 4.3 | Record date byte order | ✅ **No bug** — big-endian confirmed | `[31 83 AE 6A]` big-endian→`2026-04-29 17:44:10`✅, little-endian→`2056-09-19`✗ |
| 4.4 | TimeZone Sunday missing | 🐛 **Bug confirmed** — `ToArray()` drops Sunday | Device sends 28 bytes (7 days); `ToArray()` loops `day < 6`, writes only 6 days |

### Additional Observations

- **Unsolicited ACK=0xDF packet**: Received immediately after the connection handshake (before any command was sent), with `awaiting=0x00 match=False`. The receiver correctly routed it to the `ReceivedPacket` event without error. This appears to be a push notification from the device (possibly a real-time attendance record); the existing event infrastructure handles it correctly.

- **§5 error handling in practice**: During `SetBasicSettings`, the device returned `RET=FAIL` (0x01). This caused `Response.FromStream` to throw a generic `Exception("RET: FAIL")`, which `AnvizStream` translated into `DeviceConnectionLostException`, killing the entire session. Typed `AnvizException` (§5) would allow callers to recover from `ACK_FAIL` without losing the connection.

### Raw Log Excerpts

**§1.2 LEN — fingerprint template download:**
```
[§1.2 LEN] Raw bytes: [01 52] big-endian=338 little-endian=20993 (current code uses big-endian=338)
[§1.2 LEN] DATA length after parse: 338 bytes, CRC OK
```

**§2.1 SN command:**
```
[§2.1] Sending command: expecting ACK=0xA4 from device
[§1.2/§2.1] Frame received: STX=0xA5 ACK=0xA4 DeviceID=[00 00 00 01]
[§1.2 LEN] DATA length after parse: 16 bytes, CRC OK
[§2.1] Receiver got ACK=0xA4 (awaiting=0xA4 match=True)
```

**§3 Epoch — two attendance records:**
```
[§4.3 Record] UserCode=54390 raw date bytes=[31 83 AE 6A] big-endian seconds=830713450 (→ 2026-04-29 17:44:10) little-endian seconds=1789821745 (→ 2056-09-19 12:42:25)
[§3 Epoch] raw seconds=0x3183AE6A (830713450d) epoch=2000-01-02 → DateTime=2026-04-29 17:44:10 (if epoch were 2000-01-01: 2026-04-28 17:44:10)
```

**§4.4 TimeZone — 28 bytes received:**
```
[§4.4 TimeZone] Raw DATA (28 bytes, expected 24 for 6-day or 28 for 7-day): 00 00 00 00 00 00 00 00 ... 00 00 00 00
[§4.4 TimeZone] Day[6] (Sunday): [00 00 00 00] From=00:00 To=00:00
```
