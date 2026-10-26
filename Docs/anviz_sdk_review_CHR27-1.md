# Anviz.SDK desk review against the protocol document (CHR27-1)

What this is: every command the SDK sends and every answer field it reads, set against `Docs/Communication
Protocol.pdf` ("TC_B Communication Protocol V2.1", 39 pages). Where the PDF and its markdown copy differ, the PDF wins.
The M7 column holds what the real M7 did in slice S6 (appendix *M7 runs*): of the 27 "M7 decides" rows, 10 match,
3 are kept as M7 deviations, 12 could not be decided on the M7, and 2 were fixed.

Written 2026-10-25 from the code on branch `CHR27-1` at `7144bdb` (the review: commit `d9e0687`). The Fake column
follows the fake device as of `ee7ab8a` (S2 fake-device tests) and `3357e74` (S2 review fixes). Row ids (`R40.5`,
`RF.10`, `RX.2`) are what tests and commits name.

## Summary

**Rows per verdict** (sections 1, 2 and 4 together; 182 rows):

| Verdict | Rows | What it means |
|---|---:|---|
| matches | 116 | The code does what the document says. |
| matches — the M7 confirms the code (document silent) | 10 | The document says nothing; the M7 does what the code does. |
| kept — M7 deviation | 3 | The code differs from the document and the M7 follows the code: the code stays, with a `// M7 deviation (CHR27-1 S6):` comment. |
| kept — not decided on the M7 | 12 | The M7 run could not show it (the reason is in the row); the code stays. |
| fixed (S6, commit) | 2 | The M7 follows the document (R73.11) or showed the code wrong (R46.3); fixed in the named commit. |
| deviates — kept, M7 cannot run it (C12) | 14 | The code differs from the document, but the M7 check may not run that command (forbidden, or outside the M7 list). Its layout stays. |
| M5 path — not tested (D8) | 2 | The ANSI (30-byte) staff record used by M5 devices. It stays as it is and is not tested on the M7. |
| fixed (S3, commit) | 17 | An S3 fault fixed in the named commit. |
| fixed (S4, commit) | 5 | A speed fault (buffers, allocations, timers, logging) fixed in S4 in the named commit. Speed gain on the fake: none measurable (section 5). |
| noted — no change (D3) | 1 | Found, but changing it would change the public behaviour (RX.8). |

Of the 27 "M7 decides" rows, 14 are places where the document is **silent** (it never says how a value is encoded)
and the code picked something; the M7 showed what the device really does (S6). Of the 18 S3 rows, 4 (R44.5, R45.5, R4C.5,
R5C.6) are the error-answer fault RF.10 seen from one command; they went with it. All 16 left in S3 are fixed, and RX.14 (the write timeouts), found in the S3 review, is fixed too; R73.11
and R73.13 (the per-user "stored" answer flag) moved to "M7 decides"; on the M7 the flag marks a new and an updated user as stored, as the document says, so
R73.11 is fixed in S6 (R73.13, the ANSI record, is not run on the M7).

**Key to the codes in this document**

| Code | Meaning |
|---|---|
| S2, S3, S4, S6 | Slices of CHR27-1: S2 this review and the fake-device tests; S3 fixes the frame faults; S4 the speed work; S6 runs the real M7 and fills the M7 column. |
| D2 | What the M7 check may do: reads, and one test user added and removed. It never clears records, resets to factory settings, reboots, or changes the network settings, device id or serial number: "forbidden (D2)". |
| D3 | The public API stays as it is; only the inside of the SDK changes. |
| D4 | Where code and document differ, the M7 decides: the code is fixed if the M7 follows the document, kept with a comment if the M7 follows the code. This is the "M7 decides (S6)" verdict. |
| D8 | The M5 path (the ANSI staff record) and other models stay as they are; a layout changes only where the document and the M7 agree. |
| D9, D10 | The M7 check may set the clock (D9) and enrols one finger, the owner's (D10). |
| C3 | Speed is measured before and after on the fake device (section 5). In S4 a change stays if the measurement shows a gain **or** it removes the fault its row names. |
| C4 | The transport may be rewritten inside, with the same timeouts and events. |
| C6 | The "new records" request changes the device's new-record marks, so it is proved on the fake only, never on the M7. |
| C12 | A command the M7 check cannot run keeps the layout the code has today (sites run it); only frame faults are fixed for it. This is the "kept, M7 cannot run it (C12)" verdict. |
| C13 | The M7 test user: name, password, card and one made-up fingerprint template. |
| C14 | Speed is also timed on the M7 (S6); "no measurable gain" is a valid result. |
| `_Today` | A test whose name ends `_Today` pins what the SDK does today where it differs from the document or the document is silent; the row it pins says so. |
| CH | The 4-byte device id at the start of every frame. |
| CMD / ACK | The command code; the device's answer code (ACK) is the command + 0x80. |
| RET | The answer's return code: 0x00 ACK_SUCCESS, 0x01 ACK_FAIL, 0x06 ACK_NO_USER and five more (RF.9). |
| LEN | The 2-byte count of the DATA bytes. |

**The five worst findings, in plain words**

1. **Any error answer costs the connection** (RF.10, RF.11). When the device says "no such user" or "failed", the SDK
   throws, leaves two bytes of that answer unread, then trips over them and closes the connection. An error answer
   for a command nobody is waiting on still fails whatever call is waiting. Chronos meets this when it deletes or reads
   a user the device does not have. Fixed in S3.
2. **An exception in the background reader ends the whole program** (RX.1). The reader is an `async void` method: if
   sending the ping answer fails, or a caller's event handler throws, nothing catches it and the process stops. Fixed
   in S3.
3. **Dispose can hang for ever** (RX.2). When the code that resumes after an answer runs on the reader's own thread and
   calls `Dispose`, Dispose waits for that thread to finish — that is, for itself. S1 saw this happen in a test. Fixed
   in S3.
4. **"Delete all users" sends data the document says it does not take** (R4D.1). Chronos calls it. The document gives
   0x4D no data; the SDK sends 6 bytes. A device that follows the document refuses it, and today that refusal also
   closes the connection (point 1, fixed in S3). The M7 may not run it (forbidden), so the layout stays unless the owner rules.
5. **Clocking times are right on the M7** (R40.5). The SDK counts record seconds from 2 January 2000; the document says
   "since the year 2000". The M7 counts from 2 January too (a clocking read back at its PC time), as the CrossChex SDK
   document says: the code stays. Every clocking Chronos downloads goes through this.

Also worth knowing: a frame that arrives in more than one TCP piece closes the connection (RF.15, fixed in S3), and
setting users ignores the device's per-user "stored / not stored" answer (R73.11; fixed in S6).

**How to read the tables.** Each SDK command has one table. Its heading gives the command code, the public method,
whether Chronos calls it, and the M7 check: *runs*, *forbidden (D2)*, *not run (C6)* (the "new records" family), or
*not run (outside the M7 list)* — the M7 check only reads, adds/reads/removes one test user (C13), sets the clock (D9)
and enrols one finger (D10). Columns: **Document** (what the PDF says, with its section number), **Code** (what the SDK
does), **Fake** (what the test fake device does: *doc* = from the document, *code* = follows the code because the
document is silent, *refuses* = the fake answers ACK_FAIL and keeps a frame error, because the command is not in the
document or its data length is not the document's, *—* = the fake does not serve that case), **M7** (what the M7 did
in S6: *same* = as the code expects; *not run*; *not seen* = the case did not arise), **Verdict**. Under each table, *Fields* counts the document's fields for that command and names the rows that
cover them.

Byte positions follow the document: byte 1 is the first DATA byte.

## 1. The frame

Document Part I. These rows hold for every command; command tables refer back to them.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| RF.1 | STX | 0xA5 | 0xA5 | doc | same | matches |
| RF.2 | CH (device code) | 4 bytes, "IDHH, IDHL, IDLH, IDLL" (high byte first). CH 0: every connected device answers. | High byte first. The first command (0x48 at connect) goes with CH 0; the SDK then takes the answer's CH as its device id. | doc | same: the 0x48 at connect went with CH 0; the M7 answered with CH 1 | matches |
| RF.3 | CMD / ACK | 1 byte; the answer's ACK = command + 0x80 | Same; the waiting call is matched on command + 0x80 | doc | same | matches |
| RF.4 | LEN byte order | 2 bytes; no sentence gives the order, but every LEN example in the PDF writes high byte first: `0x01 0x52` (338, 0x44 answer), `0x05 0x00` (1,280, iris 0x44 answer), `0x01 0x58` (344, 0x45), `0x05 0x06` (1,286, iris 0x45). | High byte first | doc | same (the 0x44 answer: `0x01 0x52`, 338) | matches |
| RF.5 | Multi-byte numbers inside DATA (user code, card, amounts, record time, port, memory alarm) | Order never stated | High byte first everywhere | code | card 12345678 sent `00 BC 61 4E`, read back 12345678; the 0x3C counts match the user list; 0x3A port `13 92` = 5010 | matches — the M7 confirms the code (document silent) |
| RF.6 | DATA length | "0-400Bytes" | No limit on send (the face template sends 15,366 bytes); reads up to 16,000. Only face devices use more than 400. | doc | not run (no frame over 400 bytes) | deviates — kept, M7 cannot run it (C12) |
| RF.7 | CRC16 algorithm | "CRC16 check means all data CRC16" — no polynomial, start value or range | CRC-16/MCRF4XX (polynomial 0x8408 reflected, start 0xFFFF, no final XOR) over STX up to the last DATA byte | code | the M7 accepts every frame's CRC; its answers check | matches — the M7 confirms the code (document silent) |
| RF.8 | CRC byte order | "CRCL CRCH" (the 0x47 answer table alone writes CRCH CRCL — a typo) | Low byte first | doc | same | matches |
| RF.9 | RET codes | 8 codes: 0x00 success, 0x01 fail, 0x04 full, 0x05 empty, 0x06 no user, 0x08 capture timeout, 0x0A user occupied, 0x0B finger occupied | All 8 known (`AnvizReturnCode`); any other value is thrown as an unnamed code | doc | same (0x00 and 0x06 seen) | matches |
| RF.10 | Error answer: "When the RET != ACK_SUCCESS, the DATA and LEN … are always 0" | An error answer is 11 bytes: header, RET, LEN 0x00 0x00, CRC | Reads the error answer to its end, checks the CRC, then throws `AnvizException` with its return code; the connection stays. Tests: `FrameTests.ErrorAnswer_IsAnvizException_AndConnectionStays`, `GetFingerprintTemplate_NoUser_Throws_AndTheConnectionStays`. | doc | same: 11-byte ACK_NO_USER answers to 0x44, the connection stays (the `development` build lost it there: appendix) | fixed (S3, `1e3d399`) |
| RF.11 | Which call an error answer belongs to | The ACK code names the command (command + 0x80) | An error answer fails only the call waiting for its code; an error answer for a code nobody waits for is raised as `ReceivedPacket`, as a stray success answer is (RF.17), never as `DeviceError` (Chronos takes `DeviceError` as a lost connection); the waiting call still gets its own answer and the connection stays. Test: `FrameTests.RF11_ErrorAnswerForAnotherCode_IsReceivedPacket_AndTheWaitingCallGetsItsAnswer`. | doc | not seen | fixed (S3, `33c0055`) |
| RF.12 | Frames the device starts (code below 0x80, no RET) | Command format: STX, CH, CMD, LEN (2), DATA, CRC — LEN is bytes 6-7 of the frame | Reads LEN from bytes 6-7. Test: `FrameTests.DeviceSentFrameWithData_IsReadWhole_AndRaisedAsReceivedPacket`. | doc (LEN 0 only: the 0x7F ping) | not seen | fixed (S3, `1e3d399`) |
| RF.13 | Device ping 0x7F | No ping in the document. 0x7F is section 83 "Sending Email", a PC-to-device command for the C5; its answer is 0xFF. | A device-sent 0x7F is taken as a ping: the SDK answers 0xFF (see RFF.1) and raises `DevicePing`. | code | not seen (no ping) | kept — not decided on the M7 (no device ping in any run) |
| RF.14 | Real-time record 0xDF (section 41) | Device sends ACK 0xDF, RET, LEN 0x00 0x0E, 14-byte record (layout as R40.4–R40.8); "only response message" — the PC sends nothing back | Raises `ReceivedRecord` with the record; sends nothing back. The record's time is read from 2 January 2000, as R40.5 (right on the M7). Pinned: `ReceivedRecord_RealTimeRecord0xDF_RaisesItWithTheRecord_Today`. M7: seen only if real-time mode is already on (the check does not switch it on). | doc | not seen | matches |
| RF.15 | Frame split over several TCP reads | — (TCP may split any frame) | Every part of the frame is read until full. Tests: `FrameTests.SplitHeader_IsReadWhole`, `FrameTests.SplitEverywhere_IsReadWhole`. | doc (can split an answer) | same (every answer read whole) | fixed (S3, `1e3d399`) |
| RF.16 | Bad STX or bad CRC | Document silent on what to do | `DeviceError` raised, connection closed, no attempt to find the next frame. Pinned: `DeviceError_BadCrc_RaisesIt_AndClosesTheConnection`. | doc | not seen | matches |
| RF.17 | Answers nobody asked for | — | An answer with no call waiting for its code is raised as `ReceivedPacket` (kept). An answer completes the waiting call only when it has its code and its first byte came after the command was written (bytes the reader has already counted, or waiting in the socket, are earlier frames'; the count is read first, so no byte is counted twice); a repeat already counted is raised as `ReceivedPacket`. A repeat whose bytes the reader has read but not yet counted, or that is still on the wire, when the command goes may still be taken as the answer: the protocol has no sequence number to tell them apart. CH is not compared (one device per connection). Test: `FrameTests.RF17_StrayRepeatOfAnAnswer_DoesNotCompleteTheNextCall`. | doc | not seen | fixed (S3, `72fb41e`) |

`Command.cs` builds every frame as above (RF.1–RF.8); `PongCommand.cs` has its own table in section 2 (0xFF).

## 2. The commands

### 0x04 — SetConnectionPassword(user, password) · Chronos uses: no · M7 check: not run (outside the M7 list)

Not in the document. Also sent by `AnvizManager.Connect` when `AuthenticateConnection` is true. The CrossChex SDK
document (`crosschex_new_sdk_api_20180710.md`, section 2.55) describes 0x04 as connection authentication — a sign-in
with the device's communication password, not a change of it: "Byte[12] username; Byte[12] password", the user name
used only by iris devices.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R04.1 | The command | Not in the document (CrossChex: sign-in, 24 bytes) | Sends 0x04 with 24 bytes; the method name says "set" | refuses | not run | deviates — kept, M7 cannot run it (C12) |
| R04.2 | User 1-12, password 13-24 | Not in the document (CrossChex: 12 + 12 bytes) | ASCII, zero-padded; a user name over 12 characters runs into the password bytes, over 24 throws | refuses | not run | deviates — kept, M7 cannot run it (C12) |

*Fields:* none in the document (2 in CrossChex: R04.2).

### 0x24 — GetDeviceSN() · Chronos uses: no · M7 check: runs

Section 61.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R24.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R24.2 | Serial number, bytes 1-16 | 16 bytes, encoding not stated | ASCII, trailing zeros trimmed | code | ASCII "1590100019260166" (seen in the after-fix run only: R46.3) | matches — the M7 confirms the code (document silent) |

*Fields:* 1 (R24.2).

### 0x25 — SetDeviceSN(value) · Chronos uses: no · M7 check: forbidden (D2)

Section 62.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R25.1 | Serial number, bytes 1-16 | 16 bytes, encoding not stated | ASCII, zero-padded; over 16 characters throws | code | forbidden (D2) | deviates — kept, M7 cannot run it (C12) |

*Fields:* 1 (R25.1).

### 0x2E — RebootDevice() · Chronos uses: no · M7 check: forbidden (D2)

Not in the document (it lists 0x2A–0x2D and 0x2F; no 0x2E).

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R2E.1 | The command | Not in the document | Sends 0x2E with one byte 0x60 (named "REBOOT_PARAM_UNKNOWN" in the code) | refuses | forbidden (D2) | deviates — kept, M7 cannot run it (C12) |

*Fields:* none in the document.

### 0x30 — GetBasicSettings() · Chronos uses: no · M7 check: runs

Section 1. Answer 18 bytes.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R30.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R30.2 | Firmware version, 1-8 | ASCII | `Firmware`, ASCII, trailing zeros trimmed | doc | same: "06.48.M1" | matches |
| R30.3 | Communication password, 9-11 | Byte 9 bits 7-4 = length; bits 3-0 + bytes 10-11 = password | `ManagementPassword` = the 20-bit value; the length is not kept | doc | same: 0 | matches |
| R30.4 | Sleep time, 12 | 0-250 minutes | `Sleep` | doc | same: 10 | matches |
| R30.5 | Volume, 13 | 0-5, 0 mute | `Volume` Off…Maximum = 0-5 | doc | same: Medium | matches |
| R30.6 | Language, 14 | 0-5: simplified Chinese, traditional Chinese, English, French, Spanish, Portuguese | `Language`, the same 0-5 | doc | same: English | matches |
| R30.7 | Date/time format, 15 | Bits 7-4 date: 0 Chinese, 1 America, 2 English. Bits 3-0 time: 0 = 24 h, 1 = 12 h. | `DateFormat` = byte AND 0xFE with YYMMDD = 0x08, MMDDYY = 0x10, DDMMYY = 0x20; `Is24HourClock` = bit 0 is 0. America (0x10) and English (0x20) agree; Chinese (0x00) reads as no named value, 0x08 is not in the document, and time-format bits 1-3 leak into `DateFormat`. | doc | 0x10 (America, 24 h) | kept — not decided on the M7 (the M7 sends 0x10 = America/24 h, a value code and document agree on) |
| R30.8 | Attendance state, 16 | 0-15 | `Attendance` | doc | same: 0 | matches |
| R30.9 | Language setting flag, 17 | 0x10 = menu language may change | `LangCHG` | doc | same: 0 | matches |
| R30.10 | Command version, 18 | 0x01 / 0x02 (which staff commands the device answers) | `CMDVersion` | doc | same: command version 3 | matches |

*Fields:* 9 (R30.2–R30.10). The document's LEN example `0x00 0x12` is right for 18 bytes.

### 0x31 — SetBasicSettings(value) · Chronos uses: no · M7 check: not run (outside the M7 list)

Section 2. Data 10 bytes. (The heading says 20 bytes for the frame, right; the table says "12Byte" data, wrong; LEN
`0x00 0x0A` is right.)

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R31.1 | LEN | 10 | 10 | doc | not run | matches |
| R31.2 | Password and length, 1-3 | Byte 1 bits 7-4 = length; bits 3-0 + bytes 2-3 = password (the text says "Byte(10-11)", a copy slip) | Length = number of decimal digits; 20-bit value | doc | not run | matches |
| R31.3 | Sleep, volume, language, 4-6 | As R30.4–R30.6 | Same | doc | not run | matches |
| R31.4 | Date/time format, 7 | As R30.7 | `DateFormat` + (24 h ? 0 : 1) — the same encoding as R30.7. If S6 changes the R30.7 read, this write stays as it is (C12). | doc | not run | deviates — kept, M7 cannot run it (C12) |
| R31.5 | Attendance state, language flag, 8-9 | As R30.8–R30.9 | Same | doc | not run | matches |
| R31.6 | Reserved, 10 | Reserved | 0 | doc | not run | matches |
| R31.7 | "If you just modify some of the items … set them as 0xFF" | Optional | Always writes every item (the caller reads, changes, writes back) | doc (0xFF "leave this item" not modelled) | not run | matches |

*Fields:* 8 (R31.2: 1, R31.3: 3, R31.4: 1, R31.5: 2, R31.6: 1).

### 0x32 — GetAdvancedSettings() · Chronos uses: no · M7 check: runs

Section 3. Answer 15 bytes.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R32.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R32.2 | Fingerprint precision, 1 | 0-2 low/medium/high | `FPPrecision` Low/Medium/High | doc | same: Low | matches |
| R32.3 | Fixed Wiegand head code, 2 | 1-254 | `WiegandHead` | doc | same: 128 | matches |
| R32.4 | Wiegand option, 3 | 0-3 | `WiegandMode` (raw byte) | doc | same: 0 | matches |
| R32.5 | Work code permission, 4 | 0/1 | `WorkCode` (non-zero = true) | doc | same: on | matches |
| R32.6 | Real-time mode, 5 | 0/1 | `RealTimeMode` | doc | same: on | matches |
| R32.7 | FP auto update, 6 | 0/1 | `FPAutoUpdate` | doc | same: on | matches |
| R32.8 | Relay mode, 7 | 0 lock, 1 bell | `RelayMode` Door = 0, Bell = 1 (also None = 3, not in the document; harmless on read) | doc | same: Door | matches |
| R32.9 | Lock delay, 8 | 0-15 s | `LockDelay` | doc | same: 5 | matches |
| R32.10 | Memory full alarm, 9-11 | 0-5000, 3 bytes | `MemoryFullAlarm`, 3 bytes (order: RF.5) | doc | same: 100 | matches |
| R32.11 | Repeat attendance delay, 12 | 0-250 min | `RepeatAttendanceDelay` | doc | same: 0 | matches |
| R32.12 | Door sensor delay, 13 | 0-250 s | `DoorSensorDelay` | doc | same: 0 | matches |
| R32.13 | Scheduled bell delay, 14 | 0-15 s | `ScheduledBellDelay` | doc | same: 5 | matches |
| R32.14 | Reserved, 15 | Reserved | `Reserved` | doc | same | matches |

*Fields:* 13 (R32.2–R32.14).

### 0x33 — SetAdvancedSettings(value) · Chronos uses: no · M7 check: not run (outside the M7 list)

Section 4. Data 15 bytes, the same layout as 0x32.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R33.1 | LEN | 15 | 15 | doc | not run | matches |
| R33.2 | All 13 fields, 1-15 | As R32.2–R32.14 | Written at the same positions (memory alarm order: RF.5) | doc | not run | matches |

*Fields:* 13 (R33.2). The R32 read on the M7 confirms the shared layout.

### 0x38 — GetDateTime() · Chronos uses: no · M7 check: runs

Section 5. Answer 6 bytes.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R38.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R38.2 | Year, 1 | One byte, base not stated | 2000 + byte | code | year byte 0x1A = 2026 | matches — the M7 confirms the code (document silent) |
| R38.3 | Month, day, hour, minute, second, 2-6 | One byte each | Read as they are | doc | same | matches |

*Fields:* 6 (R38.2: 1, R38.3: 5).

### 0x39 — SetDateTime(dateTime) · Chronos uses: yes · M7 check: runs (D9)

Section 6. Data 6 bytes; the answer has no data.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R39.1 | LEN | 6 | 6 | doc | same | matches |
| R39.2 | Year, 1 | One byte, base not stated | Year − 2000 | code | year byte 0x1A = 2026 | matches — the M7 confirms the code (document silent) |
| R39.3 | Month, day, hour, minute, second, 2-6 | One byte each | As given (the caller's clock, no time-zone change) | doc | same: set and read back to the second | matches |

*Fields:* 6 (R39.2: 1, R39.3: 5).

### 0x3A — GetTcpParameters() · Chronos uses: no · M7 check: runs

Section 7. Answer 27 bytes.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R3A.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R3A.2 | IP 1-4, mask 5-8, gateway 15-18, server IP 19-22 | 4 bytes each, octet order not stated | First octet first (`IPAddress(bytes)`) | code | IP 10.31.241.240 read as such (mask 255.255.255.0, gateway 10.31.241.1) | matches — the M7 confirms the code (document silent) |
| R3A.3 | MAC, 9-14 | 6 bytes | `PhysicalAddress`, first byte first | doc | same | matches |
| R3A.4 | Far limit, 23 | 1 byte | `FarLimit` | doc | same: 0 | matches |
| R3A.5 | Com port no., 24-25 | 2 bytes | `ComPort` (order: RF.5) | doc | same: 5010 | matches |
| R3A.6 | TCP/IP mode, 26 | 0 server, 1 client | `TcpMode` Server = 0, Client = 1 | doc | same: Server | matches |
| R3A.7 | DHCP limit, 27 | "DHCP limit", values not defined | `DhcpEnabled` = non-zero | code | 0 (a fixed-address M7) | kept — not decided on the M7 (byte 0 on a fixed-address M7; non-zero not seen) |

*Fields:* 9 (R3A.2: 4, R3A.3–R3A.7: 1 each).

### 0x3B — SetTCPParameters(value) · Chronos uses: no · M7 check: forbidden (D2)

Section 8 (its "Function" line says "Get", a copy slip). Data 27 bytes, the layout of 0x3A.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R3B.1 | LEN | 27 | 27 | doc | forbidden (D2) | matches |
| R3B.2 | All 9 fields | As R3A.2–R3A.7 | Written at the same positions with the same encodings as the R3A read (octet order and DHCP value as R3A.2, R3A.7). If S6 changes the read, this write stays as it is (C12). | code | forbidden (D2) | deviates — kept, M7 cannot run it (C12) |

*Fields:* 9 (R3B.2).

### 0x3C — GetDownloadInformation() · Chronos uses: yes · M7 check: runs

Section 9. Answer 18 bytes. (The LEN example `0x00 0x22` is wrong for 18 bytes; the code takes LEN from the frame.)

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R3C.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R3C.2 | User 1-3, FP 4-6, password 7-9, card 10-12, all records 13-15, new records 16-18 | 3 bytes each | `UserAmount`, `FingerPrintAmount`, `PasswordAmount`, `CardAmount`, `AllRecordAmount`, `NewRecordAmount` at those positions (order: RF.5) | doc | same: 13 users, 22 fingerprints, 12 passwords, 13 cards, 0 records, 0 new; the user count matches the 0x72 list | matches |

*Fields:* 6 (R3C.2).

### 0x40 — DownloadRecords(onlyNew) · Chronos uses: yes · M7 check: runs for all records; not run (C6) for `onlyNew: true`

Section 10. Request 2 bytes; answer N + N × 14 bytes. `DownloadRecords` first reads 0x3C for the count, then pages.
Raises `RecordDownloaded` per record.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R40.1 | Parameter, 1 | 0 next, 1 restart all, 2 restart new, 0x10 send last again | 1 (or 2 for new) on the first page, 0 after; never 0x10. Parameter 2 is proved on the fake (C6): `DownloadRecords_OnlyNew_Sends2ThenPages_ReadsOnlyTheNewRecords`. | doc (0, 1 and 2; 0x10 not modelled) | same: parameter 1 with amount 1 for the 1 record (enrol run 2), never 2 | matches |
| R40.2 | Record amount, 2 | ≤ 25 | Lesser of 25 and the records still to read | doc | same (1) | matches |
| R40.3 | Valid records N, answer byte 1 | N | Pages until N = 0 or the 0x3C count is reached | doc | same: N = 1 | matches |
| R40.4 | User code, 1-5 | 5 bytes | `UserCode` (order: RF.5) | doc | same: user code 1 | matches |
| R40.5 | Date&time, 6-9 | "how many seconds is it since the year 2000" (the worked example is garbled) | Seconds from **2 January 2000** (`DateConversions.RecordEpoch`). The CrossChex SDK document says "The number of seconds after 2000.1.2". Pinned: `DownloadRecords_All_Reads60RecordsIn3Pages_RaisingRecordDownloaded_Today` (the fake counts from 1 January, the SDK returns one day later). | doc | 0x326FDB81 seconds read from 2 January 2000 = 2026-10-25 21:11:29, the PC time of the clocking; from 1 January it would be a day early. record `01 | 00 00 00 00 01 | 32 6F DB 81 | 02 | 80 | 00 00 00` (one clocking the owner made at about 21:11 PC time, after the check set the M7 clock to the PC) | kept — M7 deviation |
| R40.6 | Backup code, 10 | 3 card, 2 password, 1 FP2, 0 FP1 | `BackupCode`, raw byte | doc | the M7 sent 2 (how the owner verified was not recorded) | matches |
| R40.7 | Record type, 11 | Bit 7 = may open door; low 4 bits = attendance state | `RecordType`, raw byte | doc | same: 0x80 (bit 7 set, state 0) | matches |
| R40.8 | Work types, 12-14 | 3 bytes | `WorkType` (order: RF.5) | doc | same: 0 | matches |

*Fields:* 8 (R40.1–R40.8). The document's LEN cell "(1 + N * 13)" is a slip for 14; the code uses the frame's LEN.

### 0x41 — SetRecords(record) · Chronos uses: no · M7 check: not run (outside the M7 list — it would add a clocking)

Section 11 (its command code is printed "041" and its LEN `0x00 0x0D` = 13 for 14 bytes — both slips).

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R41.1 | LEN | 14 bytes | 14 | doc | not run | matches |
| R41.2 | User code 1-5, backup code 10, record type 11, work code 12-14 | As R40.4, R40.6–R40.8 | Same positions | doc | not run | matches |
| R41.3 | Date&time, 6-9 | Seconds since 2000 | Seconds from 2 January 2000, the same epoch as R40.5. If S6 changes R40.5, this upload keeps today's epoch (C12). Pinned: `SetRecords_Sends14Bytes_Today`. | doc | not run | deviates — kept, M7 cannot run it (C12) |

*Fields:* 5 (R41.2: 4, R41.3: 1).

### 0x44 — GetFingerprintTemplate(employeeID, finger) · Chronos uses: yes · M7 check: runs (the test user, C13)

Section 14. Request 6 bytes; answer 338 bytes (1,280 on iris devices).

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R44.1 | User code, 1-5 | 5 bytes | 5 bytes (order: RF.5) | doc | same | matches |
| R44.2 | Backup code, 6 — FP1 / FP2 | 1 = FP1, 2 = FP2 | `finger + 1`: RightThumb → 1, RightIndex → 2 (the only two Chronos uses) | doc | same: FP1, 338 bytes, as sent | matches |
| R44.3 | Backup code, 6 — other fingers | Only 1 and 2 defined | RightMiddle … LeftThumb → 3 … 10. CrossChex lists "10 Fingerprints (1~10)". | code (keeps any backup code) | ACK_NO_USER, not ACK_FAIL, for backup codes 3-10 on a user holding FP1 only: the M7 does not refuse them | kept — not decided on the M7 (it did not refuse codes 3-10; whether they are fingers was not shown) |
| R44.4 | Template | 338 bytes (iris 1,280) | Returns DATA as it came, whatever its length | doc | same: 338 bytes | matches |
| R44.5 | ACK_NO_USER | Error answer, LEN 0 | Throws `AnvizException(NoUser)`; the connection stays (RF.10). Test: `GetFingerprintTemplate_NoUser_Throws_AndTheConnectionStays`. | doc | same: ACK_NO_USER, the connection stays | fixed (S3, `1e3d399`) |

*Fields:* 3 (R44.1, R44.2 + R44.3 for the one backup field, R44.4).

### 0x44 — GetFaceTemplate(employeeID) · Chronos uses: no · M7 check: runs (the FP1 frame; the M7 has no face, so it answers its FP1 template)

The document has no face template command. The SDK reuses 0x44.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R44.6 | The command | 0x44 is the fingerprint template; no face variant (only iris, 1,280 bytes) | 0x44 with backup code 1, the frame of R44.1/R44.2 | doc (answers the FP1 frame; no face) | the FP1 template (the M7 is a finger device) | kept — not decided on the M7 (a finger device cannot show the face path) |
| R44.7 | Answer length | Up to 400 bytes (RF.6) | Returns DATA raw; FacePass 7 sends 15,360 bytes (code comment) | doc (338 bytes; no face answer) | 338 bytes (the FP1 template) | kept — not decided on the M7 (a finger device cannot show the face path) |

*Fields:* none of its own (the request is the 0x44 request).

### 0x45 — SetFingerprintTemplate(employeeID, finger, template) · Chronos uses: yes · M7 check: runs (one made-up template, C13)

Section 15. Data 344 bytes.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R45.1 | LEN | 344 (`0x01 0x58`) | 344 | doc | same: LEN 344 | matches |
| R45.2 | User code, 1-5 | 5 bytes | 5 bytes | doc | same | matches |
| R45.3 | Backup code, 6 | 1 FP1, 2 FP2 | `finger + 1`, as R44.2 / R44.3 | doc | same: FP1 | matches (other fingers: R44.3) |
| R45.4 | Template, 7-344 | 338 bytes | A shorter template is padded with zeros; a longer one throws `ArgumentException`. The frame is always 344 bytes. | doc | same: 338 bytes, read back unchanged | matches |
| R45.5 | Error answers (fail, no user) | Error answer, LEN 0 | Throws `AnvizException`; the connection stays (RF.10) | doc | not seen (0x45 answered success) | fixed (S3, `1e3d399`) |

*Fields:* 3 (R45.2–R45.4). The iris variant (1,286 bytes) is not in the SDK — section 3.

### 0x45 — SetFaceTemplate(employeeID, template) · Chronos uses: no · M7 check: not run (the M7 has no face; a 15,366-byte frame)

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R45.6 | The command | 0x45 is the fingerprint template; no face variant | 0x45 with user code, backup code 1 and a 15,360-byte template (a longer one throws) | refuses | not run | deviates — kept, M7 cannot run it (C12) |
| R45.7 | LEN | Up to 400 (RF.6) | 15,366 | refuses | not run | deviates — kept, M7 cannot run it (C12) |

*Fields:* none of its own.

### 0x46 — GetDeviceID() (obsolete) · Chronos uses: no · M7 check: runs

Section 16, titled "Get device S/N" but described as "Get device ID which we set in device".

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R46.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R46.2 | Device ID, 1-4 | 4 bytes | 4 bytes (order: RF.5) | doc | same: 4 bytes, 0x31353930 | matches |
| R46.3 | Which id that is | 0x46 is the id "set in device" (menu); the document also has 0x74 "communication device ID". It does not say which one is CH. | Took the 0x46 answer as its CH for every later command. Now: `GetDeviceID` returns the 0x46 id and the CH from Connect stays. Test: `OtherCommandsTests.R46_3_GetDeviceID_ReturnsTheAnswer_AndKeepsItsDeviceId`. | code (one id, its CH) | 0x46 answers 0x31353930; the M7's CH is 1, and a command sent to 0x31353930 got no answer | fixed (S6, `23cf990`) |

*Fields:* 1 (R46.2).

### 0x47 — SetDeviceID(newDeviceId) · Chronos uses: no · M7 check: forbidden (D2)

Section 17 ("Modify device ID in device menu").

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R47.1 | Device ID, 1-4 | 4 bytes | 4 bytes (order: RF.5) | doc | forbidden (D2) | matches |
| R47.2 | After success | Menu id; 0x75 is the "communication device ID" | Takes the new id as its CH, unless its CH was 0. Whatever S6 finds for R46.3, this stays as it is (C12). | code | forbidden (D2) | deviates — kept, M7 cannot run it (C12) |

*Fields:* 1 (R47.1).

### 0x48 — GetDeviceTypeCode() · Chronos uses: yes · M7 check: runs

Section 18. Answer 8 bytes. Sent by every `Connect` (through `GetDeviceBiometricType`).

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R48.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R48.2 | Type code, 1-8 | 8 bytes; example `"TC400"000` | `DeviceType`, ASCII, trailing zeros trimmed | doc | same: "M7A+-N" | matches |
| R48.3 | Answer CH | Part I note 3: CH 0 reaches every device | Takes the answer's CH as `DeviceId` | doc | same: CH 1 | matches |
| R48.4 | Type code → biometric type | Not in the document | `BiometricTypes`: "M7A+-N" → Finger, "M5…" → Finger, "FACE7" → Face, others → Unknown. Pinned: `GetDeviceBiometricType_DecodesTheTypeCode_Today`. | code | "M7A+-N" → Finger | matches — the M7 confirms the code (document silent) |

*Fields:* 1 (R48.2).

### 0x4C — DeleteEmployeesData(id), (user), (id, method) · Chronos uses: yes · M7 check: runs (the test user, C13)

Section 22. Data 6 bytes; answer success or ACK_NO_USER.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R4C.1 | User code, 1-5 | 5 bytes | 5 bytes (order: RF.5) | doc | same | matches |
| R4C.2 | Backup code 0xFF | 0xFF = delete everything of the user, staff info too | `DeleteEmployeesData(id)` and `(user)` send 0xFF | doc | same: the test user deleted | matches |
| R4C.3 | Backup code bits 0-3 | Bit 3 card, 2 password, 1 FP2, 0 FP1 (staff info stays) | `(id, method)` sends the `VerificationMethod` byte: RightThumb 0x01, RightIndex 0x02, Password 0x04, RFCard 0x08 match | doc | not run | matches |
| R4C.4 | Backup code bits 4-7 | Not defined | `VerificationMethod` also has RightMiddle 0x10 … LeftThumb 0x80; they are not single bits (RightLittle 0x30 = RightMiddle + RightAnular) | doc (bits 4-7 ignored) | 0xFF deleted the whole user, as R4C.2 says; the single bits 0x10-0x80 were never sent | kept — not decided on the M7 (bits 4-7 alone were not sent) |
| R4C.5 | ACK_NO_USER | Error answer, LEN 0 | Throws `AnvizException(NoUser)`; the connection stays (RF.10). Test: `FrameTests.ErrorAnswer_IsAnvizException_AndConnectionStays`. | doc | not seen (the check deletes only the test user, which is there) | fixed (S3, `1e3d399`) |

*Fields:* 2 (R4C.1; the backup field: R4C.2–R4C.4).

### 0x4D — DeleteEmployeesData() · Chronos uses: yes · M7 check: forbidden (D2: delete all users)

Section 23 "Initialize the user area". Same SDK class as 0x4C.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R4D.1 | Data | None: LEN `0x00 0x00`, a 10-byte command | 6 bytes `00 00 00 00 00 FF` (the 0x4C layout copied). Pinned: `DeleteEmployeesData_All_Sends6Bytes_Today` — the fake refuses it with ACK_FAIL, as a device following the document would. | doc | forbidden (D2) | deviates — kept, M7 cannot run it (C12) |

*Fields:* 0 (the command has no data).

### 0x4E — DeleteAllRecords(), ClearNewRecords(), ClearNewRecords(amount) · Chronos uses: yes (DeleteAllRecords) · M7 check: forbidden (D2) for DeleteAllRecords; not run (C6) for ClearNewRecords

Section 24. The document contradicts itself on the data length: "Command (13 Byte)" and "Data format (3 Byte)", but
LEN `0x00 0x04` and a byte table of 1 + 2-4.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R4E.1 | Data length | 3 (heading) or 4 (table, LEN example) | 4 | doc (4) | forbidden (D2); not run (C6) | matches (the table and LEN) |
| R4E.2 | Clear type, 1 | 0 clear all records, 1 clear all new-record marks, 2 clear N new-record marks | `DeleteAllRecords` 0, `ClearNewRecords()` 1, `ClearNewRecords(amount)` 2 | doc | forbidden (D2); not run (C6) | matches |
| R4E.3 | New record amount, 2-4 | 3 bytes | 3 bytes (order: RF.5); 0 for types 0 and 1 | doc | forbidden (D2); not run (C6) | matches |
| R4E.4 | Answer amount, 1-3 | Records deleted / marks cleared | Read but not returned (the methods return `Task`; the public API stays, D3) | doc | forbidden (D2); not run (C6) | matches |

*Fields:* 3 (R4E.2, R4E.3, R4E.4).

### 0x4F — ResetToFactorySettings() · Chronos uses: no · M7 check: forbidden (D2)

Section 25 "Initialize System".

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R4F.1 | Request | LEN 0 | LEN 0 | doc | forbidden (D2) | matches |

*Fields:* 0.

### 0x50 — GetTimeZoneInfo(number) · Chronos uses: no · M7 check: runs

Section 26. Request 1 byte; answer 28 bytes.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R50.1 | NO., 1 | "The total time zone amount is 30" (0x51 says 32); first number not stated | Refuses anything outside 1–30 before sending | code (1–30) | zones 1 and 30 both answered | matches — the M7 confirms the code (document silent) |
| R50.2 | Monday … Sunday, 4 bytes each, 1-28 | Seven sub-time-zones in that order | `Days[Monday]` … `Days[Sunday]` | doc | same: 28 bytes, all zero | matches |
| R50.3 | Sub-time-zone format | Begin hour, begin minute, end hour, end minute | `From` / `To` | doc | same | matches |

*Fields:* 8 (R50.1; R50.2: 7, each read by R50.3).

### 0x51 — SetTimeZoneInfo(number, value) · Chronos uses: no · M7 check: not run (outside the M7 list)

Section 27. Data 29 bytes (its LEN example `0x00 0x00` is a slip).

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R51.1 | LEN | 29 | 29 | doc | not run | matches |
| R51.2 | NO., 1 | "total time zone amount is 32" (0x50 says 30); first number not stated | 1–30 | code (1–30) | not run | deviates — kept, M7 cannot run it (C12) |
| R51.3 | Monday … Sunday, 2-29 | As R50.2 / R50.3 | Same | doc | not run | matches |

*Fields:* 8 (R51.2; R51.3: 7).

### 0x54 — GetScheduledBells() · Chronos uses: no · M7 check: runs

Section 30. Answer 90 bytes = 30 time points of 3 bytes. (Its LEN example `0x00 0x3C` = 60 and its heading "Time format
(2Byte)" are slips; the table has 3 bytes.)

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R54.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R54.2 | Time points 1–30, 3 bytes each | 30 × 3 | Reads every 3 bytes of DATA, whatever its length | doc | same: 30 slots, 90 bytes | matches |
| R54.3 | Hour, minute | 1 byte each | `Time` | doc | same | matches |
| R54.4 | Weekday bits 1-6 | Bits 6-1 = Saturday … Monday (example `00111110` = Monday to Friday) | Monday = bit 1 … Saturday = bit 6 | doc | same | matches |
| R54.5 | Weekday bit 0, empty slot | Bit 0 not named (Sunday by elimination); an empty slot not described | Sunday = bit 0; `HasValue` is false for 0 and 0xFF | code | all 30 bells empty (zero) | kept — not decided on the M7 (all 30 bells empty) |

*Fields:* 30 points × 3 (R54.2; hour/minute R54.3; weekday R54.4 + R54.5).

### 0x55 — SetScheduledBell(number, value) · Chronos uses: no · M7 check: not run (outside the M7 list)

Section 31. Data 4 bytes.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R55.1 | LEN | 4 | 4 | doc | not run | matches |
| R55.2 | NO., 1 | 30 bells; first number not stated | Allows 0–30 (31 numbers; the time-zone commands allow 1–30) | code (1–30) | not run | deviates — kept, M7 cannot run it (C12) |
| R55.3 | Hour, minute, weekday, 2-4 | As R54.3–R54.5 | Same | doc | not run | matches |

*Fields:* 4 (R55.2; R55.3: 3).

### 0x5C — EnrollFingerprint(employeeID, verifyCount) · Chronos uses: yes · M7 check: runs (one enrolment with the owner's finger, D10)

Section 38. Data 7 bytes; the answer has no data (its heading "12Byte" is a slip; the layout is 11).

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R5C.1 | User ID, 1-5 | 5 bytes | 5 bytes | doc | same (id 1, enrol run 3) | matches |
| R5C.2 | Backup ID, 6 | Which finger slot | Always 1 (FP1) | doc | same: 1 | matches |
| R5C.3 | Enroll times, 7 | 0 first, 1 second ("verify double times") | 0 on the first capture, 1 on every later one (default 2 captures; `verifyCount` 3 sends 0, 1, 1) | doc | same: 0, then 1; both answered ACK_SUCCESS | matches |
| R5C.4 | After the captures | — | Reads the new template with 0x44 backup 1 (R44.2) and returns it | doc | same: 338 bytes, not the made-up template | matches |
| R5C.5 | Waiting for the finger | The device waits for the finger and answers ACK_TIME_OUT on its own timeout (length not stated) | The SDK gives up after 10 s and closes the connection (the 10 s timeout stays, C4) | — | both captures answered within 4.3 s together; the M7's own timeout was not seen | kept — not decided on the M7 (the owner pressed in time; the M7's own timeout and an answer after 10 s were not seen) |
| R5C.6 | Error answers | ACK_FAIL, ACK_TIME_OUT; on the second capture also ACK_FULL, user occupied, finger occupied | Throws `AnvizException`; the connection stays (RF.10) | doc | ACK_FAIL at the first capture while every finger was already held by other users (enrol runs 1-2); the call threw `AnvizException` Fail and the connection stayed | fixed (S3, `1e3d399`) |

*Fields:* 3 (R5C.1–R5C.3).

### 0x5E — UnlockDoor() · Chronos uses: no · M7 check: forbidden (D2)

Section 40.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R5E.1 | Request | LEN 0 | LEN 0 | doc | forbidden (D2) | matches |

*Fields:* 0.

### 0x72 — GetEmployeesData() · Chronos uses: yes (with EmployeesDataGet and its Cancel) · M7 check: runs

Section 44. Request 2 bytes; answer N + N × 40 (Unicode, ≤ 8) or N + N × 30 (ANSI, ≤ 12). First reads 0x3C for the
user count, then pages. Raises `EmployeesDataGet` per user; `Cancel` stops after that page.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R72.1 | Parameter, 1 | 0 next, 1 start, 0x10 resend | 1 first, 0 after; never 0x10 | doc | same | matches |
| R72.2 | Amount, 2 | ≤ 8 (Unicode), ≤ 12 (ANSI) | Lesser of 8 and the users still to read | doc | same: 8 | matches |
| R72.3 | Valid records N, answer byte 1 | N | Record length = (LEN − 1) / N, so 40 or 30 is taken from the answer | doc | same: 40-byte records | matches |
| R72.4 | User ID, 1-5 | 5 bytes | `Id` (order: RF.5) | doc | same | matches |
| R72.5 | Password, 6-8 | Byte 6 >> 4 = length; low 20 bits of the password in 6-8; all 0xFF = no password | `Password` = the low 20 bits; null when all 0xFF; length not kept | doc | same: 4321 for the test user | matches |
| R72.6 | Password high 8 bits, byte 38 (ANSI 28) | "high 8 bits saved in Byte28" | Not added to `Password` (kept raw in `PWDH8`): a password above 1,048,575 reads wrong. Pinned: `GetEmployeesData_PasswordAbove20Bits_DropsHigh8Bits_Today`. | doc | 0xFF on every user, the M7's own included, with a password of 20 bits or less; the test user's 4321 with 0xFF reads back 4321 | kept — M7 deviation |
| R72.7 | Card ID, 9-12 | 4 bytes; all 0xFF = no card | `Card`; null when all 0xFF | doc | same: 12345678 for the test user | matches |
| R72.8 | Name, 13-32 (ANSI 13-22) | 20 (10) bytes, encoding not stated | UTF-16 high byte first (ANSI: ASCII), trailing zeros trimmed | code | UTF-16 high byte first: "CHR27 TEST" round trip | matches — the M7 confirms the code (document silent) |
| R72.9 | Department 33, group 34, attendance mode 35 | 1 byte each | `Department`, `Group`, `Mode` | doc | the M7 does not store the department (sent 1, read 0); group 1, mode 6 same | matches |
| R72.10 | FP enroll state, 36-37 | Bit 0 = FP1 enrolled, bit 1 = FP2 | `EnrolledFingerprints`: bit 0 RightThumb, bit 1 RightIndex (bits 2-9 read as further fingers; not in the document) | doc | same | matches |
| R72.11 | Keep, 39 | Reserved | `Keep` | doc | the M7 does not keep it: sent 0xFF, read 0x00 | matches |
| R72.12 | Special info, 40 (ANSI 30) | Bits 7-6 authority (1 normal, 3 admin); bit 4 card length (1 = 32-bit, 0 = 24-bit) | `Message`, raw byte | doc | same: 0x40 | matches |
| R72.13 | The ANSI (30-byte) record | Section 44 A) | Read at the ANSI positions (name 13-22) when the answer's records are 30 bytes | — | not run (M5 path, D8) | M5 path — not tested (D8) |

*Fields:* 2 request + N + 11 per record (R72.4–R72.12: id, password, password high, card, name, department, group,
mode, FP state, keep, special); the ANSI record's 11 fields: R72.13.

### 0x73 — SetEmployeesDataUnicode(user), (users) · Chronos uses: yes · M7 check: runs (one test user, C13)

Section 45 B). Data N + N × 40 (N ≤ 8); answer a 2-byte flag.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R73.1 | Information number N, 1 | ≤ 8 | Up to 8 per frame; more users go in further frames | doc | same: 1 | matches |
| R73.2 | User ID, 1-5 | 5 bytes | `Id` | doc | same | matches |
| R73.3 | Password, 6-8 | Length nibble + low 20 bits; empty = 0xFF | Length = decimal digits; low 20 bits; null → `FF FF FF` | doc | same: 4321 | matches |
| R73.4 | Password high 8 bits, 38 | High 8 bits of the password; "If user data is empty, set it as 0xFF" | Writes `PWDH8`, which `UserInfo(id, name)` sets to 0xFF: read by the document, the device stores 0xFF << 20 + password. Pinned: `SetEmployeesData_SendsRecords_Today`. | doc | 0xFF with password 4321 reads back 4321 (R72.6) | kept — M7 deviation |
| R73.5 | Card ID, 9-12 | 4 bytes; empty = 0xFF | `Card`; null → `FF FF FF FF` | doc | same: 12345678 | matches |
| R73.6 | Name, 13-32 | 20 bytes, encoding not stated | UTF-16 high byte first, zero-padded (a name over 10 characters: RX.6) | code | UTF-16 high byte first: "CHR27 TEST" round trip | matches — the M7 confirms the code (document silent) |
| R73.7 | Department 33, group 34, attendance mode 35 | 1 byte each | As set (`UserInfo(id, name)` gives 1, 1, 6) | doc | the M7 does not store the department (sent 1, read 0) | matches |
| R73.8 | FP enroll state, 36-37 | "can not set, this value is 0" | 0 | doc | same | matches |
| R73.9 | Keep, 39 | Reserved; "If user data is empty, set it as 0xFF" | `Keep` (constructor: 0xFF) | doc | the M7 does not keep it: sent 0xFF, read 0x00 | matches |
| R73.10 | Special info, 40 | Bits 7-6 authority; bit 4 card length (1 = 32-bit) | `Message` (constructor: 0x40 = normal user); bit 4 never set, even for a card above 24 bits. Pinned: `SetEmployeesData_SendsRecords_Today`. | doc | 0x40, bit 4 clear (card 12345678 fits 24 bits) | kept — not decided on the M7 (card 12345678 fits 24 bits) |
| R73.11 | Answer flag, 2 bytes | Bits 0-11: user 1–12 stored (1) or not (0) | Ignored: a user the device refused was not reported. Now (Unicode only; ANSI R73.13 unchanged, D8): a 0 bit for a user of the frame throws `AnvizException(Fail)`. Test: `ChronosCallsTests.R73_11_UserTheDeviceDidNotStore_IsAnvizException_Unicode_AnsiUnchanged`. Chronos sees this: on a non-M5 device a user the device refuses now brings up the error box and stops that push, where before it was skipped silently. An answer without the 2-byte flag is still taken as stored. Tests: `R73_11_Unicode_OneUserOfThreeNotStored_IsAnvizException_AndTheConnectionStays`, `R73_11_Unicode_AnswerWithoutTheFlag_IsTakenAsStored`. | doc | flag 0x0001 for a new and for an updated user, as the document says | fixed (S6, `160bc6a`) |

*Fields:* N + 11 per record (R73.2–R73.10: id, password, password high, card, name, department, group, mode, FP
state, keep, special) + the answer flag (R73.11).

### 0x73 — SetEmployeesDataAnsi(user), (users) · Chronos uses: yes (M5 devices) · M7 check: not run (D8)

Section 45 A). Data N + N × 30.

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R73.12 | The 30-byte record | N ≤ 12; the 11 fields at the ANSI positions (name 13-22, 10 bytes) | Up to 8 per frame; the same fields at the ANSI positions; name ASCII (a name over 10 characters: RX.13); password high byte 28 and special info byte 30 as R73.4 / R73.10 | doc | not run (M5 path, D8) | M5 path — not tested (D8) |
| R73.13 | Answer flag | As R73.11 | Ignored, as R73.11 | doc | not run (M5 path, D8; the check's line labelled "R73.13" is the Unicode update, R73.11) | kept — not decided on the M7 (the ANSI record's flag: M5 path, D8) |

*Fields:* N + 11 per record (R73.12) + flag (R73.13).

### 0x7E — InquireCard(retries) · Chronos uses: no · M7 check: runs (the M7 is not a T5S: an ACK_FAIL is expected)

Section 82 ("T5S use only").

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| R7E.1 | Request | LEN 0 | LEN 0 | doc | same | matches |
| R7E.2 | Card number, 1-4 | 4 bytes; 0 when no card was punched | Read (order: RF.5); asks again every 500 ms while 0, up to `retries` times | doc | success with card 0 (the check expected ACK_FAIL; it passes on either) | matches |

*Fields:* 1 (R7E.2).

### 0xFF — PongCommand (the SDK's answer to a device ping) · Chronos uses: no (automatic) · M7 check: runs only if the M7 pings

| Id | Field | Document | Code | Fake | M7 | Verdict |
|---|---|---|---|---|---|---|
| RFF.1 | The frame | 0xFF is the answer to the C5 "Sending Email" command 0x7F; no ping in the document (RF.13) | Answer format: STX, CH, 0xFF, RET 0x00, LEN 0x00 0x00, CRC (11 bytes) | code | not seen (no ping) | kept — not decided on the M7 (no device ping in any run) |

*Fields:* none in the document. The pong is written without the command lock — RX.3.

## 3. Document commands the SDK does not have (D5)

Listed only; nothing is added in CHR27-1.

| Code | Document section |
|---|---|
| 0x10 | 68 Update firmware, photo, voice (761 platform) |
| 0x12 | 69 Directory file operation (761 platform) |
| 0x13 | 70 Download log record (761 platform; its table prints CMD 0x40, a slip) |
| 0x16 / 0x17 | 78 / 79 Get / set GPRS parameters |
| 0x18 / 0x19 | 75 / 76 Read / set optional language combination |
| 0x1A / 0x1B | 73 / 74 Read / set daylight saving |
| 0x1C / 0x1D | 71 / 72 Read / set admin card or password (T5) |
| 0x20 / 0x21 | 57 / 58 Get / set attendance-state auto switch (OA3000/OA1000) |
| 0x22 / 0x23 | 59 / 60 Download / upload staff, 84-byte record (761 platform) |
| 0x26 – 0x29 | 53 – 56 Long messages (OA3000) |
| 0x2A – 0x2D | 64 – 67 Photos (OA1000/OA3000/761) |
| 0x2F | 63 Get special state (VF30/VP30/T60+) |
| 0x3D | 48 Clear administrator flag |
| 0x3E / 0x3F | 49 / 50 Read / set enrolment timestamp |
| 0x42 / 0x43 | 12 / 13 Download / upload staff, old 27-byte record (the SDK uses 0x72 / 0x73) |
| 0x44 / 0x45 iris | 14 / 15 The iris template variant (1,280 / 1,286 bytes) |
| 0x49 | 19 Modify device type code |
| 0x4A / 0x4B | 20 / 21 Get / modify factory info code |
| 0x52 / 0x53 | 28 / 29 Get / set group |
| 0x56 – 0x59 | 32 – 35 Short messages |
| 0x5A / 0x5B | 36 / 37 Get / set attendance-state table |
| 0x5D | 39 Get device capacity |
| 0x70 / 0x71 | 42 / 43 Get / set customised attendance-state table |
| 0x74 / 0x75 | 46 / 47 Get / modify communication device ID (see R46.3) |
| 0x76 / 0x77 | 51 / 52 Random number / encrypt device type |
| 0x78 | 77 Receive feature value or card ID |
| 0x7A / 0x7B | 80 / 81 Get / modify extended information (vendor name, address, tax code) |
| 0x7F | 83 Sending email (C5) — the SDK reads a device-sent 0x7F as a ping instead (RF.13) |

0x5F (section 41, the real-time record) is device-sent; the SDK handles its 0xDF frame (RF.14).

## 4. Transport and code faults found by reading

The frame faults are in section 1: **RF.10, RF.11, RF.12, RF.15, RF.17** (all fixed in S3). The answer flag (R73.11 /
R73.13) moved from S3 to the M7 run (S6), which fixed R73.11. The rows below are the rest.

| Id | Where | What happens | Verdict |
|---|---|---|---|
| RX.1 | `AnvizStream.ReceiverThreadFunc` | The reader is an `async void` method started on a `Thread`. The thread ends at its first `await`; the rest runs on the thread pool, where any exception nothing catches ends the process: a failed pong write, an exception from a caller's `ReceivedPacket` / `DevicePing` / `ReceivedRecord` / `DeviceError` handler, or a short 0xDF record. Now: the reader is a `Task`; whatever fails in it is raised as `DeviceError`, the connection closes and the waiting call gets `DeviceConnectionLostException`. Test: `FrameTests.RX1_ExceptionInTheReader_IsDeviceError_AndEndsTheConnection`. | fixed (S3, `5933f87`) |
| RX.2 | `AnvizStream.SendCommand` / `Dispose` | The answer's `TaskCompletionSource` has no `RunContinuationsAsynchronously`, so the caller can resume on the reader's thread; if it then calls `Dispose`, `ReceiverThread.Join()` waits for itself for ever. Seen in S1 (a test hung in `Dispose`). Risk for any caller without a synchronisation context. Now: the answer's `TaskCompletionSource` runs continuations asynchronously, and `Dispose` closes the socket without waiting for the reader. Test: `FrameTests.RX2_DisposeWhereTheConnectCompletes_NeverHangs` (server mode: pings already waiting keep the reader on its first thread, as in S1). | fixed (S3, `fbd4e64`) |
| RX.3 | `AnvizStream.ReceiverThreadFunc` (pong) | The pong is written from the reader without the command lock, so it can interleave its bytes with a command being written at the same moment. Now: commands and the pong are written under one write lock, held for the write only. Test (a guard: the interleaving cannot be made to happen on the fake): `FrameTests.RX3_PingsWhileCommandsAreWritten_EveryFrameArrivesWhole`. | fixed (S3, `00433a5`) |
| RX.4 | `AnvizManager.GetDevice` | When the first command at connect (0x04 or 0x48) fails, the new `AnvizDevice` is not disposed: its socket and reader stay open. Now: the device is disposed, then the exception goes on to the caller. Test: `FrameTests.RX4_FirstCommandAtConnectFails_TheConnectionIsClosed`. | fixed (S3, `67711be`) |
| RX.5 | `AnvizDevice.GetEmployeesData` | `catch (Exception) { }` around each user: a record that fails to read, or an exception from the caller's `EmployeesDataGet` handler, silently leaves that user out. Now: the other users are still read (Chronos's behaviour); each skipped user is logged at Error level with its position and its exception, and the download goes on. It is not raised as `DeviceError`: Chronos takes that as a lost connection and disposes the device. Test: `FrameTests.RX5_UserTheReadFailsOn_IsSkipped_AndLogged_NotDeviceError`. | fixed (S3, `35d8a08`) |
| RX.6 | `UserInfo.ToArray`, Unicode record (0x73 B, the M7) | The name is copied in whole. The name field is 20 bytes (10 Unicode characters); a longer name runs into department, group, mode and the FP-state bytes (the first three are written again afterwards, the FP-state bytes keep name bytes); over 14 characters it throws `ArgumentException`. The record layout does not change (D8): S3 only stops a name longer than its field from writing past it, with the same guard as RX.13. Now: the name is cut to its 20 bytes. Test: `FrameTests.RX6_RX13_NameLongerThanItsField_IsCut_AndLeavesTheOtherFields(40)`. | fixed (S3, `35e915d`) |
| RX.13 | `UserInfo.ToArray`, ANSI record (0x73 A, the M5) | The same as RX.6 in the 10-byte name field (10 ANSI characters): a longer name runs into department, group, mode and the FP-state bytes; over 18 characters it throws. The layout does not change (D8): the same S3 guard as RX.6. Now: the name is cut to its 10 bytes. Test: `FrameTests.RX6_RX13_NameLongerThanItsField_IsCut_AndLeavesTheOtherFields(30)`. | fixed (S3, `35e915d`) |
| RX.14 | `AnvizStream.SendCommand` (command write) / `AnvizStream.ReceiveAsync` (pong write) | Neither write had a time limit (`WriteTimeout` covers synchronous writes only; the SDK writes async). A command write stalled in the socket, or behind a stalled pong, hung the call for ever, and the stalled pong held the reader with it; a stalled pong with no command running held the reader until the next command. A command write that failed with `IOException` (the device reset the connection before the reader saw it) reached the caller as `IOException`. Now: the command write and its answer share the 10-second limit; at the limit the connection closes and the caller gets `DeviceConnectionLostException`. A pong write has its own 10-second limit; at the limit it is raised as `DeviceError` and the connection closes (it is gone). A failed command write closes the connection and reaches the caller as `DeviceConnectionLostException`, the `IOException` inside. Tests: `FrameTests.StalledWrite_TheCallEndsAtTheTimeout_AndTheConnectionCloses`, `FrameTests.CloseWhileTheCallWaitsToWrite_TheCallerGetsDeviceConnectionLost`, `FrameTests.StalledPongWithNoCall_EndsAtTheTimeout_AsDeviceError`, `FrameTests.WriteFailsOnAResetConnection_TheCallerGetsDeviceConnectionLost`. | fixed (S3, `9596ad1` command write, `8330a99` pong write and failed write) |
| RX.7 | `AnvizStream.SendCommand` | Each command starts a 10-second `Task.Delay` that is never cancelled: one live timer per command until it runs out. The pong write's limit (RX.14) did the same when the write did not end at once. Now: both limits are cancelled when the call or the pong write ends. Test: `AnswerTimerTests.RX7_AnsweredCommands_LeaveNoLiveTimer` (500 commands left 500 live timers before). | fixed (S4, `84be951`) |
| RX.8 | `SetEmployeesDataUnicode(List)` / `SetEmployeesDataAnsi(List)` | Empty the caller's list (`users.RemoveRange`). Pinned by `SetEmployeesData_SendsRecords_Today`. Changing it changes the public behaviour. | noted — no change (D3) |
| RX.9 | `Response.FromStream` | A new 16,000-byte buffer for every frame; a LEN above about 15,990 throws `IndexOutOfRangeException`. Now: the header is read into 9 bytes, then one buffer sized from LEN holds the frame; any LEN up to 65,535 is read whole. Test: `FrameTests.RX9_AnswerOfAnyLen_IsReadWhole(16000)` and `(65535)` (both lost the connection before). | fixed (S4, `36fd8c3`) |
| RX.10 | `Bytes.Split` | Every field is cut with LINQ `Skip().Take().ToArray()`: a walk from the start of the buffer and a new array per field. Now: one `Array.Copy` of the range; a range past the end still gives the bytes that are there, as before. Covered by every frame test. Kept as code hygiene: no measurable gain on the fake (section 5); it stays under C3 because it removes the row's fault (the walk from the start and LINQ's extra allocations per field). | fixed (S4, `f6b2573`) |
| RX.11 | `AnvizManager.Connect` / `Accept` | `NoDelay` is not set on the socket, so small frames can wait for Nagle's algorithm. Now: `AnvizStream` sets `NoDelay` on every socket (Connect and Accept both pass through it). Kept as code hygiene: no measurable gain on the fake, where loopback has no real network delay (section 5); it stays under C3 because it removes the row's fault (the Nagle delay). Its effect can show only on a real network: it is timed on the M7 in S6 (C14). | fixed (S4, `774aab7`) |
| RX.12 | `DownloadRecords`, `GetEmployeesData`, `UserInfo` | `DownloadRecords` builds every `Record` twice (once for the event, once for the list); `GetEmployeesData` writes three debug lines per page and `UserInfo` one per user, the text built even when debug logging is off. Now: each record is read once; the per-page and per-user debug text is built only when debug logging is on. Gain: small, within run-to-run noise on the fake (21-run medians: get 1,000 users 19.5 → 17.1 ms, download 5,000 records 32.5 → 29.9 ms; section 5). **Deliberate deviation from D3** ("only the inside changes"): `RecordDownloaded`'s `Record` is now the same object as the one in the returned list, as `EmployeesDataGet`'s `Employee` already was, so a handler that edits `e.Record` edits the list. Chronos's three handlers read only the counts (`ChronosApp/Shared/DeviceControlService.cs:215`, `ChronosApp/Controls/DeviceControl_Anviz.cs:746-750`, `Chronos.Web/Screens/DeviceControl/DeviceControlScreen.Anviz.cs:176-180`). | fixed (S4, `f91899c`) |

## 5. Timings

### Before (S1)

From slice S1 (fix round 1), against the fake device on 127.0.0.1, milliseconds, verbatim:

`dotnet test --project Anviz.SDK.Tests\Anviz.SDK.Tests.csproj --filter-class "*TimingTests" --output Detailed`:

| Case | No answer delay | Task.Delay(5) before each answer |
|---|---:|---:|
| One round trip (get device type code) | 0.2 | 16.5 |
| Download 5,000 records | 31.1 | 3157.2 |
| Get 1,000 users | 16.6 | 1983.6 |
| Get 200 templates | 19.3 | 3137.1 |
| Set 200 templates | 23.1 | 3082.0 |
| Set 200 users (Unicode) | 2.9 | 384.2 |

### S4: before and after (this machine, 2026-10-25)

The S3 build (`11ac8ee`, before) and the S4 build (after) were timed side by side in two worktrees, taking turns, three
`TimingTests` runs each; each cell is the lowest of the three medians. The machine was shared with other work, so one
run in three often came out 30-80% slower on a path no change touched; taking turns and the lowest value keeps that out
of the table. Same command as above.

| Case | Before: no delay | After: no delay | Before: Task.Delay(5) | After: Task.Delay(5) |
|---|---:|---:|---:|---:|
| One round trip (get device type code) | 0.2 | 0.2 | 16.4 | 15.7 |
| Download 5,000 records | 30.9 | 34.0 | 3124.2 | 2900.2 |
| Get 1,000 users | 16.1 | 15.1 | 1864.6 | 1933.3 |
| Get 200 templates | 22.3 | 20.5 | 3029.1 | 3007.9 |
| Set 200 templates | 24.0 | 20.7 | 3036.6 | 2934.6 |
| Set 200 users (Unicode) | 2.8 | 2.9 | 384.4 | 383.1 |

**What it shows: no measurable gain (C14).** The largest difference, RX.12 (`f91899c`: 21-run medians get 1,000 users
19.5 → 17.1 ms, download 5,000 records 32.5 → 29.9 ms), is small, within run-to-run noise on the fake. The differences
above sit inside the spread between runs of the same build (download 5,000 records: before 30.9-34.3 ms, after 34.0-35.3 ms; set 200 templates: before 24.0-39.9 ms).
Over 21 runs of each case, side by side, the download median was 29.8-30.0 ms after against 30.5-34.5 ms before, the
reverse of the table, so neither direction is real. The "no delay" column is about one loopback round trip (0.1-0.2 ms)
per page or command plus the fake's own work; the delayed column is Windows timer steps (about 15.6 ms per answer), so
neither moves with a change inside the SDK. On the M7, where a round trip is milliseconds, it would show less still.

Each change, kept for the fault its row names (S4 rule: kept if the table improves **or** it removes its row's fault):

| Change | Row | Commit | Gain |
|---|---|---|---|
| Socket `NoDelay` | RX.11 | `774aab7` | none measurable on the fake (each frame is one write, and the protocol waits for each answer, so Nagle rarely held one); kept as code hygiene, it removes the Nagle delay; its effect can show only on a real network, timed on the M7 in S6 (C14) |
| Frame buffer sized from LEN (no 16,000-byte buffer per frame; any LEN up to 65,535 read) | RX.9 | `36fd8c3` | none measurable; the fault is gone (a LEN of 16,000 lost the connection) |
| Answer timer cancelled when the call or pong write ends | RX.7 | `84be951` | none measurable; the leak is gone (500 commands left 500 live timers) |
| `Bytes.Split` by `Array.Copy` instead of LINQ | RX.10 | `f6b2573` | none measurable; kept as code hygiene, it removes LINQ's walk and extra allocations per field |
| Each record read once; debug text built only when debug logging is on | RX.12 | `f91899c` | small, within run-to-run noise on the fake: 21-run medians get 1,000 users 19.5 → 17.1 ms, download 5,000 records 32.5 → 29.9 ms; not visible in the table |

Two candidates were already done by S3 and were not changed again: the waiting caller resumes off the reader (the
answer's `TaskCompletionSource` runs continuations asynchronously, RX.2), and the reader is an async loop on a `Task`
(RX.1). A slow `RecordDownloaded` / `EmployeesDataGet` handler therefore runs on the caller's side, not on the reader,
which goes on answering pings.

The M7 timings before and after are in the appendix *M7 runs*: no measurable gain on the M7 either.

## Appendix: M7 runs (S6, 2026-10-25)

`Anviz.SDK.M7Check` against the M7 (type "M7A+-N", firmware "06.48.M1", serial number "1590100019260166", IP
10.31.241.240, port 5010) through its local relay. Each run adds the test user (C13), checks it and removes it.

| Run | SDK | Flags | Passed | Connection lost at |
|---|---|---|---:|---|
| Before | `development` | `--time` | 21 of 26 | 0x40 (0 records), 0x24, 0x73 add, R44.3 (backup codes 3-10), the final 0x3C |
| After | CHR27-1 before the S6 fixes | `--time` | 25 of 26 | 0x24 only: sent to CH 0x31353930 after `GetDeviceID` took the 0x46 answer as its CH (R46.3) |
| After the fixes | CHR27-1 with `23cf990` (R46.3) and `160bc6a` (R73.11) | none | 25 of 25 | none (exit 0); 0x24 answers ASCII "1590100019260166" |
| Enrol 1 | CHR27-1 | `--enrol` | 25 of 26 | none; 0x5C answered ACK_FAIL at the first capture (the test user still held the made-up FP1) |
| Enrol 2 | CHR27-1 with `f6efd04` (FP1 removed first, as Chronos does) | `--enrol` | 24 of 26 | none; 0x5C ACK_FAIL again: the owner's fingers were enrolled on every user; the owner clocked during the run, so the new-record check failed too |
| Enrol 3 | the same | `--enrol` | 26 of 26 | none (exit 0); enrolled in 4.3 s, after every user was removed at the owner's request |

Why enrol 1 and 2 failed: the owner's fingers were enrolled on every user on the M7, so the M7 would not take the same finger for a new user (the owner's explanation; removing FP1 first, run 2, did not help). With no users left, run 3 enrolled at once. The M7 was left with no users; a local backup of the 13 users and 22 templates is kept outside the repo, and putting them back is the owner's call.

**C14 timings on the M7** (3 runs each, median ms, through the relay; the relay's own share is 0.1-0.6 ms):

| Case | Before (`development`) | After (CHR27-1) |
|---|---:|---:|
| One round trip (get device type code) | 54.5 | 55.0 |
| Download all records (0 on the M7: one empty answer, so this row says nothing about record speed; see section 5 for the fake) | 241.8 | 241.5 |
| Get all users | 598.9 | 597.5 |
| Get the test user's FP1 template | 55.9 | 56.4 |

What it shows: no measurable gain (C14). The device and the network set the time; the SDK's own share is too small
to see.

## Appendix: Chronos on the new SDK (S7, 2026-10-25)

Throwaway worktrees of Chronos `main` (8fafaf27) and `CHR2027` (06394e5f) whose `..\..\Anviz` reference pointed at this
branch (at `7c725a0`), built with Visual Studio's MSBuild (Debug), with no Chronos code changed (C11). One local-only
change in those copies: their DevExpress references were moved from 26.1.5 to 26.1.6, because this machine's GAC has
held only 26.1.6 since 2026-10-24 and neither branch builds here otherwise; it has nothing to do with the SDK.

| Chronos | Build | ChronosApp.Tests | Chronos.Web.Tests (no database) |
|---|---|---:|---:|
| `main` | 0 errors | 53 of 53 | — (not on main) |
| `CHR2027` | 0 errors | 304 of 304 | 896 passed, 0 failed, 679 skipped (database tests) |

Behaviour Chronos sees from the SDK changes (D3 keeps every public shape): a device error answer no longer drops the
connection (S3); on a non-M5 device a user the device refuses to store now shows the error box for that user (R73.11);
a name over 10 characters is cut by the SDK as Chronos already cuts it (RX.6, RX.13).
