# Anviz SDK review and refactor — spec

Date: 2026-10-25   Ticket: CHR27-1
Size: project   Status: approved 2026-10-25

## 1. Problem

Chronos talks to Anviz clocks through Anviz.SDK, a fork of the MxLabs Anviz library. The ticket:

> The code must be fully reviewed for compliance with the document and any deviations must be identified.
> The code must also be refactored for best performance.
> This work must all be done on a separate worktree/branch and tested for compliance against the actual Anviz device.

"The document" is the *Communication Protocol* PDF (TC_B Communication Protocol V2) in the repo's Docs folder. The
device we have is an **Anviz M7** (the ticket says M5; there is no M5). No review of the SDK has been trusted so far:
the April 2026 review document is set aside and not used.

Faults are already visible on a first reading, for example: when the device answers a command with an error code, the
SDK stops reading that answer half-way, so the next answer is read from the wrong place; a header that arrives in two
network pieces is treated as a lost connection. Chronos sees these as dropped connections and failed downloads.

## 2. Solution

Every command the SDK sends and every answer it reads is checked, field by field, against the protocol document and
then against the M7. The result is a written review that lists each deviation and what was done about it. The faults
are fixed, the code is made faster where a measurement shows it, and Chronos keeps calling the SDK exactly as it does
now.

1. As the owner, I want a review document that lists, for every SDK command, whether it matches the protocol document
   and the M7, so that I know exactly where the SDK and the document differ.
2. As the owner, I want every deviation that is a fault on the M7 fixed, and every deviation where the M7 agrees with
   the code kept and marked, so that the SDK works with the real device.
3. As a Chronos user, I want downloads of records, users and fingerprint templates to be as fast as the device allows,
   so that a device download takes less time.
4. As a Chronos user, I want a device error (for example "user does not exist") to come back as that error and leave
   the connection working, so that one refused command does not end the whole session.
5. As the owner, I want automated tests for the SDK that run without any device, so that each fix is proved and stays
   fixed.
6. As the owner, I want a short check run on the real M7 at the end, so that the result is proved on the device and not
   only on a fake.
7. As a Chronos developer, I want the main and CHR2027 Chronos branches to build and pass their tests against the new
   SDK with no change to their code.

## 3. Decisions — owner

- D1. The device under test is the Anviz M7. The owner connects it to the network; the real-device check waits until
  then (round 1, Q1, and the owner's note).
- D2. On the M7 the agent runs the checks itself: read commands, plus one test user that the check adds and then
  removes. It never clears records, resets to factory settings, reboots, or changes the device's network settings, ID
  or serial number (round 1, Q1).
- D3. The public API stays the same: every public class, method, event, property and name that Chronos or another
  caller uses keeps its shape. Only the inside changes (round 1, Q2).
- D4. When the code and the document differ, the M7 decides: if the M7 follows the document, the code is fixed; if the
  M7 follows the code, the code stays, gets a comment naming the deviation, and the review lists it (round 1, Q3).
- D5. Protocol commands the SDK does not have are listed in the review and not built (round 1, Q4).
- D6. A new automated test project for the SDK (xUnit v3, as the Chronos tests) drives the public SDK against a fake
  device on 127.0.0.1; one small console check covers the real M7 (round 1, Q5).
- D7. The April 2026 review document is ignored: the review starts fresh from the protocol document (owner's note).
- D8. Sites may run the M5 or other Anviz models. The M5 path (the ANSI staff record) and the other models' changes stay
  as they are; a command's data layout changes only where the document and the M7 agree. The review lists the M5 path as
  "not tested" (round 2, Q1).
- D9. The M7 check may set the device clock to the PC's time, as Chronos does on every download, and read it back
  (round 2, Q2).
- D10. Fingerprint enrolment is tested on the M7: the owner presses a finger once, at the end, when the agent says so.
  This is the only owner step in the M7 check (round 2, Q3).

## 4. Calls — agent

- C1. The work is on the Anviz branch `CHR27-1`, cut from `development`, in its own worktree. Chronos keeps building
  from the normal Anviz checkout, so it does not get the change until the owner says to merge it into `development`.
- C2. The review is a new document in the Anviz repo's Docs folder, one row per command and per answer field: what
  the document says, what the code does, what the M7 does, and the verdict (matches / fixed / kept as an M7 deviation /
  not tested on the M7, with the reason).
- C3. Speed is measured before and after on the fake device for the work Chronos does most: download all records, get
  all users, get and set fingerprint templates, set users. A change made for speed stays only if the measurement shows
  it helps; the numbers go in the review.
- C4. The transport (the part that sends a command and waits for its answer) may be rewritten inside, as long as D3
  holds: one command at a time per connection, the same timeouts, the same events (device ping, received packet,
  received record, device error).
- C5. A device error answer (the document's return codes) is read to the end of its frame and raised to the caller as
  the SDK's existing Anviz error type; the connection stays open. A frame that cannot be read (bad header, bad CRC,
  closed socket) still ends the connection as now.
- C6. On the M7, records are read with the "all records" request only. The "new records" request changes the device's
  new-record marker, so it is proved on the fake device and listed in the review as not run on the M7.
- C7. Commands Chronos does not use but the SDK has (reboot, factory reset, network settings, device ID and serial
  number, door unlock, bells, time zones, face templates and the like) are reviewed against the document and tested on
  the fake device; the ones D2 forbids are listed as not run on the M7.
- C8. The SDK gets its own fake device inside its test project, built from the protocol document. Chronos's own fake
  device in its web tests stays as it is.
- C9. The Anviz repo's own changelog gets an entry for the new version; the Chronos CHR2027 changelog gets one
  `[CHR2027]` entry per slice and the Chronos slice reports go on CHR27-1.
- C10. A fix the M7 needs that the document does not describe (an M7 extension) is kept and named as such in the
  review, the same as D4.
- C11. Before the merge, Chronos main and CHR2027 are built and their tests run against the new SDK (by pointing a
  throwaway Chronos build at the worktree), so D3 is proved, not assumed.
- C12. A command the M7 check cannot run (clear records, the "new records" request, the forbidden writes in D2): its
  data layout stays as the code has it, because sites run it today; the deviation from the document is listed as "not
  tested on the M7". Only faults in the frame reading and sending, which every command and every model share, are fixed
  for these commands.
- C13. The test user of D2 includes its name, password, card number and one fingerprint template made up by the check;
  each is written, read back and compared, then the user is removed.
- C14. Speed is also timed on the M7, read-only (all records, all users, the test user's template), before and after.
  "No measurable gain" is a valid result and is reported as such.
- C15. The work stays on CHR27-1. Merging into Anviz `development` also ships the new SDK to the next main release, so
  at the merge the Chronos main changelog gets a `[main]` entry next to the `[CHR2027]` ones, and the main release notes
  get a line at the next bump.

## 5. Test seams agreed

- **The public SDK against a fake device**: the new SDK test project connects `AnvizManager` to a fake Anviz device
  listening on 127.0.0.1 and calls the public `AnvizDevice` methods; the fake checks every frame it receives against the
  protocol document (header, device ID, command, length, data, CRC) and answers with frames built the same way,
  including error answers and answers split across several network reads. Prior art: the fake Anviz device and its
  tests in Chronos.Web.Tests (same idea, Chronos's scenarios).
- **The real M7 by console check**: a small console program in the Anviz repo connects to the M7 and runs the D2-safe
  commands, printing each frame's fields and the verdict for the review. Run by the agent once the owner connects the
  device.
- **Chronos unchanged**: the existing Chronos test suites (ChronosApp.Tests, Chronos.Web.Tests' fake-device tests) run
  green against the new SDK.

## 6. One-way doors

- A contract between apps (the SDK's public API, used by ChronosApp and Chronos.Web): settled by D3.
- A new third-party dependency (xUnit v3 in the Anviz repo, test project only): settled by D6.
- Writes to a real device: settled by D2.
- Merging into Anviz `development` (which both Chronos branches build from) and pushing: not settled — the build stops
  and asks before the merge.

## 7. Out of scope

- Building the protocol commands the SDK does not have (D5).
- Any change to Chronos code (D3); a Chronos change found needed goes in the slice report's "Found, not done".
- The M5 and other Anviz models; the CrossChex SDK document is used only to explain a field the protocol document leaves
  unclear.
- Publishing the SDK as a NuGet package.
- Real-device commands D2 forbids.

## 8. Glossary terms

- **Deviation**: a place where the SDK, the protocol document and the M7 do not all agree.
- **M7 deviation**: a deviation where the M7 agrees with the code and not with the document (kept, marked, listed).
- **Frame**: one command or one answer on the wire — header byte, device ID, command or answer code, return code
  (answers only), data length, data, CRC.
