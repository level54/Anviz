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
}
