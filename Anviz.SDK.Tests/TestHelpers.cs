using Anviz.SDK.Tests.Fake;

namespace Anviz.SDK.Tests;

/// <summary>Helpers the fake-device tests share.</summary>
internal static class TestHelpers
{
    /// <summary>How long a test waits for something the SDK does in the background (an event, a closed connection).</summary>
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    /// <summary>The frames after the 0x48 that Connect sends.</summary>
    public static Frame[] AfterConnect(FakeAnvizDevice fake) => fake.Received.Skip(1).ToArray();

    public static void AssertFrame(byte code, byte[] data, Frame frame)
    {
        Assert.Equal(code, frame.Code);
        Assert.Equal(data, frame.Data);
    }

    /// <summary>
    /// Adds <paramref name="count"/> records to the fake: user codes 1000, 1001, …, one minute apart from 2026-10-25,
    /// record type 0; record i is new when <paramref name="isNew"/> says so (none by default).
    /// </summary>
    public static void AddRecords(FakeAnvizDevice fake, int count, Func<int, bool>? isNew = null)
    {
        for (var i = 0; i < count; i++)
            fake.Records.Add(new FakeRecord((ulong)(1000 + i), new DateTime(2026, 10, 25).AddMinutes(i), 0) { New = isNew?.Invoke(i) ?? false });
    }
}
