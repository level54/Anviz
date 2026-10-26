using System.Text;
using Anviz.SDK.Exceptions;
using Anviz.SDK.Responses;
using Anviz.SDK.Tests.Fake;
using Anviz.SDK.Utils;
using static Anviz.SDK.Tests.Fake.CommandCode;
using static Anviz.SDK.Tests.TestHelpers;

namespace Anviz.SDK.Tests;

/// <summary>
/// Today's behaviour of the SDK calls, events and fields Chronos uses, against the fake device: the frame the fake
/// received (code and data as the document lays them out) and the value the call returns. A name ending _Today pins
/// code that differs from the document; the comment on that line says what the document says.
/// </summary>
public class ChronosCallsTests
{
    private const ulong Password = 1234, Card = 99887766;
    private static readonly byte[] CardBytes = [0x05, 0xF4, 0x2A, 0x96]; // 99887766, high byte first
    private static readonly byte[] PasswordBytes = [0x40, 0x04, 0xD2];  // length 4 in the high nibble, then 1234

    private static byte[] Template(byte seed) => Enumerable.Range(0, 338).Select(i => (byte)(i * seed)).ToArray();

    /// <summary>The fake's own answer to 0x48: "M7A+-N" zero-padded to 8 bytes.</summary>
    private static Frame TypeCodeAnswer(FakeAnvizDevice fake) =>
        new(fake.DeviceId, GetDeviceTypeCode + 0x80, FakeAnvizDevice.AckSuccess, Encoding.ASCII.GetBytes("M7A+-N\0\0"));

    [Fact]
    public async Task GetDeviceTypeCode_ReadsTypeCode()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        Assert.Equal("M7A+-N", await device.GetDeviceTypeCode());
        AssertFrame(GetDeviceTypeCode, [], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task GetDownloadInformation_ReadsSixAmounts()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.AddRange([
            new FakeUser(1, "A") { Password = Password },
            new FakeUser(2, "B") { Card = Card },
            new FakeUser(3, "C") { Password = 1, Card = 2 },
            new FakeUser(4, "D"),
        ]);
        fake.Templates[(1, 1)] = Template(1);
        fake.Templates[(1, 2)] = Template(2);
        fake.Templates[(3, 1)] = Template(3);
        AddRecords(fake, 7, i => i < 2);
        using var device = await fake.ConnectAsync();

        var info = await device.GetDownloadInformation();

        Assert.Equal(
            (4UL, 3UL, 2UL, 2UL, 7UL, 2UL),
            (info.UserAmount, info.FingerPrintAmount, info.PasswordAmount, info.CardAmount, info.AllRecordAmount, info.NewRecordAmount));
        AssertFrame(GetRecordInformation, [], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task DownloadRecords_All_Reads60RecordsIn3Pages_RaisingRecordDownloaded_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        var start = new DateTime(2026, 10, 25, 7, 0, 0);
        for (var i = 0; i < 60; i++) fake.Records.Add(new FakeRecord((ulong)(1000 + i), start.AddMinutes(i * 7), (byte)(i % 4)));
        using var device = await fake.ConnectAsync();
        var events = new List<RecordDownloadedEventArgs>();
        device.RecordDownloaded += (_, e) => events.Add(e);

        var records = await device.DownloadRecords();

        Assert.Collection(AfterConnect(fake),
            f => AssertFrame(GetRecordInformation, [], f),
            f => AssertFrame(DownloadRecords, [0x01, 25], f),
            f => AssertFrame(DownloadRecords, [0x00, 25], f),
            f => AssertFrame(DownloadRecords, [0x00, 10], f));
        Assert.Equal(fake.Records.Select(r => r.UserCode), records.Select(r => r.UserCode));
        Assert.Equal(fake.Records.Select(r => r.RecordType), records.Select(r => r.RecordType));
        // The document counts the seconds "since the year 2000"; today's code counts from 2000-01-02, one day later.
        Assert.Equal(fake.Records.Select(r => r.DateTime.AddDays(1)), records.Select(r => r.DateTime));
        // One event per record, in order: the total, the 1-based count, and the record the list holds.
        Assert.Equal(
            records.Select((r, i) => (60, i + 1, r.UserCode, r.DateTime, r.RecordType)),
            events.Select(e => (e.TotalRecords, e.CurrentRecord, e.Record.UserCode, e.Record.DateTime, e.Record.RecordType)));
    }

    [Fact]
    public async Task GetEmployeesData_Reads20UsersIn3Pages_RaisingEmployeesDataGet()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        for (var i = 1; i <= 20; i++) fake.Users.Add(new FakeUser((ulong)i, $"Jürgen Ñ{i:00}") { Department = (byte)(i % 5 + 1) });
        fake.Users[2] = fake.Users[2] with { Password = Password };
        fake.Users[4] = fake.Users[4] with { Card = Card };
        fake.Templates[(3, 1)] = Template(1);
        fake.Templates[(4, 2)] = Template(2);
        fake.Templates[(5, 1)] = Template(3);
        fake.Templates[(5, 2)] = Template(4);
        using var device = await fake.ConnectAsync();
        var events = new List<EmployeesDataGetEventArgs>();
        device.EmployeesDataGet += (_, e) => events.Add(e);

        var users = await device.GetEmployeesData();

        Assert.Collection(AfterConnect(fake),
            f => AssertFrame(GetRecordInformation, [], f),
            f => AssertFrame(DownloadStaff, [0x01, 8], f),
            f => AssertFrame(DownloadStaff, [0x00, 8], f),
            f => AssertFrame(DownloadStaff, [0x00, 4], f));
        // FP enroll state bit 0 (FP1) reads as RightThumb, bit 1 (FP2) as RightIndex.
        string Fingers(ulong id) => string.Join(",", new[] { (1, Finger.RightThumb), (2, Finger.RightIndex) }
            .Where(f => fake.Templates.ContainsKey((id, (byte)f.Item1))).Select(f => f.Item2));
        Assert.Equal(
            fake.Users.Select(u => (u.UserCode, u.Name, u.Password, u.Card, u.Department, Fingers(u.UserCode))),
            users.Select(u => (u.Id, u.Name, u.Password, u.Card, u.Department, string.Join(",", u.EnrolledFingerprints))));
        // One event per user, in order: the total, the 1-based count, and the very UserInfo the list holds.
        Assert.Equal(Enumerable.Range(1, 20).Select(i => (20, i)), events.Select(e => (e.TotalEmployees, e.CurrentEmployee)));
        Assert.Equal(users, events.Select(e => e.Employee));
    }

    [Fact]
    public async Task GetEmployeesData_CancelStopsAfterThatPage()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        for (var i = 1; i <= 20; i++) fake.Users.Add(new FakeUser((ulong)i, $"User {i:00}"));
        using var device = await fake.ConnectAsync();
        device.EmployeesDataGet += (_, e) => e.Cancel = e.CurrentEmployee == 3;

        var users = await device.GetEmployeesData();

        Assert.Collection(AfterConnect(fake),
            f => AssertFrame(GetRecordInformation, [], f),
            f => AssertFrame(DownloadStaff, [0x01, 8], f));
        Assert.Equal(Enumerable.Range(1, 8).Select(i => (ulong)i), users.Select(u => u.Id));
    }

    [Fact]
    public async Task GetEmployeesData_PasswordAbove20Bits_DropsHigh8Bits_Today()
    {
        const ulong LongPassword = 12345678; // 0xBC614E: the low 20 bits are 0xC614E, the high 8 bits 0x0B
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(1, "A") { Password = LongPassword });
        using var device = await fake.ConnectAsync();

        var user = Assert.Single(await device.GetEmployeesData());

        // The document: "The low 20bits of password is saved in Byte 6-8, high 8 bits saved in Byte28" (byte 38 in the
        // Unicode record). Today Password reads only the low 20 bits; the high 8 bits are left in PWDH8.
        Assert.Equal(LongPassword & 0xFFFFF, user.Password);
        Assert.Equal(0x0B, user.PWDH8);
    }

    [Theory]
    [InlineData(40, "Jürgen Ñ")]
    [InlineData(30, "Sipho ")]
    public async Task SetEmployeesData_SendsRecords_Today(int recordLength, string name)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var users = Enumerable.Range(1, 10).Select(i => new UserInfo((ulong)i, $"{name}{i:00}")).ToList();
        users[0].Password = Password;
        users[0].Card = Card;
        var expected = users.Select(u => (u.Id, u.Name)).ToList();

        await (recordLength == 40 ? device.SetEmployeesDataUnicode(users) : device.SetEmployeesDataAnsi(users));

        var frames = AfterConnect(fake);
        Assert.All(frames, f => Assert.Equal(UploadStaff, f.Code));
        Assert.Equal([8, 2], frames.Select(f => (int)f.Data[0]));
        Assert.Equal([1 + 8 * recordLength, 1 + 2 * recordLength], frames.Select(f => f.Data.Length));
        Assert.Equal(expected, fake.Users.Select(u => (u.UserCode, u.Name)));
        var first = frames[0].Data[1..(1 + recordLength)];
        Assert.Equal(PasswordBytes, first[5..8]);
        Assert.Equal(CardBytes, first[8..12]);
        // The document puts the password's high 8 bits in byte 38 (Unicode) / 28 (ANSI): 0x00 for 1234.
        Assert.Equal(0xFF, first[recordLength - 3]);
        // The document sets bit 4 of the last byte for a 32-bit card ID (99887766 needs 27 bits).
        Assert.Equal(0x40, first[recordLength - 1]);
        Assert.Empty(users); // Today the call empties the caller's list.
    }

    // R73.11: the M7 answers the flag 0x0001 for a new and an updated user (S6), as the document says; a 0 bit is a refusal.
    [Fact]
    public async Task R73_11_Unicode_OneUserOfThreeNotStored_IsAnvizException_AndTheConnectionStays()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        AnswerUploadStaffWithFlag(fake, 0b101); // user 2 of the frame not stored

        var error = await Assert.ThrowsAsync<AnvizException>(() =>
            device.SetEmployeesDataUnicode([new UserInfo(7, "A"), new UserInfo(8, "B"), new UserInfo(9, "C")]));

        Assert.Equal(AnvizReturnCode.Fail, error.ReturnCode);
        Assert.NotNull(await device.GetDeviceTypeCode());
    }

    [Fact]
    public async Task R73_11_Unicode_AnswerWithoutTheFlag_IsTakenAsStored()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        fake.AnswerNext(UploadStaff, _ => new Frame(fake.DeviceId, UploadStaff + 0x80, FakeAnvizDevice.AckSuccess, []).ToBytes());

        await device.SetEmployeesDataUnicode(new UserInfo(7, "A"));
    }

    [Fact]
    public async Task R73_13_Ansi_UserNotStored_IsStillIgnored()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        AnswerUploadStaffWithFlag(fake, 0);

        await device.SetEmployeesDataAnsi(new UserInfo(7, "A")); // the M5 path is unchanged (D8; the M7 cannot run it)
    }

    private static void AnswerUploadStaffWithFlag(FakeAnvizDevice fake, int flag) =>
        fake.AnswerNext(UploadStaff, _ => new Frame(fake.DeviceId, UploadStaff + 0x80, FakeAnvizDevice.AckSuccess, [(byte)(flag >> 8), (byte)flag]).ToBytes());

    [Fact]
    public async Task DeleteEmployeesData_SendsUserIdAndFF()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(123456, "A"));
        fake.Templates[(123456, 1)] = Template(1);
        using var device = await fake.ConnectAsync();

        await device.DeleteEmployeesData(123456);

        AssertFrame(DeleteUser, [0x00, 0x00, 0x01, 0xE2, 0x40, 0xFF], Assert.Single(AfterConnect(fake)));
        Assert.Empty(fake.Users);
        Assert.Empty(fake.Templates);
    }

    [Theory]
    [InlineData(VerificationMethod.RightThumb, 1)]
    [InlineData(VerificationMethod.RightIndex, 2)]
    public async Task DeleteEmployeesData_OneFinger_SendsItsBackupCode(VerificationMethod method, byte backupCode)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(42, "A"));
        fake.Templates[(42, 1)] = Template(1);
        fake.Templates[(42, 2)] = Template(2);
        using var device = await fake.ConnectAsync();

        await device.DeleteEmployeesData(42, method);

        AssertFrame(DeleteUser, [0, 0, 0, 0, 42, backupCode], Assert.Single(AfterConnect(fake)));
        Assert.Single(fake.Users);
        // Backup code bit 0 is FP1, bit 1 FP2: the other finger's template stays.
        Assert.Equal([(42UL, (byte)(3 - backupCode))], fake.Templates.Keys.Select(k => (k.UserCode, k.BackupCode)));
    }

    [Fact]
    public async Task DeleteEmployeesData_All_Sends6Bytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(1, "A"));
        using var device = await fake.ConnectAsync();

        var error = await Assert.ThrowsAsync<AnvizException>(() => device.DeleteEmployeesData());

        // The document's 0x4D has no data (LEN 0x00 0x00); today's code sends 6 bytes, a frame error the fake answers
        // with ACK_FAIL (what a real device answers is not known here).
        AssertFrame(InitializeUserArea, [0, 0, 0, 0, 0, 0xFF], Assert.Single(AfterConnect(fake)));
        Assert.Contains("0x4D has 6 data bytes", Assert.Single(fake.TakeFrameErrors()));
        Assert.Equal(AnvizReturnCode.Fail, error.ReturnCode);
        Assert.Single(fake.Users);
    }

    [Fact]
    public async Task DeleteAllRecords_SendsMode0()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRecords(fake, 5);
        using var device = await fake.ConnectAsync();

        await device.DeleteAllRecords();

        AssertFrame(ClearRecords, [0x00, 0x00, 0x00, 0x00], Assert.Single(AfterConnect(fake)));
        Assert.Empty(fake.Records);
    }

    [Fact]
    public async Task SetDateTime_Sends6Bytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        await device.SetDateTime(new DateTime(2026, 10, 25, 14, 30, 5));

        AssertFrame(SetDateTime, [26, 10, 25, 14, 30, 5], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task GetFingerprintTemplate_Returns338Bytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Templates[(7, 2)] = Template(5);
        using var device = await fake.ConnectAsync();

        var template = await device.GetFingerprintTemplate(7, Finger.RightIndex);

        AssertFrame(DownloadTemplate, [0, 0, 0, 0, 7, 2], Assert.Single(AfterConnect(fake)));
        Assert.Equal(fake.Templates[(7, 2)], template);
    }

    [Fact]
    public async Task GetFingerprintTemplate_NoUser_Throws_AndTheConnectionStays()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        var error = await Assert.ThrowsAsync<AnvizException>(() => device.GetFingerprintTemplate(7, Finger.RightThumb));

        Assert.Equal(AnvizReturnCode.NoUser, error.ReturnCode);
        // RF.10 / R44.5: the error answer is read to its end (LEN 0, then CRC), so the next command still works.
        Assert.Equal("M7A+-N", await device.GetDeviceTypeCode());
    }

    [Fact]
    public async Task SetFingerprintTemplate_Sends344Bytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var template = Template(3);

        await device.SetFingerprintTemplate(7, Finger.RightThumb, template);

        AssertFrame(UploadTemplate, [0, 0, 0, 0, 7, 1, .. template], Assert.Single(AfterConnect(fake)));
        Assert.Equal(template, fake.Templates[(7, 1)]);
    }

    [Fact]
    public async Task EnrollFingerprint_Sends2Captures_ThenReadsTemplate()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(9, "A"));
        using var device = await fake.ConnectAsync();

        var template = await device.EnrollFingerprint(9);

        Assert.Collection(AfterConnect(fake),
            f => AssertFrame(EnrollFingerprint, [0, 0, 0, 0, 9, 1, 0], f),
            f => AssertFrame(EnrollFingerprint, [0, 0, 0, 0, 9, 1, 1], f),
            f => AssertFrame(DownloadTemplate, [0, 0, 0, 0, 9, 1], f));
        Assert.Equal(fake.Templates[(9, 1)], template);
    }

    [Fact]
    public async Task ReceivedPacket_AnswerNotWaitedFor_RaisesIt_AndTheCallGoesOn()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var packets = new List<Response>();
        device.ReceivedPacket += (_, e) => packets.Add(e);
        byte[] information = [.. Enumerable.Range(1, 18).Select(i => (byte)i)];
        // Before its answer to 0x48, the device sends a 0x3C answer the SDK did not ask for.
        fake.AnswerNext(GetDeviceTypeCode, command =>
        [
            .. new Frame(fake.DeviceId, GetRecordInformation + 0x80, FakeAnvizDevice.AckSuccess, information).ToBytes(),
            .. TypeCodeAnswer(fake).ToBytes(),
        ]);

        Assert.Equal("M7A+-N", await device.GetDeviceTypeCode());

        var packet = Assert.Single(packets);
        Assert.Equal((fake.DeviceId, (byte)(GetRecordInformation + 0x80)), (packet.DeviceID, packet.ResponseCode));
        Assert.Equal(information, packet.DATA);
    }

    [Fact]
    public async Task DeviceError_BadCrc_RaisesIt_AndClosesTheConnection()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var errors = new List<Exception>();
        device.DeviceError += (_, e) => errors.Add(e);
        fake.AnswerNext(GetDeviceTypeCode, command =>
        {
            var answer = TypeCodeAnswer(fake).ToBytes();
            answer[^1] ^= 0xFF;
            return answer;
        });

        await Assert.ThrowsAsync<DeviceConnectionLostException>(device.GetDeviceTypeCode);

        Assert.Equal("Invalid CRC", Assert.Single(errors).Message);
        await fake.ClientClosed.WaitAsync(Wait, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Dispose_ClosesTheConnection()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        var device = await fake.ConnectAsync();

        device.Dispose();

        await fake.ClientClosed.WaitAsync(Wait, TestContext.Current.CancellationToken);
    }
}
