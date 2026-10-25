using System.Net;
using System.Text;
using Anviz.SDK.Exceptions;
using Anviz.SDK.Responses;
using Anviz.SDK.Tests.Fake;
using Anviz.SDK.Utils;
using static Anviz.SDK.Tests.Fake.CommandCode;
using static Anviz.SDK.Tests.TestHelpers;
using Bell = Anviz.SDK.Responses.ScheduledBell;
using Record = Anviz.SDK.Responses.Record;
using Day = Anviz.SDK.Responses.AnvizTimeZone.DayName;

namespace Anviz.SDK.Tests;

/// <summary>
/// Today's behaviour of every other public AnvizDevice call (the ones Chronos does not make), against the fake device:
/// the frame the fake received (code and data as the document lays them out) and the value the call returns. A name
/// ending _Today pins code that differs from the document, or does what the document does not say; the comment names
/// the review row (Docs/anviz_sdk_review_CHR27-1.md).
/// </summary>
public class OtherCommandsTests
{
    private static byte[] Bytes(int length, byte seed) => Enumerable.Range(0, length).Select(i => (byte)(i * seed + 1)).ToArray();

    /// <summary>A command the fake refuses: ACK_FAIL, and the frame error it kept (taken here, so it does not fail the test).</summary>
    private static async Task AssertRefused(FakeAnvizDevice fake, Func<Task> call, string frameError)
    {
        var error = await Assert.ThrowsAsync<AnvizException>(call);
        Assert.Equal(AnvizReturnCode.Fail, error.ReturnCode);
        Assert.Contains(frameError, Assert.Single(fake.TakeFrameErrors()));
    }

    [Fact]
    public async Task GetBasicSettings_ReadsEighteenBytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        var s = await device.GetBasicSettings();

        AssertFrame(GetBasicSettings, [], Assert.Single(AfterConnect(fake)));
        Assert.Equal(
            ("V2.1.0", 1234UL, (byte)5, Volume.Medium, Language.English, DateFormat.DDMMYY, true, (byte)1, (byte)0x10, (byte)2),
            (s.Firmware, s.ManagementPassword, s.Sleep, s.Volume, s.Language, s.DateFormat, s.Is24HourClock, s.Attendance, s.LangCHG, s.CMDVersion));
    }

    [Theory]
    [InlineData(0x00, (DateFormat)0, true)]          // Chinese, 24 h: no named DateFormat
    [InlineData(0x11, DateFormat.MMDDYY, false)]     // America, 12 h
    [InlineData(0x03, (DateFormat)2, false)]         // time-format bits 1-3 leak into DateFormat
    [InlineData(0x08, DateFormat.YYMMDD, true)]      // 0x08 is not in the document
    public async Task GetBasicSettings_DateTimeFormat_Today(byte format, DateFormat dateFormat, bool is24Hour)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.BasicSettings[14] = format;
        using var device = await fake.ConnectAsync();

        var s = await device.GetBasicSettings();

        // R30.7: the document's byte 15 is bits 7-4 date (0 Chinese, 1 America, 2 English), bits 3-0 time (0 = 24 h, 1 = 12 h).
        Assert.Equal((dateFormat, is24Hour), (s.DateFormat, s.Is24HourClock));
    }

    [Fact]
    public async Task SetBasicSettings_WritesTenBytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var s = await device.GetBasicSettings();
        (s.ManagementPassword, s.Sleep, s.Volume, s.Language, s.DateFormat, s.Is24HourClock, s.Attendance) =
            (56789, 10, Volume.High, Language.French, DateFormat.MMDDYY, false, 2);

        await device.SetBasicSettings(s);

        // Password length 5 in the high nibble, then 56789; America + 12 h = 0x11; the reserved byte 0.
        byte[] sent = [0x50, 0xDD, 0xD5, 10, 4, 3, 0x11, 2, 0x10, 0];
        AssertFrame(SetBasicSettings, sent, AfterConnect(fake)[1]);
        Assert.Equal(sent[..9], fake.BasicSettings[8..17]);
        var again = await device.GetBasicSettings();
        Assert.Equal((56789UL, Volume.High, DateFormat.MMDDYY, false), (again.ManagementPassword, again.Volume, again.DateFormat, again.Is24HourClock));
    }

    [Fact]
    public async Task SetBasicSettings_YYMMDD_Writes08_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var s = await device.GetBasicSettings();
        (s.DateFormat, s.Is24HourClock) = (DateFormat.YYMMDD, true);

        await device.SetBasicSettings(s);

        // R31.4: the document's date bits 7-4 have no 0x08 (0 is Chinese); today's code writes YYMMDD as 0x08.
        Assert.Equal(0x08, AfterConnect(fake)[1].Data[6]);
    }

    [Fact]
    public async Task GetAdvancedSettings_ReadsFifteenBytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        var s = await device.GetAdvancedSettings();

        AssertFrame(GetAdvancedSettings, [], Assert.Single(AfterConnect(fake)));
        Assert.Equal(
            (FPPrecision.Medium, (byte)0x11, (byte)2, true, false, true, RelayMode.Bell, (byte)5),
            (s.FPPrecision, s.WiegandHead, s.WiegandMode, s.WorkCode, s.RealTimeMode, s.FPAutoUpdate, s.RelayMode, s.LockDelay));
        Assert.Equal(
            (500UL, (byte)30, (byte)10, (byte)3, (byte)0),
            (s.MemoryFullAlarm, s.RepeatAttendanceDelay, s.DoorSensorDelay, s.ScheduledBellDelay, s.Reserved));
    }

    [Fact]
    public async Task SetAdvancedSettings_WritesFifteenBytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var s = await device.GetAdvancedSettings();
        (s.FPPrecision, s.RealTimeMode, s.RelayMode, s.MemoryFullAlarm) = (FPPrecision.High, true, RelayMode.Door, 5000);

        await device.SetAdvancedSettings(s);

        byte[] sent = [2, 0x11, 2, 1, 1, 1, 0, 5, 0x00, 0x13, 0x88, 30, 10, 3, 0];
        AssertFrame(SetAdvancedSettings, sent, AfterConnect(fake)[1]);
        Assert.Equal(sent, fake.AdvancedSettings);
    }

    [Fact]
    public async Task GetDateTime_ReadsSixBytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        var clock = await device.GetDateTime();

        AssertFrame(GetDateTime, [], Assert.Single(AfterConnect(fake)));
        // R38.2: the document does not give the year's base; today's code reads 2000 + byte (the fake sends 26).
        Assert.Equal(fake.Clock, clock);
    }

#pragma warning disable CS0612 // GetDeviceID is [Obsolete]
    [Fact]
    public async Task GetDeviceID_ReadsFourBytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.DeviceId = 0x01020304;
        using var device = await fake.ConnectAsync();

        Assert.Equal(0x01020304UL, await device.GetDeviceID());
        AssertFrame(GetDeviceId, [], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task GetDeviceID_TakesTheAnswerAsItsDeviceId_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        fake.AnswerNext(GetDeviceId, _ => new Frame(fake.DeviceId, GetDeviceId + 0x80, FakeAnvizDevice.AckSuccess, [0, 0, 0, 9]).ToBytes());

        Assert.Equal(9UL, await device.GetDeviceID());

        // R46.3: the document's 0x46 is the id set in the device menu (0x74 is the communication id) and does not say
        // it is CH; today's code takes it as CH. Here CH is 1, so the next command goes to device 9 and is lost.
        Assert.Equal(9UL, device.DeviceId);
        await Assert.ThrowsAsync<DeviceConnectionLostException>(() => device.GetDeviceTypeCode());
        Assert.Contains("Frame for device 9", Assert.Single(fake.TakeFrameErrors()));
    }
#pragma warning restore CS0612

    [Theory]
    [InlineData(1UL, 0x01020304UL)]
    [InlineData(0UL, 0UL)]
    public async Task SetDeviceID_SendsFourBytes_TakesTheNewIdUnlessItWas0_Today(ulong idBefore, ulong idAfter)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.DeviceId = idBefore;
        using var device = await fake.ConnectAsync();

        await device.SetDeviceID(0x01020304);

        AssertFrame(SetDeviceId, [1, 2, 3, 4], Assert.Single(AfterConnect(fake)));
        Assert.Equal(0x01020304UL, fake.DeviceId);
        // R47.2: the document's 0x47 sets the menu id (0x75 the communication id); today's code takes it as CH, unless CH was 0.
        Assert.Equal(idAfter, device.DeviceId);
        await device.GetDeviceTypeCode();
        Assert.Equal(idAfter, fake.Received[^1].DeviceId);
    }

    [Fact]
    public async Task GetDeviceSN_Reads16Bytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        // R24.2: the document does not say how the 16 bytes are encoded; today's code reads ASCII, trailing zeros trimmed.
        Assert.Equal("M7A0123456789", await device.GetDeviceSN());
        AssertFrame(GetSerialNumber, [], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task SetDeviceSN_Sends16Bytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        await device.SetDeviceSN("ABC-123");

        // R25.1: the document does not say how the 16 bytes are encoded; today's code writes ASCII, zero-padded.
        AssertFrame(SetSerialNumber, [.. "ABC-123"u8, .. new byte[9]], Assert.Single(AfterConnect(fake)));
        Assert.Equal("ABC-123", fake.SerialNumber);
    }

    [Fact]
    public async Task GetTcpParameters_Reads27Bytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.TcpParameters[26] = 2;
        using var device = await fake.ConnectAsync();

        var p = await device.GetTcpParameters();

        AssertFrame(GetTcpParameters, [], Assert.Single(AfterConnect(fake)));
        // R3A.2: the document does not give the octet order; today's code reads the first octet first.
        Assert.Equal(
            ("192.168.1.218", "255.255.255.0", "001122334455", "192.168.1.1", "192.168.1.10"),
            (p.IP.ToString(), p.SubnetMask.ToString(), p.MacAddress.ToString(), p.DefaultGateway.ToString(), p.Server.ToString()));
        // R3A.7: the document names byte 27 "DHCP limit" without values; today's code reads any non-zero as DHCP on.
        Assert.Equal(((byte)0, 5010UL, TcpMode.Server, true), (p.FarLimit, p.ComPort, p.TcpMode, p.DhcpEnabled));
    }

    [Fact]
    public async Task SetTCPParameters_Writes27Bytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var p = await device.GetTcpParameters();
        (p.IP, p.ComPort, p.TcpMode, p.DhcpEnabled) = (IPAddress.Parse("10.0.0.5"), 5011, TcpMode.Client, true);

        await device.SetTCPParameters(p);

        // R3B.2: the octet order and the DHCP value as R3A.2 / R3A.7 (DHCP on is written as 1).
        byte[] sent = [10, 0, 0, 5, 255, 255, 255, 0, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 192, 168, 1, 1, 192, 168, 1, 10, 0, 0x13, 0x93, 1, 1];
        AssertFrame(SetTcpParameters, sent, AfterConnect(fake)[1]);
        Assert.Equal(sent, fake.TcpParameters);
    }

    [Fact]
    public async Task SetRecords_Sends14Bytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var when = new DateTime(2026, 10, 25, 8, 0, 0);

        await device.SetRecords(new Record(77) { DateTime = when, BackupCode = 2, RecordType = 1, WorkType = 0x010203 });

        // R41.3: the document counts the seconds since 2000; today's code counts from 2000-01-02, so the device
        // (the fake counts from 2000-01-01) stores the clocking one day early.
        var seconds = (ulong)(when - new DateTime(2000, 1, 2)).TotalSeconds;
        AssertFrame(UploadRecord, [0, 0, 0, 0, 77, .. FakeAnvizDevice.BigEndian(seconds, 4), 2, 1, 1, 2, 3], Assert.Single(AfterConnect(fake)));
        var stored = Assert.Single(fake.Records);
        Assert.Equal((77UL, when.AddDays(-1), (byte)1), (stored.UserCode, stored.DateTime, stored.RecordType));
    }

    [Fact]
    public async Task DownloadRecords_OnlyNew_Sends2ThenPages_ReadsOnlyTheNewRecords()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRecords(fake, 40, i => i % 4 != 0); // 30 new among 40
        using var device = await fake.ConnectAsync();

        var records = await device.DownloadRecords(onlyNew: true);

        // R40.1: parameter 2 restarts on the new records, 0 goes on; the amount is the 0x3C new record amount, <= 25 a page.
        Assert.Collection(AfterConnect(fake),
            f => AssertFrame(GetRecordInformation, [], f),
            f => AssertFrame(DownloadRecords, [0x02, 25], f),
            f => AssertFrame(DownloadRecords, [0x00, 5], f));
        Assert.Equal(fake.Records.Where(r => r.New).Select(r => r.UserCode), records.Select(r => r.UserCode));
    }

    [Fact]
    public async Task ClearNewRecords_SendsType1_ClearsEveryNewMark()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRecords(fake, 5, i => i >= 2);
        using var device = await fake.ConnectAsync();

        await device.ClearNewRecords();

        AssertFrame(ClearRecords, [1, 0, 0, 0], Assert.Single(AfterConnect(fake)));
        Assert.Equal(5, fake.Records.Count);
        Assert.DoesNotContain(fake.Records, r => r.New);
    }

    [Fact]
    public async Task ClearNewRecords_Amount_SendsType2AndTheAmount()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRecords(fake, 5, i => i >= 2);
        using var device = await fake.ConnectAsync();

        await device.ClearNewRecords(2);

        AssertFrame(ClearRecords, [2, 0, 0, 2], Assert.Single(AfterConnect(fake)));
        Assert.Equal([false, false, false, false, true], fake.Records.Select(r => r.New));
    }

    [Fact]
    public async Task RebootDevice_Sends2EWith60_NotInTheDocument_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        // R2E.1: the document has no 0x2E (it lists 0x2A-0x2D and 0x2F); the fake refuses it with ACK_FAIL.
        await AssertRefused(fake, () => device.RebootDevice(), "no answer for command 0x2E");
        AssertFrame(0x2E, [0x60], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task ResetToFactorySettings_Sends4F()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        await device.ResetToFactorySettings();

        AssertFrame(InitializeSystem, [], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task UnlockDoor_Sends5E()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        await device.UnlockDoor();

        AssertFrame(OpenLock, [], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task GetFaceTemplate_SendsTheFP1Request_ReturnsTheAnswer_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Templates[(5, 1)] = Bytes(338, 3);
        using var device = await fake.ConnectAsync();

        var template = await device.GetFaceTemplate(5);

        // R44.6 / R44.7: the document has no face template; today's code sends the FP1 template request (0x44, backup
        // code 1) and returns whatever the answer holds, here the 338-byte fingerprint template.
        AssertFrame(DownloadTemplate, [0, 0, 0, 0, 5, 1], Assert.Single(AfterConnect(fake)));
        Assert.Equal(fake.Templates[(5, 1)], template);
    }

    [Fact]
    public async Task SetFaceTemplate_Sends15366Bytes_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var template = Bytes(1000, 7);

        // R45.6 / R45.7: the document's 0x45 is the 344-byte fingerprint upload (DATA at most 400); today's code sends
        // user code, backup code 1 and the template zero-padded to 15,360 bytes. The fake refuses the length.
        await AssertRefused(fake, () => device.SetFaceTemplate(5, template), "0x45 has 15366 data bytes");
        AssertFrame(UploadTemplate, [0, 0, 0, 0, 5, 1, .. template, .. new byte[15360 - 1000]], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task GetTimeZoneInfo_Reads28Bytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.TimeZones[3] = [.. Enumerable.Repeat<byte[]>([8, 0, 17, 30], 5).SelectMany(d => d), 9, 15, 13, 0, 0, 0, 0, 0];
        using var device = await fake.ConnectAsync();

        var zone = await device.GetTimeZoneInfo(3);

        AssertFrame(GetTimeZone, [3], Assert.Single(AfterConnect(fake)));
        Assert.Equal(Enum.GetValues<Day>(), zone.Days.Keys);
        Assert.All([Day.Monday, Day.Tuesday, Day.Wednesday, Day.Thursday, Day.Friday],
            d => Assert.Equal((new TimeSpan(8, 0, 0), new TimeSpan(17, 30, 0)), (zone.Days[d].From, zone.Days[d].To)));
        Assert.Equal((new TimeSpan(9, 15, 0), new TimeSpan(13, 0, 0)), (zone.Days[Day.Saturday].From, zone.Days[Day.Saturday].To));
        Assert.Equal((TimeSpan.Zero, TimeSpan.Zero), (zone.Days[Day.Sunday].From, zone.Days[Day.Sunday].To));
    }

    [Fact]
    public async Task SetTimeZoneInfo_Writes29Bytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.TimeZones[3] = Bytes(28, 0);
        using var device = await fake.ConnectAsync();
        var zone = await device.GetTimeZoneInfo(3);
        zone.Days[Day.Sunday].From = new TimeSpan(10, 15, 0);
        zone.Days[Day.Sunday].To = new TimeSpan(12, 45, 0);

        await device.SetTimeZoneInfo(3, zone);

        byte[] days = [.. Bytes(24, 0), 10, 15, 12, 45];
        AssertFrame(SetTimeZone, [3, .. days], AfterConnect(fake)[1]);
        Assert.Equal(days, fake.TimeZones[3]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task TimeZoneInfo_NumberOutside1To30_ThrowsWithoutSending_Today(byte number)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var zone = await device.GetTimeZoneInfo(1);

        // R50.1 / R51.2: the document gives 30 time zones (0x51: 32) and no first number; today's code allows 1-30.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => device.GetTimeZoneInfo(number));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => device.SetTimeZoneInfo(number, zone));
        AssertFrame(GetTimeZone, [1], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task GetScheduledBells_Reads30Bells_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        byte[] bells = [8, 0, 0b0011_1110, 12, 30, 0b0000_0001, 7, 0, 0xFF];
        bells.CopyTo(fake.Bells, 0);
        using var device = await fake.ConnectAsync();

        var read = await device.GetScheduledBells();

        AssertFrame(GetBells, [], Assert.Single(AfterConnect(fake)));
        Assert.Equal(30, read.Count);
        // Bits 6-1 are Saturday to Monday (the document's example 00111110 is Monday to Friday).
        var weekdays = Bell.DayName.Monday | Bell.DayName.Tuesday | Bell.DayName.Wednesday | Bell.DayName.Thursday | Bell.DayName.Friday;
        Assert.Equal((new TimeSpan(8, 0, 0), weekdays, true), (read[0].Time, read[0].Days, read[0].HasValue));
        // R54.5: the document names neither bit 0 nor an empty slot; today's code reads bit 0 as Sunday, and 0 and 0xFF as no bell.
        Assert.Equal((new TimeSpan(12, 30, 0), Bell.DayName.Sunday, true), (read[1].Time, read[1].Days, read[1].HasValue));
        Assert.False(read[2].HasValue);
        Assert.All(read.Skip(3), b => Assert.False(b.HasValue));
    }

    [Fact]
    public async Task SetScheduledBell_WritesFourBytes()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        await device.SetScheduledBell(2, new Bell { Time = new TimeSpan(16, 45, 0), Days = Bell.DayName.Monday | Bell.DayName.Friday });

        AssertFrame(SetBell, [2, 16, 45, 0b0010_0010], Assert.Single(AfterConnect(fake)));
        Assert.Equal([16, 45, 0b0010_0010], fake.Bells[3..6]);
    }

    [Fact]
    public async Task SetScheduledBell_Number0_IsSent_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var bell = new Bell { Time = new TimeSpan(7, 0, 0), Days = Bell.DayName.Monday };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => device.SetScheduledBell(31, bell));
        // R55.2: the document gives 30 bells and no first number; today's code sends 0-30 (31 numbers). The fake
        // numbers them 1-30, as the time zones, and refuses 0 (a device refusal, not a frame error).
        var error = await Assert.ThrowsAsync<AnvizException>(() => device.SetScheduledBell(0, bell));

        Assert.Equal(AnvizReturnCode.Fail, error.ReturnCode);
        AssertFrame(SetBell, [0, 7, 0, 0b0000_0010], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task InquireCard_AsksAgainWhile0()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        foreach (var card in new uint[] { 0, 0, 99887766 }) fake.Cards.Enqueue(card);
        using var device = await fake.ConnectAsync();

        Assert.Equal(99887766UL, await device.InquireCard());
        Assert.Equal(3, AfterConnect(fake).Length);
        Assert.All(AfterConnect(fake), f => AssertFrame(InquireCard, [], f));
    }

    [Fact]
    public async Task InquireCard_NoCard_Returns0AfterTheRetries()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        Assert.Equal(0UL, await device.InquireCard(retries: 2));
        Assert.Equal(2, AfterConnect(fake).Length);
        Assert.All(AfterConnect(fake), f => AssertFrame(InquireCard, [], f));
    }

    [Fact]
    public async Task SetConnectionPassword_Sends04With24Bytes_NotInTheDocument_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();

        // R04.1 / R04.2: the document has no 0x04 (CrossChex: a sign-in, user 12 + password 12 bytes); today's code
        // sends both ASCII, zero-padded. The fake refuses it with ACK_FAIL.
        await AssertRefused(fake, () => device.SetConnectionPassword("admin", "1234"), "no answer for command 0x04");
        AssertFrame(0x04, [.. "admin"u8, .. new byte[7], .. "1234"u8, .. new byte[8]], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task DeleteEmployeesData_User_SendsItsIdAndFF()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(5, "A"));
        using var device = await fake.ConnectAsync();

        await device.DeleteEmployeesData(new UserInfo(5, "A"));

        AssertFrame(DeleteUser, [0, 0, 0, 0, 5, 0xFF], Assert.Single(AfterConnect(fake)));
        Assert.Empty(fake.Users);
    }

    [Fact]
    public async Task DeleteEmployeesData_RightMiddle_Sends0x10_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.Add(new FakeUser(42, "A"));
        fake.Templates[(42, 1)] = Bytes(338, 1);
        fake.Templates[(42, 2)] = Bytes(338, 2);
        using var device = await fake.ConnectAsync();

        await device.DeleteEmployeesData(42, VerificationMethod.RightMiddle);

        // R4C.4: the document defines backup code bits 0-3 only (FP1, FP2, password, card); today's code sends
        // RightMiddle as 0x10, which deletes nothing on a device that follows the document.
        AssertFrame(DeleteUser, [0, 0, 0, 0, 42, 0x10], Assert.Single(AfterConnect(fake)));
        Assert.Single(fake.Users);
        Assert.Equal(2, fake.Templates.Count);
    }

    [Theory]
    [InlineData("FACE7", BiometricType.Face)]
    [InlineData("M7A+-N", BiometricType.Finger)]
    [InlineData("M5Plus", BiometricType.Finger)]
    [InlineData("TX628", BiometricType.Unknown)]
    public async Task GetDeviceBiometricType_DecodesTheTypeCode_Today(string typeCode, BiometricType expected)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.DeviceTypeCode = typeCode;
        using var device = await fake.ConnectAsync();

        // R48.4: the document has no mapping from type code to biometric type; this is today's BiometricTypes table.
        Assert.Equal(expected, await device.GetDeviceBiometricType());
        Assert.Equal((expected, typeCode), (device.DeviceBiometricType, device.DeviceType));
        AssertFrame(GetDeviceTypeCode, [], Assert.Single(AfterConnect(fake)));
    }

    [Fact]
    public async Task ReceivedRecord_RealTimeRecord0xDF_RaisesItWithTheRecord_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var received = new TaskCompletionSource<Record>(TaskCreationOptions.RunContinuationsAsynchronously);
        device.ReceivedRecord += (_, r) => received.TrySetResult(r);
        var when = new DateTime(2026, 10, 25, 7, 58, 12);

        await fake.SendRealTimeRecordAsync(new FakeRecord(123456, when, 0x81) { BackupCode = 2, WorkType = 0x010203 });
        var r = await received.Task.WaitAsync(Wait, TestContext.Current.CancellationToken);

        // RF.14: the record as section 41 lays it out, but R40.5: today's code counts the seconds from 2000-01-02, so
        // the time comes back one day later than the device's (the fake counts from 2000-01-01).
        Assert.Equal(
            (123456UL, when.AddDays(1), (byte)2, (byte)0x81, 0x010203UL),
            (r.UserCode, r.DateTime, r.BackupCode, r.RecordType, r.WorkType));
        // "only response message": the PC sends nothing back.
        Assert.Empty(AfterConnect(fake));
    }

    [Fact]
    public async Task DevicePing_RaisesDevicePing_AndAnswersFF_Today()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var pinged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        device.DevicePing += (_, _) => pinged.TrySetResult();

        await fake.PingAsync();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(Wait);
        await pinged.Task.WaitAsync(timeout.Token);
        while (AfterConnect(fake).Length == 0) await Task.Delay(10, timeout.Token);

        // RF.13 / RFF.1: the document has no ping (0x7F is the C5's "Sending Email", answered 0xFF); today's code takes
        // a device 0x7F as a ping and answers in the answer format: 0xFF, RET 0x00, no data.
        var pong = Assert.Single(AfterConnect(fake));
        Assert.Equal((fake.DeviceId, (byte)0xFF, (byte?)0x00, 0), (pong.DeviceId, pong.Code, pong.Ret, pong.Data.Length));
    }
}
