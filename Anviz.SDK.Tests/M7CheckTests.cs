using Anviz.SDK.Check;
using Anviz.SDK.Exceptions;
using Anviz.SDK.Responses;
using Anviz.SDK.Tests.Fake;
using static Anviz.SDK.Tests.Fake.CommandCode;

namespace Anviz.SDK.Tests;

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

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: true, time: true);

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
        Assert.DoesNotContain("  FAIL:", writer.ToString()); // the verdict lines
    }

    [Fact]
    public async Task FailingStep_StillRemovesTestUser()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRealUsers(fake);
        fake.AnswerNext(UploadTemplate, command => new Frame(fake.DeviceId, UploadTemplate + 0x80, FakeAnvizDevice.AckFail, []).ToBytes());
        var writer = new StringWriter();

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: false, time: false);

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

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: true, time: true);

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

        var code = await M7Check.RunAsync("127.0.0.1", fake.Port, writer, enrol: false, time: false);

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
