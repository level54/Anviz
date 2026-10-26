using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Anviz.SDK.Exceptions;
using Anviz.SDK.Responses;
using Anviz.SDK.Utils;

namespace Anviz.SDK.M7Check;

/// <summary>
/// The M7 check (CHR27-1 S5): runs the D2-safe checks against one device and prints each result under the row ids of
/// Docs/anviz_sdk_review_CHR27-1.md, with the raw frames sent (&gt;&gt;) and received (&lt;&lt;), the decoded values and a
/// verdict. Writes only the test user (C13: added, read back, removed in a finally, also after Ctrl+C), the PC's time (D9) and, with
/// --enrol, one enrolment for the test user (D10). Every frame goes through <see cref="FrameRelay"/>, which also refuses
/// any frame outside that list.
/// </summary>
public static class M7Check
{
    public const string Usage = "Usage: Anviz.SDK.M7Check <host> [port 1-65535, default 5010] [--enrol] [--time]\n" +
                                "       Anviz.SDK.M7Check <host> [port] --remove <id>   (removes the test user \"CHR27 TEST\" a stopped run left)";
    public const string TestUserName = "CHR27 TEST";
    private const ulong TestPassword = 4321, TestCard = 12345678;

    /// <summary>The made-up 338-byte FP1 template (C13).</summary>
    private static readonly byte[] TestTemplate = Enumerable.Range(0, 338).Select(i => (byte)(i * 7 + 3)).ToArray();

    public static async Task<int> Main(string[] args)
    {
        var rest = args.ToList();
        ulong removeId = 0;
        var remove = rest.IndexOf("--remove");
        if (remove >= 0)
        {
            if (remove + 1 >= rest.Count || !ulong.TryParse(rest[remove + 1], out removeId) || removeId == 0)
                return UsageError();
            rest.RemoveRange(remove, 2);
        }
        var flags = rest.Where(a => a.StartsWith("--")).ToArray();
        rest = rest.Except(flags).ToList();
        var port = 5010;
        if (rest.Count is < 1 or > 2 || flags.Except(["--enrol", "--time"]).Any() || (removeId != 0 && flags.Length > 0) ||
            (rest.Count == 2 && (!int.TryParse(rest[1], out port) || port is < 1 or > 65535)))
            return UsageError();
        if (removeId != 0) return await RemoveAsync(rest[0], port, removeId, Console.Out);

        // Ctrl+C or closing the window stops the run; the test user is still removed (the finally in AllAsync).
        using var stop = new CancellationTokenSource();
        using var finished = new ManualResetEventSlim();
        ConsoleCancelEventHandler onCtrlC = (_, e) =>
        {
            e.Cancel = true;
            stop.Cancel();
        };
        Console.CancelKeyPress += onCtrlC;
        // Window close: Windows ends the process about 5 s after the close event, so hold it while the removal runs.
        using var onClose = PosixSignalRegistration.Create(PosixSignal.SIGHUP, _ =>
        {
            stop.Cancel();
            finished.Wait(TimeSpan.FromSeconds(4.5));
        });
        try
        {
            return await RunAsync(rest[0], port, Console.Out, flags.Contains("--enrol"), flags.Contains("--time"), stop.Token);
        }
        finally
        {
            finished.Set();
            Console.CancelKeyPress -= onCtrlC;
        }
    }

    private static int UsageError()
    {
        Console.Error.WriteLine(Usage);
        return 2;
    }

    /// <summary>Runs every check; 0 when each passed. <paramref name="cancel"/> stops it after the current check and still removes the test user.</summary>
    public static async Task<int> RunAsync(string host, int port, TextWriter output, bool enrol, bool time, CancellationToken cancel = default)
    {
        await using var relay = new FrameRelay(host, port);
        using var run = new Run(relay, output, host, port, cancel);
        output.WriteLine($"Anviz M7 check of {host}:{port} through the relay 127.0.0.1:{relay.Port}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}" +
                         $"{(enrol ? ", --enrol" : "")}{(time ? ", --time" : "")}");
        return await run.AllAsync(enrol, time);
    }

    /// <summary>Removes the test user a stopped run left (--remove): only when the device lists that id with the name "CHR27 TEST".</summary>
    public static async Task<int> RemoveAsync(string host, int port, ulong id, TextWriter output)
    {
        await using var relay = new FrameRelay(host, port);
        using var run = new Run(relay, output, host, port, default);
        output.WriteLine($"Anviz M7 check of {host}:{port} through the relay 127.0.0.1:{relay.Port}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, --remove {id}");
        return await run.RemoveLeftUserAsync(id);
    }

    private sealed class Run(FrameRelay relay, TextWriter output, string host, int port, CancellationToken cancel) : IDisposable
    {
        private const int FramesShown = 6;
        private AnvizDevice? device;
        private int passed, failed, pings;
        private ulong remains;

        public async Task<int> AllAsync(bool enrol, bool time)
        {
            try
            {
                return await ChecksAsync(enrol, time);
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested)
            {
                return Summary("Ctrl+C or the window closed");
            }
        }

        public async Task<int> RemoveLeftUserAsync(ulong id)
        {
            var isTestUser = await Check("R72.1", $"0x72 id {id} is the test user \"{TestUserName}\"", async (d, v) =>
            {
                var user = (await d.GetEmployeesData()).FirstOrDefault(u => u.Id == id);
                if (user == null) return Refuse(v, $"id {id} is not in the device's user list");
                v.Add($"read: {Describe(user)}");
                return user.Name == TestUserName || Refuse(v, $"id {id} is not named \"{TestUserName}\"");
            });
            if (!isTestUser)
            {
                output.WriteLine("REFUSED: that id is not the test user; nothing was written to the device.");
                return Summary("refused");
            }
            relay.TestUserId = id;
            await Remove(id);
            relay.TestUserId = 0;
            return Summary(null);
        }

        private string RemoveStep(ulong id) => $"Anviz.SDK.M7Check {host} {port} --remove {id}";

        private async Task<int> ChecksAsync(bool enrol, bool time)
        {
            if (!await Check("RF.1 RF.2 RF.3 RF.4 RF.7 RF.8 R48.1 R48.2 R48.3 R48.4", "0x48 device type (sent by Connect)", (d, v) =>
                {
                    v.Add($"type code \"{d.DeviceType}\", biometric type {d.DeviceBiometricType}, device id (the answer's CH) {d.DeviceId}");
                    return Task.FromResult(true);
                }))
                return Summary("the device did not answer: nothing else was run");

            Statistic? stats = null;
            await Check("R3C.1 R3C.2", "0x3C record and user statistics", async (d, v) =>
            {
                stats = await d.GetDownloadInformation();
                v.Add(Describe(stats));
                return true;
            });
            await Check("R40.1 R40.2 R40.3 R40.4 R40.5 R40.6 R40.7 R40.8", "0x40 all records (parameter 1, then 0; never 2)", async (d, v) =>
            {
                var records = await d.DownloadRecords(onlyNew: false);
                v.Add($"{records.Count} records; the first {Math.Min(3, records.Count)}:");
                foreach (var r in records.Take(3))
                    v.Add($"user {r.UserCode}, time {r.DateTime:yyyy-MM-dd HH:mm:ss} (SDK epoch 2 Jan 2000; the document's 1 Jan 2000 " +
                          $"gives {r.DateTime.AddDays(-1):yyyy-MM-dd HH:mm:ss}), backup code {r.BackupCode}, record type {r.RecordType}, work types {r.WorkType}");
                return true;
            });
            List<UserInfo>? users = null;
            await Check("R72.1 R72.2 R72.3 R72.4 R72.5 R72.6 R72.7 R72.8 R72.9 R72.10 R72.11 R72.12", "0x72 all users", async (d, v) =>
            {
                users = await d.GetEmployeesData();
                v.Add($"{users.Count} users; the first {Math.Min(5, users.Count)}:");
                v.AddRange(users.Take(5).Select(Describe));
                return true;
            });
            await Reads();

            // The test user (C13): the highest id + 1, only when the device shows it free.
            ulong testId = 0;
            var free = await Check("R44.1 R44.5 RF.10", "the test user's id is free (0x44 FP1 read: ACK_NO_USER expected)", async (d, v) =>
            {
                if (users == null || stats == null) return Refuse(v, "the users or the statistics were not read");
                if ((ulong)users.Count != stats.UserAmount)
                    return Refuse(v, $"the user list has {users.Count} users, 0x3C says {stats.UserAmount}: the highest id may not be the device's");
                testId = (users.Count == 0 ? 0 : users.Max(u => u.Id)) + 1;
                v.Add($"test user id = the highest user id + 1 = {testId}");
                if (testId > 0xFF_FFFF_FFFF) return Refuse(v, "the id does not fit in 5 bytes");
                // Any device answer other than "no such user" (ACK_NO_USER) fails this check: the id is then not shown free.
                return await NoSuchUser(d, testId, v) || Refuse(v, $"id {testId} already holds an FP1 template");
            });
            if (!free)
            {
                output.WriteLine("REFUSED: the test user's id is not shown free; nothing was written to the device.");
                return Summary("refused");
            }

            cancel.ThrowIfCancellationRequested();
            output.WriteLine($"Test user id {testId} will be added; if this run is stopped, remove it with: {RemoveStep(testId)}");
            relay.TestUserId = testId;
            try
            {
                await Writes(testId, enrol);
                if (time) await Timings(testId);
            }
            finally
            {
                await Remove(testId);
                relay.TestUserId = 0;
            }

            await Check("R3C.2 (C6)", "0x3C the new-record count is unchanged by the check", async (d, v) =>
            {
                var after = await d.GetDownloadInformation();
                v.Add($"new records before {stats?.NewRecordAmount}, after {after.NewRecordAmount}; all records before {stats?.AllRecordAmount}, after {after.AllRecordAmount}");
                return stats != null && after.NewRecordAmount == stats.NewRecordAmount;
            });
            output.WriteLine($"== RF.13 RFF.1 · device pings seen: {pings} (each answered 0xFF by the SDK; frames above)");
            return Summary(null);
        }

        /// <summary>The settings reads (D2: reads only).</summary>
        private async Task Reads()
        {
            await Check("R30.1 R30.2 R30.3 R30.4 R30.5 R30.6 R30.7 R30.8 R30.9 R30.10", "0x30 basic settings", async (d, v) =>
            {
                var s = await d.GetBasicSettings();
                v.Add($"firmware \"{s.Firmware}\", password {s.ManagementPassword}, sleep {s.Sleep}, volume {s.Volume}, language {s.Language}, " +
                      $"date format {s.DateFormat}, 24-hour {s.Is24HourClock}, attendance state {s.Attendance}, language flag {s.LangCHG}, command version {s.CMDVersion}");
                return true;
            });
            await Check("R32.1 R32.2 R32.3 R32.4 R32.5 R32.6 R32.7 R32.8 R32.9 R32.10 R32.11 R32.12 R32.13 R32.14", "0x32 advanced settings", async (d, v) =>
            {
                var s = await d.GetAdvancedSettings();
                v.Add($"FP precision {s.FPPrecision}, Wiegand head {s.WiegandHead}, Wiegand option {s.WiegandMode}, work code {s.WorkCode}, " +
                      $"real-time {s.RealTimeMode}, FP auto update {s.FPAutoUpdate}, relay {s.RelayMode}, lock delay {s.LockDelay}, " +
                      $"memory full alarm {s.MemoryFullAlarm}, repeat attendance delay {s.RepeatAttendanceDelay}, door sensor delay {s.DoorSensorDelay}, " +
                      $"bell delay {s.ScheduledBellDelay}, reserved {s.Reserved}");
                return true;
            });
            await Check("R38.1 R38.2 R38.3", "0x38 date and time", async (d, v) =>
            {
                var clock = await d.GetDateTime();
                v.Add($"device {clock:yyyy-MM-dd HH:mm:ss}, PC {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                return true;
            });
            await Check("R46.1 R46.2 R46.3", "0x46 device id", async (d, v) =>
            {
#pragma warning disable CS0612 // obsolete in the SDK; the review's 0x46 table runs it (R46.3: which id it is)
                v.Add($"device id {await d.GetDeviceID()}; the CH the device answers with: {d.DeviceId}");
#pragma warning restore CS0612
                return true;
            });
            await Check("R24.1 R24.2", "0x24 serial number", async (d, v) =>
            {
                v.Add($"serial number \"{await d.GetDeviceSN()}\"");
                return true;
            });
            await Check("R3A.1 R3A.2 R3A.3 R3A.4 R3A.5 R3A.6 R3A.7", "0x3A TCP/IP parameters", async (d, v) =>
            {
                var t = await d.GetTcpParameters();
                v.Add($"IP {t.IP}, mask {t.SubnetMask}, MAC {t.MacAddress}, gateway {t.DefaultGateway}, server {t.Server}, far limit {t.FarLimit}, " +
                      $"port {t.ComPort}, mode {t.TcpMode}, DHCP {t.DhcpEnabled}");
                return true;
            });
            await Check("R50.1 R50.2 R50.3", "0x50 time zones 1 and 30 (the SDK sends 1-30 as they are; 0 it refuses before sending)", async (d, v) =>
            {
                foreach (byte number in new byte[] { 1, 30 })
                    try
                    {
                        var zone = await d.GetTimeZoneInfo(number);
                        v.Add($"zone {number}: " + string.Join(", ", zone.Days.Select(day => $"{day.Key} {day.Value.From:hh\\:mm}-{day.Value.To:hh\\:mm}")));
                    }
                    catch (AnvizException e)
                    {
                        v.Add($"zone {number}: the device answered {Ret(e)}");
                    }
                return true;
            });
            await Check("R54.1 R54.2 R54.3 R54.4 R54.5", "0x54 scheduled bells", async (d, v) =>
            {
                var bells = await d.GetScheduledBells();
                v.Add($"{bells.Count} slots, {bells.Count(b => b.HasValue)} set: " +
                      string.Join(", ", bells.Select((b, i) => (b, i)).Where(x => x.b.HasValue).Select(x => $"{x.i + 1}: {x.b.Time:hh\\:mm} {x.b.Days}")));
                return true;
            });
            await Check("R7E.1 R7E.2", "0x7E inquire card (once; the M7 is not a T5S, ACK_FAIL expected)", async (d, v) =>
            {
                v.Add($"card {await d.InquireCard(retries: 1)}");
                return true;
            }, errorAnswerPasses: true);
        }

        /// <summary>Add the test user, read each part back, set the clock, enrol (--enrol).</summary>
        private async Task Writes(ulong testId, bool enrol)
        {
            await Check("R73.1 R73.2 R73.3 R73.4 R73.5 R73.6 R73.7 R73.8 R73.9 R73.10 R73.11", "0x73 add the test user (Unicode record)", async (d, v) =>
            {
                var user = new UserInfo(testId, TestUserName) { Password = TestPassword, Card = TestCard };
                v.Add($"sent: {Describe(user)}");
                await d.SetEmployeesDataUnicode(user);
                v.Add($"R73.11 answer flag (bit 0 = user 1 stored): {AnswerFlag()}");
                return true;
            });
            await Check("R73.13", "0x73 the test user again (an update of a stored user)", async (d, v) =>
            {
                await d.SetEmployeesDataUnicode(new UserInfo(testId, TestUserName) { Password = TestPassword, Card = TestCard });
                v.Add($"answer flag (bit 0 = user 1 stored; 0 would mark the update not stored): {AnswerFlag()}");
                return true;
            });
            await Check("R72.4 R72.5 R72.6 R72.7 R72.8 R72.9 R72.10 R72.11 R72.12 (R73.2-R73.10 read back) RF.5", "0x72 read the test user back", async (d, v) =>
            {
                var back = (await d.GetEmployeesData()).FirstOrDefault(u => u.Id == testId);
                if (back == null)
                {
                    v.Add($"id {testId} is not in the device's user list");
                    return false;
                }
                v.Add($"read: {Describe(back)}");
                // The read-back alone shows only that the bytes come back; the device's own 3-byte user count (0x3C,
                // matched to the list in the id-free check) is a number the device wrote itself.
                v.Add($"RF.5 byte order: card {TestCard} went out high byte first ({Convert.ToHexString(BitConverter.GetBytes((uint)TestCard).Reverse().ToArray())}, " +
                      $"the >> 0x73 frame) and read back as {back.Card?.ToString() ?? "none"}; password {TestPassword} read back as {back.Password?.ToString() ?? "none"}; " +
                      "the device's own user count (0x3C), read high byte first, matched the user list before the first write");
                return back.Name == TestUserName && back.Password == TestPassword && back.Card == TestCard;
            });
            await Check("R45.1 R45.2 R45.3 R45.4", "0x45 set the test user's FP1 template (made up, 338 bytes)", async (d, v) =>
            {
                await d.SetFingerprintTemplate(testId, Finger.RightThumb, TestTemplate);
                v.Add($"sent {TestTemplate.Length} bytes");
                return true;
            });
            await Check("R44.1 R44.2 R44.4", "0x44 read the FP1 template back", async (d, v) =>
            {
                var template = await d.GetFingerprintTemplate(testId, Finger.RightThumb);
                var same = template.AsSpan().SequenceEqual(TestTemplate);
                v.Add($"{template.Length} bytes; the same as sent: {same}");
                return same;
            });
            await Check("R44.3", "0x44 the test user's fingers 3-10 (backup codes 3-10; the document defines only 1 and 2)", async (d, v) =>
            {
                for (var finger = Finger.RightMiddle; finger <= Finger.LeftThumb; finger++)
                    try
                    {
                        v.Add($"backup code {(int)finger + 1} ({finger}): {(await d.GetFingerprintTemplate(testId, finger)).Length} bytes");
                    }
                    catch (AnvizException e)
                    {
                        v.Add($"backup code {(int)finger + 1} ({finger}): the device answered {Ret(e)}");
                    }
                return true;
            });
            await Check("R44.6 R44.7", "0x44 GetFaceTemplate (the FP1 frame; the M7 has no face)", async (d, v) =>
            {
                var template = await d.GetFaceTemplate(testId);
                v.Add($"{template.Length} bytes; the same as the FP1 template sent: {template.AsSpan().SequenceEqual(TestTemplate)}");
                return true;
            }, errorAnswerPasses: true);
            await Check("R39.1 R39.2 R39.3 R38.2 R38.3", "0x39 set the clock to the PC's time (D9), then 0x38", async (d, v) =>
            {
                var now = DateTime.Now;
                now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
                await d.SetDateTime(now);
                var back = await d.GetDateTime();
                v.Add($"set {now:yyyy-MM-dd HH:mm:ss}, read back {back:yyyy-MM-dd HH:mm:ss}");
                return Math.Abs((back - now).TotalSeconds) <= 2;
            });
            if (!enrol) return;
            output.WriteLine("Press the same finger on the M7 when it asks — it will ask twice.");
            await Check("R5C.1 R5C.2 R5C.3 R5C.4 R5C.5 R5C.6 R44.2", "0x5C enrol the test user's FP1 (2 captures), then 0x44 reads it (D10)", async (d, v) =>
            {
                // Remove the made-up FP1 first, as Chronos removes a user's fingers before it enrols
                // (frmScanFingerPrint_Anviz._clearOriginalFingers).
                await d.DeleteEmployeesData(testId, VerificationMethod.RightThumb);
                var watch = Stopwatch.StartNew();
                var template = await d.EnrollFingerprint(testId);
                var differs = !template.AsSpan().SequenceEqual(TestTemplate);
                v.Add($"{template.Length} bytes after {watch.Elapsed.TotalSeconds:0.0} s; replaced the made-up template: {differs}");
                return template.Length > 0 && differs;
            });
        }

        /// <summary>
        /// The S1 timing set, read-only (C14): 3 runs each, the median in ms. Through the relay (it keeps the safe list);
        /// the relay's own time inside each run is shown beside it, so its cost is visible, not assumed.
        /// </summary>
        private Task Timings(ulong testId) =>
            Check("C14 (review section 5)", "timings: 3 runs each, median ms, through the relay (frames not kept)", async (d, v) =>
            {
                v.Add("| Case | Median ms | The relay's own time in it, median ms |");
                v.Add("|---|---:|---:|");
                relay.Quiet = true;
                try
                {
                    foreach (var (name, call) in new (string, Func<Task>)[]
                    {
                        ("One round trip (get device type code)", () => d.GetDeviceTypeCode()),
                        ("Download all records", () => d.DownloadRecords()),
                        ("Get all users", () => d.GetEmployeesData()),
                        ("Get the test user's FP1 template", () => d.GetFingerprintTemplate(testId, Finger.RightThumb)),
                    })
                    {
                        double[] times = new double[3], relayTimes = new double[3];
                        for (var i = 0; i < 3; i++)
                        {
                            var relayBefore = relay.RelayTime;
                            var watch = Stopwatch.StartNew();
                            await call();
                            times[i] = watch.Elapsed.TotalMilliseconds;
                            relayTimes[i] = (relay.RelayTime - relayBefore).TotalMilliseconds;
                        }
                        Array.Sort(times);
                        Array.Sort(relayTimes);
                        v.Add($"| {name} | {times[1]:0.0} | {relayTimes[1]:0.0} |");
                    }
                }
                finally
                {
                    relay.Quiet = false;
                }
                return true;
            });

        /// <summary>
        /// Removes the test user (0x4C, backup 0xFF: all its data), once more if the first try failed, then checks it is
        /// gone. Runs even after Ctrl+C. When it is not shown gone, the run ends with a WARNING line (<see cref="Summary"/>).
        /// </summary>
        private async Task Remove(ulong testId)
        {
            Func<AnvizDevice, List<string>, Task<bool>> delete = async (d, v) =>
            {
                v.Add("R4C.4: backup code sent 0xFF (bits 0-3 FP1, FP2, password, card; bits 4-7, which the document does not define, set too)");
                await d.DeleteEmployeesData(testId);
                v.Add($"id {testId} deleted");
                return true;
            };
            if (!await Check("R4C.1 R4C.2 R4C.4", "0x4C remove the test user (backup code 0xFF)", delete, evenWhenStopped: true))
                await Check("R4C.1 R4C.2 R4C.4", "0x4C remove the test user, second try", delete, evenWhenStopped: true);
            var gone = await Check("R4C.5 R44.5", "the test user is gone (0x72 list; 0x44 FP1 read: ACK_NO_USER expected)", async (d, v) =>
            {
                var listed = (await d.GetEmployeesData()).Any(u => u.Id == testId);
                v.Add($"id {testId} in the user list: {listed}");
                return await NoSuchUser(d, testId, v) && !listed;
            }, evenWhenStopped: true);
            if (!gone) remains = testId;
        }

        /// <summary>
        /// The 0x44 FP1 read of <paramref name="id"/>: true when the device answers ACK_NO_USER (free, or gone), false when
        /// it returns a template. Any other device answer throws on, so the calling check fails.
        /// </summary>
        private static async Task<bool> NoSuchUser(AnvizDevice d, ulong id, List<string> values)
        {
            try
            {
                var template = await d.GetFingerprintTemplate(id, Finger.RightThumb);
                values.Add($"FP1 read for id {id}: a {template.Length}-byte template is there");
                return false;
            }
            catch (AnvizException e) when (e.ReturnCode == AnvizReturnCode.NoUser)
            {
                values.Add($"FP1 read for id {id}: the device answered {Ret(e)}: no such user");
                return true;
            }
        }

        /// <summary>The 2-byte DATA of the device's last answer (0x73: bits 0-11 = users 1-12 stored), from the relay.</summary>
        private string AnswerFlag() =>
            relay.LastDeviceFrame is { Length: 13 } f ? $"0x{(f[9] << 8) | f[10]:X4}" : "not in the answer";

        /// <summary>
        /// One check: its frames (the first <see cref="FramesShown"/>, and every BLOCKED or BAD line), its values, its
        /// verdict. A device error answer fails it unless <paramref name="errorAnswerPasses"/>; a lost connection fails it
        /// and the next check connects again. After Ctrl+C it throws OperationCanceledException before it starts, unless
        /// <paramref name="evenWhenStopped"/> (the removal).
        /// </summary>
        private async Task<bool> Check(string rows, string what, Func<AnvizDevice, List<string>, Task<bool>> body,
                                       bool errorAnswerPasses = false, bool evenWhenStopped = false)
        {
            if (!evenWhenStopped) cancel.ThrowIfCancellationRequested();
            output.WriteLine($"== {rows} · {what}");
            var values = new List<string>();
            string verdict;
            try
            {
                device ??= await Connect();
                verdict = await body(device, values) ? "PASS" : "FAIL: the values above are not what was expected";
            }
            catch (AnvizException e)
            {
                verdict = errorAnswerPasses ? $"PASS (the device answered {Ret(e)})" : $"FAIL: the device answered {Ret(e)}";
            }
            catch (Exception e) when (e is DeviceConnectionLostException or IOException or SocketException or ObjectDisposedException)
            {
                verdict = $"FAIL: connection lost ({e.Message}); the next check connects again";
                device?.Dispose();
                device = null;
            }
            catch (Exception e)
            {
                verdict = $"FAIL: {e.GetType().Name}: {e.Message}";
            }

            var frames = relay.TakeFrames();
            for (var i = 0; i < frames.Count; i++)
                if (i < FramesShown || !frames[i].StartsWith(">>") && !frames[i].StartsWith("<<"))
                    output.WriteLine("  " + frames[i]);
            if (frames.Count > FramesShown) output.WriteLine($"  ... {frames.Count - FramesShown} more frames");
            foreach (var value in values) output.WriteLine("  " + value);
            output.WriteLine("  " + verdict);
            if (verdict.StartsWith("PASS")) passed++;
            else failed++;
            return verdict.StartsWith("PASS");
        }

        private async Task<AnvizDevice> Connect()
        {
            var connected = await new AnvizManager().Connect("127.0.0.1", relay.Port);
            connected.DevicePing += (_, _) => Interlocked.Increment(ref pings);
            return connected;
        }

        private static bool Refuse(List<string> values, string why)
        {
            values.Add("REFUSED: " + why);
            return false;
        }

        private int Summary(string? stopped)
        {
            output.WriteLine($"{passed} of {passed + failed} checks passed{(stopped == null ? "" : $"; stopped: {stopped}")}.");
            if (remains != 0)
                output.WriteLine($"WARNING: test user id {remains} REMAINS on the device; remove it with: {RemoveStep(remains)}");
            return failed == 0 && stopped == null && remains == 0 ? 0 : 1;
        }

        private static string Ret(AnvizException e) => $"RET 0x{(byte)e.ReturnCode:X2} ({e.ReturnCode})";

        private static string Describe(Statistic s) =>
            $"users {s.UserAmount}, fingerprints {s.FingerPrintAmount}, passwords {s.PasswordAmount}, cards {s.CardAmount}, " +
            $"all records {s.AllRecordAmount}, new records {s.NewRecordAmount}";

        private static string Describe(UserInfo u) =>
            $"id {u.Id}, name \"{u.Name}\", password {u.Password?.ToString() ?? "none"}, password high 8 bits 0x{u.PWDH8:X2}, " +
            $"card {u.Card?.ToString() ?? "none"}, department {u.Department}, group {u.Group}, mode {u.Mode}, " +
            $"fingers [{string.Join(", ", u.EnrolledFingerprints)}], keep 0x{u.Keep:X2}, special info 0x{u.Message:X2}";

        public void Dispose() => device?.Dispose();
    }
}
