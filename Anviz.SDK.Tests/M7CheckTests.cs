using System.Net;
using System.Net.Sockets;
using Anviz.SDK.Exceptions;
using Anviz.SDK.Responses;
using Anviz.SDK.Tests.Fake;
using static Anviz.SDK.Tests.Fake.CommandCode;

namespace Anviz.SDK.Tests;

// Inside the namespace: the namespace Anviz.SDK.M7Check would otherwise hide its class M7Check.
using M7Check = Anviz.SDK.M7Check.M7Check;
using FrameRelay = Anviz.SDK.M7Check.FrameRelay;

/// <summary>
/// The M7 check program (Anviz.SDK.M7Check) run in-process against the fake device: it must reach only the D2-safe
/// commands, and remove its test user whatever happens.
/// </summary>
public class M7CheckTests
{
    /// <summary>
    /// Forbidden on the M7 (D2), codes as the review's section 2 headings give them: clear records 0x4E, delete all users
    /// 0x4D, reboot 0x2E, factory reset 0x4F, TCP set 0x3B, device id set 0x47, serial number set 0x25, unlock 0x5E;
    /// and outside the M7 list: set basic 0x31 / advanced 0x33 settings, time zone 0x51, bell 0x55, record upload 0x41.
    /// </summary>
    private static readonly byte[] Forbidden = [0x4E, 0x4D, 0x2E, 0x4F, 0x3B, 0x47, 0x25, 0x5E, 0x31, 0x33, 0x51, 0x55, 0x41];

    /// <summary>Every command the program may send (FrameRelay's list); the pong 0xFF only answers a device ping.</summary>
    private static readonly byte[] Allowed =
    [
        GetDeviceTypeCode, GetRecordInformation, DownloadRecords, DownloadStaff, GetBasicSettings, GetAdvancedSettings,
        GetDateTime, GetDeviceId, GetSerialNumber, GetTcpParameters, GetTimeZone, GetBells, InquireCard, DownloadTemplate,
        UploadStaff, UploadTemplate, SetDateTime, EnrollFingerprint, DeleteUser,
    ];

    private static void AssertOnlySafeCommands(FakeAnvizDevice fake)
    {
        Assert.DoesNotContain(fake.Received, f => Forbidden.Contains(f.Code));
        Assert.All(fake.Received, f => Assert.Contains(f.Code, Allowed));
        Assert.DoesNotContain(fake.Received, f => f.Code == DownloadRecords && f.Data[0] == 2); // "new records" (C6)
    }

    private static void AddRealUsers(FakeAnvizDevice fake)
    {
        fake.Users.Add(new FakeUser(1, "Anna") { Password = 1111 });
        fake.Users.Add(new FakeUser(7, "Ben") { Card = 555 });
        fake.Users.Add(new FakeUser(3, "Chen"));
        fake.Records.Add(new FakeRecord(1, new DateTime(2026, 10, 25, 7, 0, 0), 0));
        fake.Records.Add(new FakeRecord(7, new DateTime(2026, 10, 25, 7, 5, 0), 0) { New = true });
    }

    [Fact]
    public async Task RunAgainstFake_AllChecksPass_AndTestUserRemoved()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        var writer = new StringWriter();

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: true, time: true, TestContext.Current.CancellationToken);

        TestContext.Current.TestOutputHelper!.WriteLine(writer.ToString());
        Assert.Equal(0, code);
        Assert.DoesNotContain(fake.Users, u => u.Name == "CHR27 TEST");
        Assert.Equal([1UL, 7, 3], fake.Users.Select(u => u.UserCode)); // the real users untouched
        Assert.Equal(2, fake.Records.Count);
        Assert.True(fake.Records[1].New); // the new-record mark untouched
        AssertOnlySafeCommands(fake);
        // The test user is the highest id + 1, and every write names it alone.
        Assert.All(fake.Received.Where(f => f.Code is UploadTemplate or EnrollFingerprint or DeleteUser),
            f => Assert.Equal(new byte[] { 0, 0, 0, 0, 8 }, f.Data[..5]));
        Assert.All(fake.Received.Where(f => f.Code == UploadStaff), f => Assert.Equal(new byte[] { 1, 0, 0, 0, 0, 8 }, f.Data[..6]));
        Assert.Empty(fake.Templates);
        var text = writer.ToString();
        Assert.DoesNotContain("  FAIL:", text); // the verdict lines
        // K1: the way out is printed before the first write.
        var notice = text.IndexOf($"Test user id 8 will be added; if this run is stopped, remove it with: Anviz.SDK.M7Check 127.0.0.1 {fake.Port} --remove 8");
        Assert.InRange(notice, 0, text.IndexOf("== R73.1 "));
        // K4: the M7-decides rows the program can observe.
        Assert.Contains("== R44.3 ", text);
        Assert.Contains("R4C.4: backup code sent 0xFF", text);
        Assert.Contains("RF.5 byte order:", text);
        Assert.Contains("R73.13", text);
        // K5: one enrolment session, two captures.
        Assert.Contains("Press the same finger on the M7 when it asks — it will ask twice.", text);
        Assert.Equal(2, fake.Received.Count(f => f.Code == EnrollFingerprint));
    }

    [Fact]
    public async Task Stopped_StillRemovesTestUser_AndRunsNothingMore()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        using var stop = new CancellationTokenSource();
        // Ctrl+C while the template goes in (the test user exists by then).
        fake.AnswerNext(UploadTemplate, command =>
        {
            stop.Cancel();
            return new Frame(fake.DeviceId, UploadTemplate + 0x80, FakeAnvizDevice.AckSuccess, []).ToBytes();
        });
        var writer = new StringWriter();

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: true, time: true, stop.Token);

        TestContext.Current.TestOutputHelper!.WriteLine(writer.ToString());
        Assert.NotEqual(0, code);
        Assert.Contains("stopped", writer.ToString());
        Assert.DoesNotContain(fake.Received, f => f.Code is SetDateTime or EnrollFingerprint);
        Assert.Contains(fake.Received, f => f.Code == DeleteUser);
        Assert.DoesNotContain(fake.Users, u => u.UserCode == 8);
        AssertOnlySafeCommands(fake);
    }

    [Fact]
    public async Task RemovalFails_LastLineWarnsTheUserRemains()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        byte[] Refuse(Frame command) => new Frame(fake.DeviceId, DeleteUser + 0x80, FakeAnvizDevice.AckFail, []).ToBytes();
        fake.AnswerNext(DeleteUser, command =>
        {
            fake.AnswerNext(DeleteUser, Refuse); // the second try fails too
            return Refuse(command);
        });
        var writer = new StringWriter();

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: false, time: false, TestContext.Current.CancellationToken);

        TestContext.Current.TestOutputHelper!.WriteLine(writer.ToString());
        Assert.NotEqual(0, code);
        Assert.Contains(fake.Users, u => u.UserCode == 8);
        Assert.StartsWith("WARNING: test user id 8 REMAINS on the device", writer.ToString().TrimEnd().Split('\n')[^1]);
    }

    [Fact]
    public async Task IdFreeCheck_ErrorOtherThanNoUser_Refuses_AndWritesNothing()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        // The FP1 read of id 8 answers ACK_FAIL: not "no such user", so the id is not shown free.
        fake.AnswerNext(DownloadTemplate, command =>
            new Frame(fake.DeviceId, DownloadTemplate + 0x80, FakeAnvizDevice.AckFail, []).ToBytes());
        var writer = new StringWriter();

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: false, time: false, TestContext.Current.CancellationToken);

        TestContext.Current.TestOutputHelper!.WriteLine(writer.ToString());
        Assert.NotEqual(0, code);
        Assert.Contains("REFUSED", writer.ToString());
        Assert.DoesNotContain(fake.Received, f => f.Code is UploadStaff or UploadTemplate or SetDateTime or EnrollFingerprint or DeleteUser);
    }

    [Fact]
    public async Task Remove_TakesOnlyTheTestUser()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        fake.Users.Add(new FakeUser(8, M7Check.TestUserName));
        var refused = new StringWriter();
        var removed = new StringWriter();

        var refusedCode = await M7Check.RemoveAsync("127.0.0.1", fake.Port, 7, refused);
        var removedCode = await M7Check.RemoveAsync("127.0.0.1", fake.Port, 8, removed);

        TestContext.Current.TestOutputHelper!.WriteLine(refused + removed.ToString());
        Assert.NotEqual(0, refusedCode);
        Assert.Contains("REFUSED", refused.ToString());
        Assert.Equal(0, removedCode);
        Assert.Equal([1UL, 7, 3], fake.Users.Select(u => u.UserCode)); // Ben (7) kept, the test user gone
        Assert.Single(fake.Received, f => f.Code == DeleteUser);
        AssertOnlySafeCommands(fake);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("70000")]
    public async Task Main_PortOutOfRange_Usage(string port)
    {
        Assert.Equal(2, await M7Check.Main(["127.0.0.1", port]));
    }

    [Fact]
    public async Task Relay_ForwardsAFrameSplitAcrossReads_Whole()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        await using var relay = new FrameRelay("127.0.0.1", fake.Port);
        using var device = await new AnvizManager().Connect("127.0.0.1", relay.Port);
        relay.TakeFrames();
        fake.SplitNextAnswer(1, 6, 9, 12);

        Assert.Equal(fake.SerialNumber, await device.GetDeviceSN());

        var lines = relay.TakeFrames();
        Assert.Equal(2, lines.Count); // the command and the answer, each one whole line
        Assert.StartsWith(">> A5", lines[0]);
        Assert.StartsWith("<< A5", lines[1]);
    }

    [Theory]
    [InlineData("bad first byte")]
    [InlineData("short")]
    public async Task Relay_BadFrameFromTheDevice_NotForwarded_Closed_Logged(string kind)
    {
        var token = TestContext.Current.CancellationToken;
        var listener = new TcpListener(IPAddress.Loopback, 0); // a stand-in device on 127.0.0.1
        listener.Start();
        try
        {
            var answer = new Frame(1, GetDeviceTypeCode + 0x80, FakeAnvizDevice.AckSuccess, new byte[8]).ToBytes();
            byte[] bad = kind == "short" ? answer[..12] : [0x5A, .. answer[1..]];
            var deviceSide = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync(token);
                var stream = client.GetStream();
                await stream.ReadExactlyAsync(new byte[10], token); // the 0x48 command
                await stream.WriteAsync(bad, token);
            }, token); // then closes
            await using var relay = new FrameRelay("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port);
            using var pc = new TcpClient();
            await pc.ConnectAsync(IPAddress.Loopback, relay.Port, token);
            var pcStream = pc.GetStream();
            await pcStream.WriteAsync(new Frame(1, GetDeviceTypeCode, null, []).ToBytes(), token);

            var got = new MemoryStream();
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);
            limit.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await pcStream.CopyToAsync(got, limit.Token); // ends when the relay closes the connection
            }
            catch (IOException) { } // closed by a reset: closed as well

            Assert.False(limit.IsCancellationRequested, "the relay kept the connection open");
            Assert.Equal(0, got.Length); // nothing forwarded
            await deviceSide;
            Assert.Contains(relay.TakeFrames(), line => line.StartsWith("BAD FRAME"));
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task Relay_UnexpectedErrorInAConnection_IsLogged_AndDisposeDoesNotFault()
    {
        // Port 70000 does not exist: the device connect throws ArgumentOutOfRangeException inside the relay.
        var relay = new FrameRelay("127.0.0.1", 70000);
        await Assert.ThrowsAnyAsync<Exception>(() => new AnvizManager().Connect("127.0.0.1", relay.Port));

        await relay.DisposeAsync();

        Assert.Contains(relay.TakeFrames(), line => line.StartsWith("RELAY ERROR"));
    }

    [Fact]
    public async Task FailingStep_StillRemovesTestUser()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        fake.AnswerNext(UploadTemplate, command => new Frame(fake.DeviceId, UploadTemplate + 0x80, FakeAnvizDevice.AckFail, []).ToBytes());
        var writer = new StringWriter();

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: false, time: false, TestContext.Current.CancellationToken);

        TestContext.Current.TestOutputHelper!.WriteLine(writer.ToString());
        Assert.NotEqual(0, code);
        Assert.Contains("  FAIL:", writer.ToString());
        Assert.DoesNotContain(fake.Users, u => u.UserCode == 8);
        Assert.Contains(fake.Received, f => f.Code == DeleteUser);
        AssertOnlySafeCommands(fake);
    }

    [Fact]
    public async Task NextIdAlreadyOnTheDevice_Refuses_AndWritesNothing()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        fake.Templates[(8, 1)] = new byte[338]; // id 8 holds a fingerprint though no staff record lists it
        var writer = new StringWriter();

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: true, time: true, TestContext.Current.CancellationToken);

        TestContext.Current.TestOutputHelper!.WriteLine(writer.ToString());
        Assert.NotEqual(0, code);
        Assert.Contains("REFUSED", writer.ToString());
        Assert.DoesNotContain(fake.Received, f => f.Code is UploadStaff or UploadTemplate or SetDateTime or EnrollFingerprint or DeleteUser);
        AssertOnlySafeCommands(fake);
    }

    [Fact]
    public async Task UserListShorterThanTheDeviceCount_Refuses_AndWritesNothing()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        // 0x3C says 4 users; the list has 3: the highest id may not be the device's highest.
        fake.AnswerNext(GetRecordInformation, command =>
            new Frame(fake.DeviceId, GetRecordInformation + 0x80, FakeAnvizDevice.AckSuccess, [0, 0, 4, .. new byte[15]]).ToBytes());
        var writer = new StringWriter();

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: false, time: false, TestContext.Current.CancellationToken);

        TestContext.Current.TestOutputHelper!.WriteLine(writer.ToString());
        Assert.NotEqual(0, code);
        Assert.Contains("REFUSED", writer.ToString());
        Assert.DoesNotContain(fake.Received, f => f.Code is UploadStaff or UploadTemplate or SetDateTime or EnrollFingerprint or DeleteUser);
    }

    [Theory]
    [InlineData("clear records")]
    [InlineData("delete all users")]
    [InlineData("reboot")]
    [InlineData("new records")]
    [InlineData("another user")]
    public async Task Relay_BlocksAForbiddenFrame_BeforeTheDeviceSeesIt(string call)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        TestHelpers.AddRecords(fake, 1, _ => true); // a new record, so DownloadRecords(onlyNew: true) sends its 0x40
        await using var relay = new FrameRelay("127.0.0.1", fake.Port) { TestUserId = 8 };
        using var device = await new AnvizManager().Connect("127.0.0.1", relay.Port);

        Task send = call switch
        {
            "clear records" => device.DeleteAllRecords(),
            "delete all users" => device.DeleteEmployeesData(),
            "reboot" => device.RebootDevice(),
            "new records" => device.DownloadRecords(onlyNew: true),
            _ => device.SetEmployeesDataUnicode(new UserInfo(9, "X")),
        };

        await Assert.ThrowsAsync<DeviceConnectionLostException>(() => send);
        Assert.Contains(relay.TakeFrames(), line => line.StartsWith("BLOCKED"));
        // Only the connect's 0x48 (and DownloadRecords' 0x3C count) got through.
        Assert.All(fake.Received, f => Assert.Contains(f.Code, new[] { GetDeviceTypeCode, GetRecordInformation }));
    }
}
