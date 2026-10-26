## v2.0.17 (unreleased)
CHR27-1 (review against the protocol document, refactor, tested on the M7):
* S1 `Anviz.SDK.Tests`: a fake device that checks every frame against the protocol document; today's behaviour of the calls Chronos makes pinned (`_Today` where the code differs from the document); first timings
* S2 `Docs/anviz_sdk_review_CHR27-1.md`: every command and answer field checked against the protocol document (181 rows, row ids R<code>.<n>); fake-device tests for every other command, the real-time record and the "new records" request
* S3 frame faults fixed: a device error answer is read whole and thrown as `AnvizException`, the connection stays; split frames read whole; device-sent frames read at the right length; Dispose can no longer hang; a stalled write times out; the pong no longer interleaves with a command; a skipped user is logged, not silently dropped; an over-long name is cut to its field
* S4 transport hygiene: socket NoDelay, frame buffer sized from LEN (no limit at 15,990 bytes), answer and pong timers cancelled, field reads without LINQ, each record read once, debug text built only when debug logging is on. No measurable gain on the fake device; the M7 timing is S6
* S5 `Anviz.SDK.M7Check`: the real-device check program behind an allow-list relay that logs every frame; the test user is removed even when the run is stopped

## v2.0.16
FEATURES:
* InquireCard command to read current card, like EnrollFingerprint

## v2.0.15
FEATURES:
* Get/set device Scheduled Bells

IMPROVEMENTS:
* `RelayMode` is now an enum
* Check TimeZone ranges

## v2.0.14
FEATURES:
* Get/set device TimeZone information

## v2.0.13
BUG FIXES:
* Pong response was resetting internal state for parsing responses
* `SendCommand` now implements better timeout handling

## v2.0.12
BUG FIXES:
* Nuget signing key error
* `ReceivedRecord` event for RealTime support

## v2.0.11
IMPROVEMENTS:
* Ping requests are now automatically answered

## v2.0.10
IMPROVEMENTS:
* `EnrollFingerprint`: you can now specify number of times to check user FP. Default to 2 (old behaviour)

## v2.0.9
UPGRADE NOTES:
* `GetDeviceID` is now deprecated in favor of `DeviceID` property

## v2.0.0
BREAKING CHANGES:

* `AnvizManager` doesn't need `DeviceID` anymore since it's requested to the connected device automatically
* `TcpParameters` class now implements `IPAddress` for IP fields and `PhysicalAddress` for MAC

FEATURES:

* Set device network info
* Set device SN (only on supported Anviz devices)
* Set device ID
* Reboot device
* Reset device to Factory Settings
* Unlock door
* Get/set basic settings
* Get/set advanced settings
* Set/delete staff data
* Set fingerprint template
* Enroll fingerprint interactively
* Set/get facepass template
* Upload/delete records
* Server mode support
