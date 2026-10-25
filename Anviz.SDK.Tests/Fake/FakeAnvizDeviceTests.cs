using System.Net.Sockets;
using Anviz.SDK;
using Anviz.SDK.Utils;

namespace Anviz.SDK.Tests.Fake;

public class FakeAnvizDeviceTests
{
    [Fact]
    public async Task Connect_ReadsDeviceType_AndFrameIsValid()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        Assert.Equal("M7A+-N", device.DeviceType);
        Assert.Equal(BiometricType.Finger, device.DeviceBiometricType);
        var f = Assert.Single(fake.Received);
        Assert.Equal(CommandCode.GetDeviceTypeCode, f.Code);
        Assert.Empty(f.Data);
    }

    /// <summary>
    /// A command whose data length is not the document's is a frame error, answered with ACK_FAIL at once. Each case
    /// sends one byte more than the document's length, or one byte where it needs more (0x73: one 29-byte record).
    /// </summary>
    [Theory]
    [InlineData(CommandCode.SetDateTime, 1)]
    [InlineData(CommandCode.GetRecordInformation, 1)]
    [InlineData(CommandCode.DownloadRecords, 1)]
    [InlineData(CommandCode.DownloadTemplate, 1)]
    [InlineData(CommandCode.UploadTemplate, 1)]
    [InlineData(CommandCode.GetDeviceTypeCode, 1)]
    [InlineData(CommandCode.DeleteUser, 1)]
    [InlineData(CommandCode.InitializeUserArea, 1)]
    [InlineData(CommandCode.ClearRecords, 1)]
    [InlineData(CommandCode.EnrollFingerprint, 1)]
    [InlineData(CommandCode.DownloadStaff, 1)]
    [InlineData(CommandCode.UploadStaff, 30)]
    public async Task WrongDataLength_IsFrameError_AnsweredWithAckFail(byte code, int length)
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", fake.Port, TestContext.Current.CancellationToken);
        var stream = client.GetStream();
        var data = new byte[length];
        data[0] = 1;
        await stream.WriteAsync(new Frame(fake.DeviceId, code, null, data).ToBytes(), TestContext.Current.CancellationToken);

        var answer = new byte[11];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await stream.ReadExactlyAsync(answer, timeout.Token);

        Assert.Equal(new Frame(fake.DeviceId, (byte)(code + 0x80), FakeAnvizDevice.AckFail, []), Frame.Parse(answer), FrameComparer);
        Assert.Contains($"0x{code:X2}", Assert.Single(fake.TakeFrameErrors()));
    }

    private static readonly IEqualityComparer<Frame> FrameComparer = EqualityComparer<Frame>.Create(
        (a, b) => a!.DeviceId == b!.DeviceId && a.Code == b.Code && a.Ret == b.Ret && a.Data.SequenceEqual(b.Data));
}
