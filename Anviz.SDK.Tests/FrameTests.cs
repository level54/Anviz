using System.Net;
using System.Net.Sockets;
using Anviz.SDK.Exceptions;
using Anviz.SDK.Responses;
using Anviz.SDK.Tests.Fake;
using static Anviz.SDK.Tests.Fake.CommandCode;
using static Anviz.SDK.Tests.TestHelpers;

namespace Anviz.SDK.Tests;

/// <summary>The frame as the document lays it out (Part I): error answers, frames split over TCP reads, bad CRCs.</summary>
public class FrameTests
{
    [Fact]
    public async Task ErrorAnswer_IsAnvizException_AndConnectionStays()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        // RF.10: an error answer is header, RET, LEN 0x00 0x00, CRC.
        fake.AnswerNext(DeleteUser, _ => new Frame(fake.DeviceId, DeleteUser + 0x80, FakeAnvizDevice.AckNoUser, []).ToBytes());

        var ex = await Assert.ThrowsAsync<AnvizException>(() => device.DeleteEmployeesData(999));

        Assert.Equal(AnvizReturnCode.NoUser, ex.ReturnCode);
        var stats = await device.GetDownloadInformation();
        Assert.Equal((ulong)fake.Users.Count, stats.UserAmount);
    }

    [Fact]
    public async Task SplitHeader_IsReadWhole()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        fake.SplitNextAnswer(3);

        Assert.NotNull(await device.GetDownloadInformation());
    }

    [Fact]
    public async Task SplitEverywhere_IsReadWhole()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        AddRecords(fake, 60);
        using var device = await fake.ConnectAsync();
        fake.SplitNextAnswer(1, 6, 7, 9, 12);

        Assert.Equal(60, (await device.DownloadRecords()).Count);
    }

    [Fact]
    public async Task BadCrc_EndsConnection()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        fake.AnswerNext(GetRecordInformation, _ =>
        {
            var answer = new Frame(fake.DeviceId, GetRecordInformation + 0x80, FakeAnvizDevice.AckSuccess, new byte[18]).ToBytes();
            answer[^1] ^= 0xFF;
            return answer;
        });

        await Assert.ThrowsAsync<DeviceConnectionLostException>(() => device.GetDownloadInformation());
        await fake.ClientClosed.WaitAsync(Wait, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeviceSentFrameWithData_IsReadWhole_AndRaisedAsReceivedPacket()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var packets = new List<Response>();
        device.ReceivedPacket += (_, e) => packets.Add(e);
        byte[] data = [1, 2, 3];
        // RF.12: a frame the device starts has no RET, so LEN is bytes 6-7. Sent before the 0x3C answer.
        fake.AnswerNext(GetRecordInformation, _ =>
        [
            .. new Frame(fake.DeviceId, 0x5F, null, data).ToBytes(),
            .. new Frame(fake.DeviceId, GetRecordInformation + 0x80, FakeAnvizDevice.AckSuccess, new byte[18]).ToBytes(),
        ]);

        Assert.NotNull(await device.GetDownloadInformation());

        var packet = Assert.Single(packets);
        Assert.Equal(0x5F, packet.ResponseCode);
        Assert.Equal(data, packet.DATA);
    }
    [Theory]
    [InlineData(40)]
    [InlineData(30)]
    public async Task RX6_RX13_NameLongerThanItsField_IsCut_AndLeavesTheOtherFields(int recordLength)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        // 25 characters: the name field is 10 characters in both records (20 bytes Unicode, 10 bytes ANSI).
        var user = new UserInfo(7, "ABCDEFGHIJKLMNOPQRSTUVWXY");

        await (recordLength == 40 ? device.SetEmployeesDataUnicode(user) : device.SetEmployeesDataAnsi(user));

        var record = Assert.Single(AfterConnect(fake)).Data[1..];
        Assert.Equal(recordLength, record.Length);
        var nameEnd = recordLength == 40 ? 32 : 22;
        // Department, group, attendance mode as UserInfo(id, name) sets them; FP enroll state 0.
        Assert.Equal(new byte[] { 1, 1, 6, 0, 0 }, record[nameEnd..(nameEnd + 5)]);
        Assert.Equal("ABCDEFGHIJ", Assert.Single(fake.Users).Name);
    }
    [Fact]
    public async Task RX5_UserTheReadFailsOn_IsSkipped_AndRaisedAsDeviceError()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.Users.AddRange([new FakeUser(1, "A"), new FakeUser(2, "B"), new FakeUser(3, "C")]);
        using var device = await fake.ConnectAsync();
        var errors = new List<Exception>();
        device.DeviceError += (_, e) => errors.Add(e);
        var failure = new InvalidOperationException("The handler failed on user 2.");
        device.EmployeesDataGet += (_, e) => { if (e.Employee.Id == 2) throw failure; };

        var users = await device.GetEmployeesData();

        Assert.Equal([1UL, 3UL], users.Select(u => u.Id));
        Assert.Same(failure, Assert.Single(errors));
    }

    [Fact]
    public async Task RX4_FirstCommandAtConnectFails_TheConnectionIsClosed()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        fake.AnswerNext(GetDeviceTypeCode, _ => new Frame(fake.DeviceId, GetDeviceTypeCode + 0x80, FakeAnvizDevice.AckFail, []).ToBytes());

        await Assert.ThrowsAsync<AnvizException>(() => new AnvizManager().Connect("127.0.0.1", fake.Port));

        await fake.ClientClosed.WaitAsync(Wait, TestContext.Current.CancellationToken);
    }
    [Fact]
    public async Task RF11_ErrorAnswerForAnotherCode_IsDeviceError_AndTheWaitingCallGetsItsAnswer()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var packets = new List<Response>();
        var errors = new List<Exception>();
        device.ReceivedPacket += (_, e) => packets.Add(e);
        device.DeviceError += (_, e) => errors.Add(e);
        // Before its answer to 0x48, the device sends an ACK_FAIL answer to 0x3C, which the SDK did not ask for: the
        // answer code (0xBC) names the command the error is for, so it is not the waiting call's.
        fake.AnswerNext(GetDeviceTypeCode, _ =>
        [
            .. new Frame(fake.DeviceId, GetRecordInformation + 0x80, FakeAnvizDevice.AckFail, []).ToBytes(),
            .. TypeCodeAnswer(fake, "M7A+-N"),
        ]);

        Assert.Equal("M7A+-N", await device.GetDeviceTypeCode());

        Assert.Equal(AnvizReturnCode.Fail, Assert.IsType<AnvizException>(Assert.Single(errors)).ReturnCode);
        Assert.Empty(packets);
    }

    [Fact]
    public async Task RF17_StrayRepeatOfAnAnswer_DoesNotCompleteTheNextCall()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var packets = new List<Response>();
        device.ReceivedPacket += (_, e) => packets.Add(e);
        // The device sends its answer to the first 0x48 twice; the repeat is on the wire before the second 0x48 goes.
        fake.AnswerNext(GetDeviceTypeCode, _ => [.. TypeCodeAnswer(fake, "OLD"), .. TypeCodeAnswer(fake, "OLD")]);

        Assert.Equal("OLD", await device.GetDeviceTypeCode());
        Assert.Equal("M7A+-N", await device.GetDeviceTypeCode());

        var repeat = Assert.Single(packets);
        Assert.Equal(GetDeviceTypeCode + 0x80, repeat.ResponseCode);
    }

    [Fact]
    public async Task RX2_DisposeWhereTheConnectCompletes_NeverHangs()
    {
        var ct = TestContext.Current.CancellationToken;
        var port = FreePort();
        var manager = new AnvizManager();
        manager.Listen(new IPEndPoint(IPAddress.Loopback, port));
        try
        {
            // The test is the device. 2,000 pings wait before the SDK accepts: its reader reads them without waiting,
            // so the answer to its 0x48 arrives while the reader is still on the thread it started on.
            using var deviceSide = new TcpClient();
            await deviceSide.ConnectAsync(IPAddress.Loopback, port, ct);
            var stream = deviceSide.GetStream();
            var ping = new Frame(1, 0x7F, null, []).ToBytes();
            await stream.WriteAsync(Enumerable.Repeat(ping, 2000).SelectMany(b => b).ToArray(), ct);
            var answered = AnswerTypeCodeAsync(stream, ct);

            // The caller goes on where the SDK completes the connect, and disposes there at once.
            var acceptAndDispose = Task.Run(async () => (await manager.Accept()).Dispose());

            await acceptAndDispose.WaitAsync(TimeSpan.FromSeconds(10), ct);
            await answered;
        }
        finally
        {
            manager.StopServer();
        }
    }

    [Fact]
    public async Task RX1_ExceptionInTheReader_IsDeviceError_AndEndsTheConnection()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var errors = new List<Exception>();
        device.DeviceError += (_, e) => errors.Add(e);
        var failure = new InvalidOperationException("The DevicePing handler failed.");
        device.DevicePing += (_, _) => throw failure;
        // The device pings instead of answering 0x3C: the handler throws on the reader while the call waits.
        fake.AnswerNext(GetRecordInformation, _ => new Frame(fake.DeviceId, 0x7F, null, []).ToBytes());

        var lost = await Assert.ThrowsAsync<DeviceConnectionLostException>(device.GetDownloadInformation);

        Assert.Equal("Device connection lost.", lost.Message); // not the 10-second timeout
        Assert.Same(failure, Assert.Single(errors));
        await fake.ClientClosed.WaitAsync(Wait, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DeviceConnectionLostException>(device.GetDownloadInformation);
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>Reads the SDK's frames (pongs) until its 0x48, and answers it.</summary>
    private static async Task AnswerTypeCodeAsync(NetworkStream stream, CancellationToken ct)
    {
        while (true)
        {
            var start = new byte[6];
            await stream.ReadExactlyAsync(start, ct);
            var bytes = new byte[Frame.HeaderLength(start[5])];
            start.CopyTo(bytes, 0);
            await stream.ReadExactlyAsync(bytes.AsMemory(6), ct);
            var rest = new byte[((bytes[^2] << 8) | bytes[^1]) + 2];
            await stream.ReadExactlyAsync(rest, ct);
            if (start[5] != GetDeviceTypeCode) continue;
            await stream.WriteAsync(new Frame(1, GetDeviceTypeCode + 0x80, FakeAnvizDevice.AckSuccess, "M7A+-N\0\0"u8.ToArray()).ToBytes(), ct);
            return;
        }
    }

    private static byte[] TypeCodeAnswer(FakeAnvizDevice fake, string typeCode) =>
        new Frame(fake.DeviceId, GetDeviceTypeCode + 0x80, FakeAnvizDevice.AckSuccess, System.Text.Encoding.ASCII.GetBytes(typeCode.PadRight(8, '\0'))).ToBytes();
}
