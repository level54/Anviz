using Anviz.SDK;
using Anviz.SDK.Utils;

namespace Anviz.SDK.Tests.Fake;

public class FakeAnvizDeviceTests
{
    [Fact]
    public async Task Connect_ReadsDeviceType_AndFrameIsValid()
    {
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await new AnvizManager().Connect("127.0.0.1", fake.Port);
        Assert.Equal("M7A+-N", device.DeviceType);
        Assert.Equal(BiometricType.Finger, device.DeviceBiometricType);
        var f = Assert.Single(fake.Received);
        Assert.Equal(0x48, f.Code);
        Assert.Empty(f.Data);
    }
}
