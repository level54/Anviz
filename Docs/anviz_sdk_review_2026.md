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

### 1.2 LEN Field Endianness — VERIFY REQUIRED (High)

**Issue**: The TC_B V2 protocol document states the LEN field is **little-endian**. Both `Command.cs` (when writing) and `Response.cs` (when reading) treat LEN as **big-endian**.

For payloads under 256 bytes the high byte is zero, so both interpretations produce identical results. This means the bug has gone undetected for all small-packet commands. However:

- Standard fingerprint templates: 338 bytes = 0x0152 → big-endian `[0x01][0x52]`, little-endian `[0x52][0x01]`
- Iris templates: 1280 bytes = 0x0500 → big-endian `[0x05][0x00]`, little-endian `[0x00][0x05]`

**Action required**: Capture a raw fingerprint template download packet (command 0x44) and inspect bytes 6–7 of the response. If the device sends `[0x52][0x01]` (low byte first), swap the serialization/deserialization to little-endian in both `Command.cs` and `Response.cs`.

**Files**: `Anviz.SDK/Commands/Command.cs`, `Anviz.SDK/Responses/Response.cs`

### 1.3 CRC16 Algorithm — Correct

`Anviz.SDK/Utils/CRC16.cs` implements table-driven CRC16 with initial value 0xFFFF. This is consistent with the Modbus CRC-16-IBM variant referenced in the protocol. No action required.

---

## Section 2 — Command Code Discrepancies (Critical / High)

Several commands use codes that do not appear in the TC_B V2 protocol document, and two use codes that conflict with documented commands.

### 2.1 Serial Number Commands — Wrong Codes (Critical)

| File | Code in SDK | Protocol Code | Protocol Description |
|------|-------------|---------------|----------------------|
| `GetDeviceSNCommand.cs` | **0x24** | **0x46** | Get device S/N (16 bytes) |
| `SetDeviceSNCommand.cs` | **0x25** | **0x47** | Modify device S/N |

Codes 0x24 and 0x25 are not documented anywhere in the TC_B V2 protocol. If the device firmware expects 0x46/0x47, these commands silently fail or return unexpected responses.

**Action**: Test both 0x24 and 0x46 against real hardware. If 0x46 elicits a correct response, replace the command codes.

**Files**: `Anviz.SDK/Commands/GetDeviceSNCommand.cs`, `Anviz.SDK/Commands/SetDeviceSNCommand.cs`

### 2.2 Undocumented Commands (Medium)

The following commands exist in the SDK but have no corresponding entry in the TC_B V2 protocol. They may be device-model-specific extensions or internal commands:

| File | Code | Notes |
|------|------|-------|
| `RebootDeviceCommand.cs` | 0x2E | Device reboot; not in TC_B V2 |
| `SetConnectionPassword.cs` | 0x04 | Auth password; not in TC_B V2 |
| `InquireCardCommand.cs` | 0x7E | Card inquiry; not in TC_B V2 |

**Action**: Document these as device-specific extensions with a comment referencing the device model(s) they apply to.

---

## Section 3 — Epoch / Date Encoding Bug (Critical)

**File**: `Anviz.SDK/Utils/DateConversions.cs`

```csharp
// Current (wrong):
private static readonly DateTime Epoch = new DateTime(2000, 1, 2);

// Correct:
private static readonly DateTime Epoch = new DateTime(2000, 1, 1);
```

The TC_B V2 protocol states:
> "Timestamp reference: January 1, 2000 00:00:00"

The SDK epoch is January **2**, 2000 — a one-day off-by-one error. Every attendance record timestamp decoded from or written to the device is off by **86,400 seconds (1 day)**.

**Action**: Change `new DateTime(2000, 1, 2)` to `new DateTime(2000, 1, 1)`. This is a one-line fix but has a broad impact on all `Record` timestamp decoding.

---

## Section 4 — Data Model / Field Layout Issues

### 4.1 AdvancedSettings — MemoryFullAlarm Field Width (High)

**File**: `Anviz.SDK/Responses/AdvancedSettings.cs`

The code reads `MemoryFullAlarm` as **3 bytes** at offset 8. The CrossChex SDK API (`CCHEX_GET_BASIC_CFG_INFO2_STRU_EXT_INF`) declares `record_over_alarm` as a **uint (4 bytes)**. If the 0x32 response uses a 4-byte field here, every subsequent field (`RepeatAttendanceDelay`, `DoorSensorDelay`, `ScheduledBellDelay`, `Reserved`) is shifted by 1 byte, causing silent mis-parsing.

The CrossChex SDK struct total is **14 bytes**; the current code models **15 bytes**.

**Action**: Capture a 0x32 response from a device and count the data bytes. Adjust `AdvancedSettings.cs` if the field is 4 bytes.

### 4.2 BasicSettings — Date/Time Format Byte (Medium)

**File**: `Anviz.SDK/Responses/BasicSettings.cs`

Offset 14 currently combines `DateFormat` (bits 7:1) and `Is24HourClock` (bit 0, inverted) in a single byte. The CrossChex SDK API separates `date_format` and `time_format` as distinct bytes in the 0x30 response. If byte 15 is a separate `time_format` byte in the actual device response, the current single-byte parsing is incorrect and the attendance mode field at offset 15 may be mis-identified.

**Action**: Verify the 0x30 response byte layout against a captured packet. If two separate bytes, split into distinct fields.

### 4.3 Record — Date Field Byte Order (High)

**File**: `Anviz.SDK/Responses/Record.cs`

The `Date` field (4 bytes at offset 5) is read using `Bytes.Read()`, which performs a big-endian conversion. The protocol document's general guidance for multi-byte fields is little-endian. Combined with the epoch bug in §3, this could produce completely wrong timestamps.

**Action**: Capture a 0x40 record download packet and compare the raw 4 bytes at offset 5 against the expected timestamp.

### 4.4 TimeZone — Missing Sunday (Medium)

**File**: `Anviz.SDK/Responses/AnvizTimeZone.cs`

The current model uses **6 days × 4 bytes = 24 bytes** per timezone zone. The CrossChex SDK API `CChex_GetPeriodTime` documents **7 days × 4 bytes = 28 bytes** per timezone. If the 0x50 response includes Sunday as a 7th day, the current deserialization drops Sunday and misaligns all subsequent zone data.

**Action**: Verify the 0x50 response length. If 28 bytes/zone, add Sunday to the `DayName` enum and fix the deserialization loop.

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

| Priority | Item | File(s) |
|----------|------|---------|
| **Critical** | Fix epoch: `2000-01-02` → `2000-01-01` | `Utils/DateConversions.cs` |
| **Critical** | Verify and fix SN command codes (0x24/0x25 vs 0x46/0x47) | `Commands/GetDeviceSNCommand.cs`, `Commands/SetDeviceSNCommand.cs` |
| **High** | Verify LEN field endianness via packet capture; fix if little-endian | `Commands/Command.cs`, `Responses/Response.cs` |
| **High** | Verify and fix AdvancedSettings field widths (3B vs 4B MemoryFullAlarm) | `Responses/AdvancedSettings.cs` |
| **High** | Verify Record Date byte order via packet capture | `Responses/Record.cs` |
| **High** | Typed exceptions for 7 RET error codes (`AnvizException`) | `AnvizStream.cs`, `Responses/Response.cs` |
| **Medium** | Thread-safety: `SemaphoreSlim` in `AnvizStream.SendAsync` | `AnvizStream.cs` |
| **Medium** | Verify timezone 7-day layout; add Sunday if needed | `Responses/AnvizTimeZone.cs` |
| **Medium** | Verify BasicSettings date/time byte split | `Responses/BasicSettings.cs` |
| **Medium** | Implement Group Info commands (0x52/0x53) | New partial class + commands |
| **Medium** | Implement Device Capacity query (0x5D) | New partial class + command |
| **Low** | Implement Clear Admin Flag (0x3D) | New partial class + command |
| **Low** | Implement Factory Info (0x4A/0x4B) | New partial class + commands |
| **Low** | Rename `Bytes.Read` → `Bytes.ReadBigEndian`; add `ReadLittleEndian` | `Utils/Bytes.cs` |
| **Low** | Short message commands (0x56–0x59) | New partial class + commands |
| **Low** | T&A State tables (0x5A/0x5B, 0x70/0x71) | New partial class + commands |

---

## Section 10 — Verification Steps

Before and after implementing fixes:

1. **Build**: `dotnet build Anviz.sln -c Release` — zero errors, zero warnings
2. **Packet capture**: Use Wireshark or tcpdump on port 5010 to capture:
   - A fingerprint template download (0x44) — inspect bytes 6–7 for LEN byte order
   - An attendance record download (0x40) — inspect bytes 5–8 of each 14-byte record for Date byte order
   - A GetDeviceSN command — compare responses to 0x24 vs 0x46
   - A GetAdvancedSettings response (0x32) — count data bytes
3. **Timestamp check**: After fixing the epoch, decode the first record's DateTime and compare against the device display
4. **Advanced settings check**: After fixing field widths, verify `MemoryFullAlarm`, `RepeatAttendanceDelay`, and `DoorSensorDelay` match values on the device configuration screen
5. **Sample project**: `dotnet run --project Sample/Sample.csproj` against real hardware; exercise `GetDateTime`, `GetRecordInfo`, `DownloadRecords`, `GetBasicSettings`, `GetAdvancedSettings`
