# Anviz SDK review and refactor — detail plan

> Run with `level54-build`. The project's `CLAUDE.md` rules win over this plan; the spec wins over
> the plan. Steps use checkboxes.

**Goal:** Review every Anviz.SDK command and answer against the protocol document and the M7, fix the faults, make it
faster where a measurement shows it, with Chronos unchanged.
**Spec:** [sdk-review-refactor](../specs/2026-10-25-sdk-review-refactor.md), approved 2026-10-25.   **Ticket:** CHR27-1
**Architecture:** All work is in the Anviz repo on branch `CHR27-1` (worktree `D:\Worktrees\anviz-chr27-1`). A new
test project drives the public `AnvizManager` / `AnvizDevice` against a fake device on 127.0.0.1 that speaks the
protocol and checks every frame. A new console program runs the same public calls against the real M7. The review
document is the record of every deviation and every timing.
**Tech stack:** Anviz.SDK (netstandard2.0, C# 8, log4net). Tests: net10.0, xunit.v3 on Microsoft.Testing.Platform
(new package, settled by D6). M7 check: net10.0 console.

## Global constraints

- The public API does not change: every public type, method, property, event and name keeps its shape (D3). A test that
  needs a new public member is wrong; drive the existing ones.
- The protocol document is `Docs/Communication Protocol.pdf`. `Docs/communication_protocol.md` is a markdown copy that
  lost some command codes: where they differ, the PDF wins. `Docs/anviz_sdk_review_2026.md` is ignored (D7).
- The M7 decides a deviation (D4): M7 follows the document → the code is fixed; M7 follows the code → the code stays
  with a comment `// M7 deviation: <what the document says>` and the review lists it.
- A command's data layout changes only where the document and the M7 agree; the M5 path (the ANSI staff record) stays
  (D8). A command the M7 check cannot run keeps its layout; only frame faults are fixed for it (C12).
- On the M7: read commands; one test user (name, password, card, one made-up template) added, read back and removed
  (C13); setting the clock to the PC's time (D9); one fingerprint enrolment with the owner's finger (D10). Never clear
  records, the "new records" request (C6), factory reset, reboot, network settings, device ID or serial number, door
  unlock (D2).
- One command at a time per connection, a 10-second answer timeout, the same events (D3, C4).
- Commit subjects start `CHR27-1:`; pathspec commits only.

## Slices

| Slice | Title | Blocked by | Status | Demo | Door |
|---|---|---|---|---|---|
| S1 | Prefactor: SDK test project, fake device, today's behaviour pinned, first timings | — | Open | `dotnet test` all pass; the first timing table | New package xunit.v3: settled by D6 — not a stop |
| S2 | The review document and fake-device tests for the commands Chronos does not use | S1 | Open | The review document; `dotnet test` all pass | — |
| S3 | Frame faults fixed | S2 | Open | The new tests fail on S2's code and pass after | — |
| S4 | Speed: the transport inside, measured | S3 | Open | Before/after timing table in the review | Public API: settled by D3 — not a stop |
| S5 | M7 check program, proved on the fake device | S2 | Open | The program's output against the fake device | Writes to a real device: settled by D2, D9, D10 — not a stop |
| S6 | The M7 run: verdicts, M7 fixes, M7 timings, enrolment | S4, S5, the M7 on the network | Open | The program's M7 output; the review with every row filled | — |
| S7 | Chronos proof, changelogs, merge | S6 | Open | Chronos main and CHR2027 build and test output | Merge into Anviz `development` and push: unsettled — the build stops before Task 2 |

## Calls in this plan (overrule any with "P1: no, do X")

- P1. Tests: new project `Anviz.SDK.Tests` (net10.0, xunit.v3 4.*, `OutputType=Exe`), plus `global.json` at the repo root
  selecting Microsoft.Testing.Platform, as Chronos does. The fake device lives in the test project.
- P2. M7 check: new console project `Anviz.SDK.M7Check` (net10.0), added to `Anviz.sln`. `Sample` stays untouched.
- P3. Timing tests run with the normal tests (they print, never fail on a number). The fake device can add a per-answer
  delay to stand in for the network.
- P4. The M7 "before" timings: build `Anviz.SDK.M7Check` once against `development`'s SDK in a throwaway worktree
  `D:\Worktrees\anviz-before`, removed after S6.
- P5. The Anviz `CHANGELOG.md` gets one `## v2.0.17 (unreleased)` section, a line per slice. The Chronos `CHANGELOG.md`
  on CHR2027 gets one `[CHR2027]` entry per slice; slice reports go on CHR27-1.
- P6. Review document: `Docs/anviz_sdk_review_CHR27-1.md`. One table per command (code, method, Chronos uses it y/n),
  rows per field: document / code / fake / M7 / verdict. Row ids `R<command hex>.<n>` (e.g. `R40.2`) so tests and
  commits can name them.

---

### Slice S1 — Prefactor: SDK test project, fake device, today's behaviour pinned, first timings

**Delivers:** the SDK has automated tests that run with no device; today's behaviour of the 15 calls Chronos makes is
pinned; first timing numbers exist. No SDK code changes.
**Blocked by:** none
**Door:** new package xunit.v3: settled by D6 — not a stop
**Seams tested:** the public SDK against a fake device.
**Review focus:** the fake answering from the document, not from the SDK's own code (a fake copied from the SDK proves
nothing); answers split across reads not yet exercised (S3 adds them).

#### Task 1: Test project and fake device

**Files:**
- Create: `global.json` — `{"test":{"runner":"Microsoft.Testing.Platform"}}`
- Create: `Anviz.SDK.Tests/Anviz.SDK.Tests.csproj` — net10.0, Exe, xunit.v3 4.*, references `Anviz.SDK`
- Create: `Anviz.SDK.Tests/Fake/FakeAnvizDevice.cs` — the fake device
- Create: `Anviz.SDK.Tests/Fake/Frame.cs` — build and parse a frame per the document (CRC from its own copy of the
  document's CRC table, not the SDK's)
- Modify: `Anviz.sln` — add the test project
- Test: `Anviz.SDK.Tests/Fake/FakeAnvizDeviceTests.cs`

**Interfaces:**
- Consumes: public `AnvizManager.Connect(string host, int port)`, `AnvizDevice`.
- Produces:
  - `sealed class FakeAnvizDevice : IAsyncDisposable` — `static Task<FakeAnvizDevice> StartAsync()`, `int Port`,
    `ulong DeviceId` (default 1), `string DeviceTypeCode` (default `"M7A+-N"`), `TimeSpan AnswerDelay`,
    `IReadOnlyList<Frame> Received`, `List<FakeUser> Users`, `List<FakeRecord> Records`,
    `void AnswerNext(byte command, Func<Frame, byte[]> rawAnswer)` (one-shot override: the raw bytes to write, for
    error answers and malformed frames), `void SplitNextAnswer(params int[] cuts)` (write the next answer in pieces at
    these byte offsets, with a short pause between).
  - `sealed record Frame(ulong DeviceId, byte Code, byte? Ret, byte[] Data)` with `static Frame Parse(byte[] bytes)`
    (throws on a bad header, length or CRC) and `byte[] ToBytes()`.
  - The fake fails the test (records a `FrameError`) when a received frame breaks the document: header not 0xA5, length
    not matching, CRC wrong.

- [ ] **Step 1: Write the failing test** `FakeAnvizDeviceTests.Connect_ReadsDeviceType_AndFrameIsValid`

```csharp
await using var fake = await FakeAnvizDevice.StartAsync();
using var device = await new AnvizManager().Connect("127.0.0.1", fake.Port);
Assert.Equal("M7A+-N", device.DeviceType);
Assert.Equal(BiometricType.Finger, device.DeviceBiometricType);
var f = Assert.Single(fake.Received);
Assert.Equal(0x48, f.Code);
Assert.Empty(f.Data);
```

- [ ] **Step 2: Run it** — Run: `dotnet test --project Anviz.SDK.Tests/Anviz.SDK.Tests.csproj`
  Expected: FAIL — `FakeAnvizDevice` does not exist.
- [ ] **Step 3: Implement** `FakeAnvizDevice`, `Frame` and the project, answering 0x48 from the document.
- [ ] **Step 4: Run it** — Expected: all pass, 1 new; `dotnet build Anviz.sln` clean.
- [ ] **Step 5: Commit** `CHR27-1: S1 SDK test project and fake device`.

#### Task 2: Today's behaviour of the calls Chronos makes

**Files:**
- Modify: `Anviz.SDK.Tests/Fake/FakeAnvizDevice.cs` — answers for 0x3C, 0x40, 0x4E, 0x39, 0x72, 0x73, 0x4C, 0x4D,
  0x44, 0x45, 0x5C, from the document's layouts
- Test: `Anviz.SDK.Tests/ChronosCallsTests.cs`

**Interfaces:**
- Consumes: `GetDownloadInformation`, `DownloadRecords(bool)`, `DeleteAllRecords`, `SetDateTime`, `GetEmployeesData`,
  `SetEmployeesDataUnicode(List<UserInfo>)`, `SetEmployeesDataAnsi(List<UserInfo>)`, `DeleteEmployeesData(ulong)`,
  `GetFingerprintTemplate(ulong, Finger)`, `SetFingerprintTemplate(ulong, Finger, byte[])`, `EnrollFingerprint(ulong, int)`,
  `GetDeviceTypeCode`, `Dispose`.
- Produces: nothing new.

- [ ] **Step 1: Write the tests** (one per call; each asserts the frame the fake received — code and data bytes as the
  document lays them out — and the value the call returns). The values to use:
  - `DownloadRecords_All_Reads60RecordsIn3Pages`: 60 records on the fake → 60 returned, 3 frames of 0x40, the first
    with data `[0x01, 25]`, the next two with `[0x00, 25]` and `[0x00, 10]` (as the code sends today); record
    `UserCode`, `DateTime`, `RecordType` equal what the fake holds.
  - `GetEmployeesData_Reads20UsersIn3Pages`: 20 users (Unicode names, one with password 1234, one with card 99887766)
    → 20 returned, names, password and card equal.
  - `SetEmployeesDataUnicode_Sends40ByteRecords` and `SetEmployeesDataAnsi_Sends30ByteRecords`: 10 users → 2 frames,
    8 + 2 records, record length 40 / 30.
  - `DeleteEmployeesData_SendsUserIdAndFF`, `DeleteAllRecords_SendsMode0`, `SetDateTime_Sends6Bytes`
    (`2026-10-25 14:30:05` → `[26, 10, 25, 14, 30, 5]`).
  - `GetFingerprintTemplate_Returns338Bytes`, `SetFingerprintTemplate_Sends344Bytes`,
    `EnrollFingerprint_Sends2Captures_ThenReadsTemplate`.
  Where today's code differs from the document, the test pins today's code and its name ends `_Today` (S2 lists it).
- [ ] **Step 2: Run them** — Expected: all pass, 12 new (they pin today's code; a red test here means the fake is wrong).
- [ ] **Step 3: Commit** `CHR27-1: S1 the calls Chronos makes, pinned`.

#### Task 3: First timings

**Files:**
- Test: `Anviz.SDK.Tests/TimingTests.cs` — prints a table through `TestContext.Current.TestOutputHelper`

- [ ] **Step 1: Write** `TimingTests.Baseline` timing, 3 runs each, median: download 5,000 records; get 1,000 users;
  get 200 templates; set 200 users (Unicode). Once with `AnswerDelay = 0`, once with `AnswerDelay = 5 ms`.
- [ ] **Step 2: Run it** — `dotnet test --project Anviz.SDK.Tests/Anviz.SDK.Tests.csproj --filter-class "*TimingTests" --output Detailed`
  Expected: all pass; the table is printed. Copy it into the S2 review document's timing section as "before".
- [ ] **Step 3: Commit** `CHR27-1: S1 first timings`.

- [ ] **Demo:** `dotnet test` output, and the timing table.

Slice end: `level54-build` runs the review, fixes, verification, slice report and Definition of Done.

---

### Slice S2 — The review document and fake-device tests for the commands Chronos does not use

**Delivers:** the owner can read, for every SDK command and answer field, what the document says and what the code does,
with a verdict where the desk review can give one; every SDK command has a fake-device test.
**Blocked by:** S1
**Door:** none
**Seams tested:** the public SDK against a fake device.
**Review focus:** a document field the review missed (count the document's fields per command against the review's
rows); an answer the code reads with the wrong byte order or offset; a command the SDK sends with a code the document
gives to another command.

#### Task 1: The desk review

**Files:**
- Create: `Docs/anviz_sdk_review_CHR27-1.md` — per P6; sections: 1 frame (command, answer, device-sent frames such as
  ping 0x7F and real-time record 0xDF, CRC, return codes, the "RET not success → LEN 0" rule), 2 one table per SDK command
  (all 37 under `Anviz.SDK/Commands`), 3 document commands the SDK does not have (D5, a list), 4 transport and code
  faults found by reading (each with a row id and the slice that fixes it: S3 or S4), 5 timings (S1's "before").
  Verdicts now: `matches`, `deviates — M7 decides (S6)`, `deviates — kept, M7 cannot run it (C12)`, `M5 path — not
  tested (D8)`, `fault — fixed in S3/S4`.

- [ ] **Step 1:** Read the PDF section by section (the Read tool takes the PDF 20 pages at a time) and fill the
  document. Read every SDK command and answer class against it.
- [ ] **Step 2: Commit** `CHR27-1: S2 review document`.

#### Task 2: Fake-device tests for the rest

**Files:**
- Modify: `Anviz.SDK.Tests/Fake/FakeAnvizDevice.cs` — answers for every other SDK command, from the document
- Test: `Anviz.SDK.Tests/OtherCommandsTests.cs`

- [ ] **Step 1: Write** one test per public `AnvizDevice` method not in `ChronosCallsTests` (basic and advanced
  settings get/set, date/time get, device ID, serial number, TCP parameters get/set, records upload, clear new records,
  reboot, factory reset, unlock, face template get/set, time zones get/set, bells get/set, inquire card, connection
  password, delete by verification method). Each asserts the frame sent and the value returned; a deviation the review
  lists pins today's code with `_Today` and names its row id in a comment.
- [ ] **Step 2: Run them** — Expected: all pass, about 25 new.
- [ ] **Step 3: Commit** `CHR27-1: S2 fake-device tests for every command`.

- [ ] **Demo:** the review document, and `dotnet test` output.

Slice end: `level54-build` runs the review, fixes, verification, slice report and Definition of Done.

---

### Slice S3 — Frame faults fixed

**Delivers:** a device error answer comes back as `AnvizException` with its return code and the connection keeps
working (story 4); an answer that arrives in pieces is read whole; every other frame fault in the review's section 4
marked S3 is fixed.
**Blocked by:** S2
**Door:** none
**Seams tested:** the public SDK against a fake device.
**Review focus:** a device-sent frame (ping, real-time record) arriving while a command waits; a bad CRC still ending the
connection (C5); `Dispose` while a command waits.

#### Task 1: Error answers and split answers

**Files:**
- Modify: `Anviz.SDK/Responses/Response.cs`, `Anviz.SDK/AnvizStream.cs`
- Test: `Anviz.SDK.Tests/FrameTests.cs`

**Interfaces:**
- Consumes: `AnvizException.ReturnCode` (`AnvizReturnCode`), `DeviceConnectionLostException`.
- Produces: nothing public.

- [ ] **Step 1: Write the failing tests**

```csharp
// FrameTests.ErrorAnswer_IsAnvizException_AndConnectionStays
fake.AnswerNext(0x4C, f => new Frame(fake.DeviceId, 0xCC, 0x06, []).ToBytes()); // RET NO_USER, LEN 0
var ex = await Assert.ThrowsAsync<AnvizException>(() => device.DeleteEmployeesData(999));
Assert.Equal(AnvizReturnCode.NoUser, ex.ReturnCode);
var stats = await device.GetDownloadInformation();          // the next command still works
Assert.Equal((ulong)fake.Users.Count, stats.UserAmount);

// FrameTests.SplitHeader_IsReadWhole
fake.SplitNextAnswer(3);                                     // header in two pieces
Assert.NotNull(await device.GetDownloadInformation());

// FrameTests.SplitEverywhere_IsReadWhole
fake.SplitNextAnswer(1, 6, 7, 9, 12);
Assert.Equal(60, (await device.DownloadRecords()).Count);

// FrameTests.BadCrc_EndsConnection
fake.AnswerNext(0x3C, f => CorruptCrc(...));
await Assert.ThrowsAsync<DeviceConnectionLostException>(() => device.GetDownloadInformation());
```

- [ ] **Step 2: Run them** — Expected: FAIL — the error answer leaves its CRC in the stream; a split header is taken
  as a lost connection.
- [ ] **Step 3: Implement** in `Response.FromStream`: read the header with the read-until-full loop; on a RET other
  than success read LEN and CRC to the end of the frame, check the CRC, then raise `AnvizException`; read a device-sent
  frame (code below 0x80) with its length at the right offset.
- [ ] **Step 4: Run them** — Expected: all pass, 4 new; S1/S2 tests all pass (a `_Today` test that pinned a fixed fault
  is renamed and changed to the document's value).
- [ ] **Step 5: Commit** `CHR27-1: S3 error and split answers read whole`.

#### Task 2: The rest of the review's S3 rows

- [ ] **Step 1:** For each review row marked `fault — fixed in S3`: a failing test `FrameTests.<RowId>_<What>` first,
  then the fix, then all pass. One commit per row, `CHR27-1: S3 <row id> <what>`. Mark each row's verdict `fixed`.

- [ ] **Demo:** the test run on S2's code (red) and on S3's code (green).

Slice end: `level54-build` runs the review, fixes, verification, slice report and Definition of Done.

---

### Slice S4 — Speed: the transport inside, measured

**Delivers:** the same calls, faster where the timing shows it (story 3).
**Blocked by:** S3
**Door:** public API: settled by D3 — not a stop
**Seams tested:** the public SDK against a fake device (all S1–S3 tests unchanged and green are the proof of no
behaviour change).
**Review focus:** a caller's handler for `RecordDownloaded` / `EmployeesDataGet` doing slow work while pings arrive;
the answer timer leaking per command; `Dispose` during a download.

#### Task 1: Measured changes

**Files:**
- Modify: `Anviz.SDK/AnvizStream.cs`, `Anviz.SDK/Responses/Response.cs`, `Anviz.SDK/Commands/Command.cs`,
  `Anviz.SDK/Utils/Bytes.cs`, and the command files a change touches
- Test: `Anviz.SDK.Tests/TimingTests.cs`

- [ ] **Step 1:** Take each candidate in turn; keep it only if the S1 timing table improves (or it removes a review S4
  row's fault) and every test stays green; one commit each, `CHR27-1: S4 <change> (<before> → <after>)`. Candidates:
  socket `NoDelay` (small frames held back by the network stack); one answer buffer sized from LEN instead of 16,000
  bytes per frame; byte reads without LINQ `Skip/Take`; each record parsed once in `DownloadRecords`; the answer timer
  cancelled when the answer comes; the waiting caller resumed off the receiver (so a slow handler cannot stall pings);
  the receiver as an async loop instead of a blocked thread; debug log text built only when debug logging is on.
- [ ] **Step 2: Run** `TimingTests` — Expected: all pass; the "after" table is printed. Put it beside "before" in the
  review, with each kept change and its gain. "No measurable gain" is a valid line (C14).

- [ ] **Demo:** the before/after timing table.

Slice end: `level54-build` runs the review, fixes, verification, slice report and Definition of Done.

---

### Slice S5 — M7 check program, proved on the fake device

**Delivers:** a console program that runs the D2-safe checks against any address and prints each result in the
review's row ids.
**Blocked by:** S2
**Door:** writes to a real device: settled by D2, D9, D10 — not a stop
**Seams tested:** the real M7 by console check (here proved against the fake device).
**Review focus:** a forbidden command reachable from the program (there must be no code path to it); the test user's id
colliding with a real user (pick an id the device reports as absent, refuse to run otherwise); cleanup when a step fails.

#### Task 1: The program

**Files:**
- Create: `Anviz.SDK.M7Check/Anviz.SDK.M7Check.csproj` (net10.0, Exe, references `Anviz.SDK`), `Anviz.SDK.M7Check/Program.cs`
- Modify: `Anviz.sln`
- Test: `Anviz.SDK.Tests/M7CheckTests.cs` (references the program's project; runs it in-process against the fake)

**Interfaces:**
- Produces: `M7Check.RunAsync(string host, int port, TextWriter output, bool enrol, bool time) : Task<int>` (0 = every
  check passed) — usage `Anviz.SDK.M7Check <host> [port] [--enrol] [--time]`.
- Steps it runs, in order: device type; record and user statistics; all records ("all", never "new"); all users; the
  basic and advanced settings, date/time, device ID, serial number, TCP parameters, time zones, bells (reads only);
  add test user (id = highest device user id + 1, refuse if that id exists) with name `CHR27 TEST`, password 4321, card
  12345678, a made-up 338-byte template; read each back and compare; set the clock to the PC's time and read it back;
  `--enrol`: prompt "Press a finger on the M7 now", enrol for the test user, read the template back; remove the test
  user in a `finally`, then check it is gone; `--time`: the S1 timing set, read-only (records, users, the test user's
  template). Each line: row id, raw frame bytes sent and received, verdict.

- [ ] **Step 1: Write the failing test** `M7CheckTests.RunAgainstFake_AllChecksPass_AndTestUserRemoved`

```csharp
var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: true, time: false);
Assert.Equal(0, code);
Assert.DoesNotContain(fake.Users, u => u.Name == "CHR27 TEST");
Assert.DoesNotContain(fake.Received, f => f.Code is 0x4E or 0x4D or 0x2E or 0x4F or 0x3B or 0x47 or 0x25 or 0x5E);
```
  (the forbidden codes: clear records, delete all users, reboot, factory reset, TCP set, device ID set, serial number
  set, unlock — confirm each code against the review document).
- [ ] **Step 2: Run it** — Expected: FAIL — the program does not exist.
- [ ] **Step 3: Implement** the program.
- [ ] **Step 4: Run it** — Expected: all pass, 1 new. Also `M7CheckTests.FailingStep_StillRemovesTestUser` (the fake
  refuses the template write) — all pass, 2 new.
- [ ] **Step 5: Commit** `CHR27-1: S5 M7 check program`.

- [ ] **Demo:** the program's output against the fake device.

Slice end: `level54-build` runs the review, fixes, verification, slice report and Definition of Done.

---

### Slice S6 — The M7 run: verdicts, M7 fixes, M7 timings, enrolment

**Delivers:** every review row has its M7 verdict; deviations the M7 decides are fixed or kept (D4); M7 timings before
and after; enrolment proved on the M7 (stories 1, 2, 6).
**Blocked by:** S4, S5, and the owner connecting the M7 (the build stops and asks for its address when it reaches here)
**Door:** none
**Seams tested:** the real M7 by console check; the public SDK against a fake device for each fix.
**Review focus:** a fix the M7 proves that would change an M5 site (D8: only where document and M7 agree); the test user
left on the device after a failed run.

#### Task 1: The runs

- [ ] **Step 1:** Check the M7 answers: `Test-NetConnection <host> -Port 5010`.
- [ ] **Step 2:** "Before": build `Anviz.SDK.M7Check` in `D:\Worktrees\anviz-before` against `development`'s SDK; run
  `--time` only (reads). Save the output to the review's appendix.
- [ ] **Step 3:** "After": run `Anviz.SDK.M7Check <host> --time` from this branch. Save the output.
- [ ] **Step 4:** Fill each row's M7 column and verdict from the output.

#### Task 2: The fixes the M7 decides

- [ ] **Step 1:** For each row now `M7 follows the document`: a fake-device test with the document's value (red), the
  fix, all pass; one commit per row `CHR27-1: S6 <row id> <what>`. For each row `M7 follows the code`: the comment
  `// M7 deviation: ...` at the code, the verdict `kept — M7 deviation`.
- [ ] **Step 2:** Run the M7 check again after the fixes. Expected: exit code 0.

#### Task 3: Enrolment

- [ ] **Step 1:** Tell the owner in one line when to press a finger, then run `Anviz.SDK.M7Check <host> --enrol`.
  Expected: exit code 0; the enrolment row is filled; the test user is gone.
- [ ] **Step 2:** Remove `D:\Worktrees\anviz-before`. Commit the review `CHR27-1: S6 M7 verdicts and timings`.

- [ ] **Demo:** the M7 outputs, and the review with no `pending` left.

Slice end: `level54-build` runs the review, fixes, verification, slice report and Definition of Done.

---

### Slice S7 — Chronos proof, changelogs, merge

**Delivers:** proof that Chronos main and CHR2027 build and pass their tests on the new SDK with no code change (story
7, C11); the changelogs; the merge into Anviz `development` once the owner says so.
**Blocked by:** S6
**Door:** merge into Anviz `development` and push: unsettled — the build stops before Task 2
**Seams tested:** Chronos unchanged.
**Review focus:** a Chronos call that compiled but now behaves differently (an exception type, a returned null); the
two unpushed CHR-165 commits already on `development` going out with the merge.

#### Task 1: Chronos on the new SDK

- [ ] **Step 1:** Make throwaway worktrees `D:\Worktrees\chr-main` (Chronos `main`) and `D:\Worktrees\chr-2027`
  (Chronos `CHR2027`), and a junction `D:\Worktrees\Anviz` → `D:\Worktrees\anviz-chr27-1`: their projects'
  `..\..\Anviz` reference (from `D:\Worktrees\chr-main\ChronosApp`) then resolves to this branch.
- [ ] **Step 2:** Build each with Visual Studio's MSBuild (Debug). Expected: 0 errors.
- [ ] **Step 3:** Run `ChronosApp.Tests` and `Chronos.Web.Tests` (no DB variables) in each. Expected: all pass.
- [ ] **Step 4:** Remove the worktrees and the junction. Put the results in the review.
- [ ] **Step 5:** Anviz `CHANGELOG.md` v2.0.17 section complete; Chronos `CHANGELOG.md` `[main]` entry drafted for
  the merge (C15). Commit `CHR27-1: S7 Chronos proof and changelogs`.

#### Task 2: Merge (the build stops before this task and asks the owner)

- [ ] **Step 1:** On the owner's yes: merge `CHR27-1` into Anviz `development` in `C:\Source Code\General\Anviz`;
  build Chronos CHR2027 there once. Push only if the owner said push.
- [ ] **Step 2:** Remove the worktree `D:\Worktrees\anviz-chr27-1` once merged.

- [ ] **Demo:** the Chronos build and test output on the new SDK.

Slice end: `level54-build` runs the review, fixes, verification, slice report and Definition of Done.
