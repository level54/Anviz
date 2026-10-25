using Anviz.SDK.Responses;
using Anviz.SDK.Tests.Fake;
using Anviz.SDK.Utils;

namespace Anviz.SDK.Tests;

/// <summary>
/// Today's behaviour of the SDK calls Chronos makes, against the fake device: the frame the fake received (code and
/// data as the document lays them out) and the value the call returns. A name ending _Today pins code that differs
/// from the document; the comment on that line says what the document says.
/// </summary>
public class ChronosCallsTests
{
    private const ulong Password = 1234, Card = 99887766;
    private static readonly byte[] CardBytes = [0x05, 0xF4, 0x2A, 0x96]; // 99887766, high byte first
    private static readonly byte[] PasswordBytes = [0x40, 0x04, 0xD2];  // length 4 in the high nibble, then 1234

    private static Task<AnvizDevice> Connect(FakeAnvizDevice fake) => new AnvizManager().Connect("127.0.0.1", fake.Port);

    /// <summary>The frames after the 0x48 that Connect sends.</summary>
    private static Frame[] AfterConnect(FakeAnvizDevice fake) => fake.Received.Skip(1).ToArray();

    private static byte[] Template(byte seed) => Enumerable.Range(0, 338).Select(i => (byte)(i * seed)).ToArray();

    private static void AssertFrame(byte code, byte[] data, Frame frame)
    {
        Assert.Equal(code, frame.Code);
        Assert.Equal(data, frame.Data);
    }

    [Fact]
    public async Task GetDeviceTypeCode_ReadsTypeCode()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await Connect(fake);

        Assert.Equal("M7A+-N", await device.GetDeviceTypeCode());
        AssertFrame(0x48, [], Assert.Single(AfterConnect(fake)));
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
        for (var i = 0; i < 7; i++) fake.Records.Add(new FakeRecord(1, new DateTime(2026, 10, 25), 0) { New = i < 2 });
        using var device = await Connect(fake);

        var info = await device.GetDownloadInformation();

        Assert.Equal(
            (4UL, 3UL, 2UL, 2UL, 7UL, 2UL),
            (info.UserAmount, info.FingerPrintAmount, info.PasswordAmount, info.CardAmount, info.AllRecordAmount, info.NewRecordAmount));
        AssertFrame(0x3C, [], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task DownloadRecords_All_Reads60RecordsIn3Pages_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        var start = new DateTime(2026, 10, 25, 7, 0, 0);
        for (var i = 0; i < 60; i++) fake.Records.Add(new FakeRecord((ulong)(1000 + i), start.AddMinutes(i * 7), (byte)(i % 4)));
        using var device = await Connect(fake);

        var records = await device.DownloadRecords();

        Assert.Collection(AfterConnect(fake),
            f => AssertFrame(0x3C, [], f),
            f => AssertFrame(0x40, [0x01, 25], f),
            f => AssertFrame(0x40, [0x00, 25], f),
            f => AssertFrame(0x40, [0x00, 10], f));
        Assert.Equal(60, records.Count);
        Assert.Equal(fake.Records.Select(r => r.UserCode), records.Select(r => r.UserCode));
        Assert.Equal(fake.Records.Select(r => r.RecordType), records.Select(r => r.RecordType));
        // The document counts the seconds "since the year 2000"; today's code counts from 2000-01-02, one day later.
        Assert.Equal(fake.Records.Select(r => r.DateTime.AddDays(1)), records.Select(r => r.DateTime));
    }

    [Fact]
    public async Task GetEmployeesData_Reads20UsersIn3Pages()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        for (var i = 1; i <= 20; i++) fake.Users.Add(new FakeUser((ulong)i, $"Jürgen Ñ{i:00}"));
        fake.Users[2] = fake.Users[2] with { Password = Password };
        fake.Users[4] = fake.Users[4] with { Card = Card };
        using var device = await Connect(fake);

        var users = await device.GetEmployeesData();

        Assert.Collection(AfterConnect(fake),
            f => AssertFrame(0x3C, [], f),
            f => AssertFrame(0x72, [0x01, 8], f),
            f => AssertFrame(0x72, [0x00, 8], f),
            f => AssertFrame(0x72, [0x00, 4], f));
        Assert.Equal(
            fake.Users.Select(u => (u.UserCode, u.Name, u.Password, u.Card)),
            users.Select(u => (u.Id, u.Name, u.Password, u.Card)));
    }

    [Fact]
    public async Task SetEmployeesDataUnicode_Sends40ByteRecords_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await Connect(fake);
        var users = Enumerable.Range(1, 10).Select(i => new UserInfo((ulong)i, $"Jürgen Ñ{i:00}")).ToList();
        users[0].Password = Password;
        users[0].Card = Card;
        var expected = users.Select(u => (u.Id, u.Name)).ToList();

        await device.SetEmployeesDataUnicode(users);

        var frames = AfterConnect(fake);
        Assert.All(frames, f => Assert.Equal(0x73, f.Code));
        Assert.Equal([8, 2], frames.Select(f => (int)f.Data[0]));
        Assert.Equal([1 + 8 * 40, 1 + 2 * 40], frames.Select(f => f.Data.Length));
        Assert.Equal(expected, fake.Users.Select(u => (u.UserCode, u.Name)));
        var first = frames[0].Data[1..41];
        Assert.Equal(PasswordBytes, first[5..8]);
        Assert.Equal(CardBytes, first[8..12]);
        Assert.Equal(0xFF, first[37]); // The document puts the password's high 8 bits in byte 38: 0x00 for 1234.
        Assert.Equal(0x40, first[39]); // The document sets bit 4 of byte 40 for a 32-bit card ID (99887766 needs 27 bits).
        Assert.Empty(users); // Today the call empties the caller's list.
    }

    [Fact]
    public async Task SetEmployeesDataAnsi_Sends30ByteRecords_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await Connect(fake);
        var users = Enumerable.Range(1, 10).Select(i => new UserInfo((ulong)i, $"Sipho {i:00}")).ToList();
        users[0].Password = Password;
        users[0].Card = Card;
        var expected = users.Select(u => (u.Id, u.Name)).ToList();

        await device.SetEmployeesDataAnsi(users);

        var frames = AfterConnect(fake);
        Assert.All(frames, f => Assert.Equal(0x73, f.Code));
        Assert.Equal([8, 2], frames.Select(f => (int)f.Data[0]));
        Assert.Equal([1 + 8 * 30, 1 + 2 * 30], frames.Select(f => f.Data.Length));
        Assert.Equal(expected, fake.Users.Select(u => (u.UserCode, u.Name)));
        var first = frames[0].Data[1..31];
        Assert.Equal(PasswordBytes, first[5..8]);
        Assert.Equal(CardBytes, first[8..12]);
        Assert.Equal(0xFF, first[27]); // The document puts the password's high 8 bits in byte 28: 0x00 for 1234.
        Assert.Equal(0x40, first[29]); // The document sets bit 4 of byte 30 for a 32-bit card ID (99887766 needs 27 bits).
        Assert.Empty(users); // Today the call empties the caller's list.
    }

    [Fact]
    public async Task DeleteEmployeesData_SendsUserIdAndFF()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(123456, "A"));
        fake.Templates[(123456, 1)] = Template(1);
        using var device = await Connect(fake);

        await device.DeleteEmployeesData(123456);

        AssertFrame(0x4C, [0x00, 0x00, 0x01, 0xE2, 0x40, 0xFF], Assert.Single(AfterConnect(fake)));
        Assert.Empty(fake.Users);
        Assert.Empty(fake.Templates);
    }

    [Fact]
    public async Task DeleteEmployeesData_RightIndex_SendsBackupCode2()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(42, "A"));
        fake.Templates[(42, 1)] = Template(1);
        fake.Templates[(42, 2)] = Template(2);
        using var device = await Connect(fake);

        await device.DeleteEmployeesData(42, VerificationMethod.RightIndex);

        AssertFrame(0x4C, [0, 0, 0, 0, 42, 0x02], Assert.Single(AfterConnect(fake)));
        Assert.Single(fake.Users);
        Assert.Equal([(42UL, (byte)1)], fake.Templates.Keys.Select(k => (k.UserCode, k.BackupCode)));
    }

    [Fact]
    public async Task DeleteEmployeesData_All_Sends6Bytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(1, "A"));
        using var device = await Connect(fake);

        await device.DeleteEmployeesData();

        // The document's 0x4D has no data (LEN 0x00 0x00); today's code sends 6 bytes.
        AssertFrame(0x4D, [0, 0, 0, 0, 0, 0xFF], Assert.Single(AfterConnect(fake)));
        Assert.Empty(fake.Users);
    }

    [Fact]
    public async Task DeleteAllRecords_SendsMode0()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        for (var i = 0; i < 5; i++) fake.Records.Add(new FakeRecord(1, new DateTime(2026, 10, 25), 0));
        using var device = await Connect(fake);

        await device.DeleteAllRecords();

        AssertFrame(0x4E, [0x00, 0x00, 0x00, 0x00], Assert.Single(AfterConnect(fake)));
        Assert.Empty(fake.Records);
    }

    [Fact]
    public async Task SetDateTime_Sends6Bytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await Connect(fake);

        await device.SetDateTime(new DateTime(2026, 10, 25, 14, 30, 5));

        AssertFrame(0x39, [26, 10, 25, 14, 30, 5], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task GetFingerprintTemplate_Returns338Bytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Templates[(7, 2)] = Template(5);
        using var device = await Connect(fake);

        var template = await device.GetFingerprintTemplate(7, Finger.RightIndex);

        AssertFrame(0x44, [0, 0, 0, 0, 7, 2], Assert.Single(AfterConnect(fake)));
        Assert.Equal(338, template.Length);
        Assert.Equal(fake.Templates[(7, 2)], template);
    }

    [Fact]
    public async Task SetFingerprintTemplate_Sends344Bytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await Connect(fake);
        var template = Template(3);

        await device.SetFingerprintTemplate(7, Finger.RightThumb, template);

        var frame = Assert.Single(AfterConnect(fake));
        AssertFrame(0x45, [0, 0, 0, 0, 7, 1, .. template], frame);
        Assert.Equal(344, frame.Data.Length);
        Assert.Equal(template, fake.Templates[(7, 1)]);
    }

    [Fact]
    public async Task EnrollFingerprint_Sends2Captures_ThenReadsTemplate()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(9, "A"));
        using var device = await Connect(fake);

        var template = await device.EnrollFingerprint(9);

        Assert.Collection(AfterConnect(fake),
            f => AssertFrame(0x5C, [0, 0, 0, 0, 9, 1, 0], f),
            f => AssertFrame(0x5C, [0, 0, 0, 0, 9, 1, 1], f),
            f => AssertFrame(0x44, [0, 0, 0, 0, 9, 1], f));
        Assert.Equal(338, template.Length);
        Assert.Equal(fake.Templates[(9, 1)], template);
    }
}
