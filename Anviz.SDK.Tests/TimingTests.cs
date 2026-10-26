using System.Diagnostics;
using System.Text;
using Anviz.SDK.Responses;
using Anviz.SDK.Tests.Fake;
using Anviz.SDK.Utils;

namespace Anviz.SDK.Tests;

/// <summary>
/// The timing baseline later slices compare against: fixed data sizes, 3 runs each on a fresh fake and connection,
/// the median in milliseconds (the call only, not the connect). Prints a markdown table; never fails on a number.
/// Runs alone (its own collection, no parallel classes) so other tests do not slow it.
/// </summary>
[Collection(nameof(TimingTests))]
public class TimingTests
{
    private const int Runs = 3;

    /// <summary>The answer delay asked of the fake; Windows timers make the real one longer, the first row measures it.</summary>
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(5);

    private static byte[] Template(int seed) => Enumerable.Range(0, 338).Select(b => (byte)(b * seed)).ToArray();

    private static readonly (string Name, Func<FakeAnvizDevice, Func<AnvizDevice, Task>> Arrange)[] Cases =
    [
        ("One round trip (get device type code)", _ => device => device.GetDeviceTypeCode()),
        ("Download 5,000 records", fake =>
        {
            var start = new DateTime(2026, 10, 25, 7, 0, 0);
            for (var i = 0; i < 5000; i++) fake.Records.Add(new FakeRecord((ulong)(1000 + i % 500), start.AddMinutes(i), (byte)(i % 4)));
            return device => device.DownloadRecords();
        }),
        ("Get 1,000 users", fake =>
        {
            for (var i = 1; i <= 1000; i++) fake.Users.Add(new FakeUser((ulong)i, $"Jürgen{i:0000}"));
            return device => device.GetEmployeesData();
        }),
        ("Get 200 templates", fake =>
        {
            for (var i = 1; i <= 200; i++) fake.Templates[((ulong)i, 1)] = Template(i);
            return async device =>
            {
                for (var i = 1; i <= 200; i++) await device.GetFingerprintTemplate((ulong)i, Finger.RightThumb);
            };
        }),
        ("Set 200 templates", _ => async device =>
        {
            for (var i = 1; i <= 200; i++) await device.SetFingerprintTemplate((ulong)i, Finger.RightThumb, Template(i));
        }),
        ("Set 200 users (Unicode)", _ => device =>
            device.SetEmployeesDataUnicode(Enumerable.Range(1, 200).Select(i => new UserInfo((ulong)i, $"Jürg Ñ{i:000}")).ToList())),
    ];

    [Fact]
    public async Task Baseline()
    {
        var table = new StringBuilder($"| Case | No answer delay | Task.Delay({Delay.TotalMilliseconds:0}) before each answer |\n|---|---:|---:|\n");
        foreach (var (name, arrange) in Cases)
        {
            var none = await Median(arrange, TimeSpan.Zero);
            var delayed = await Median(arrange, Delay);
            table.Append($"| {name} | {none:0.0} | {delayed:0.0} |\n");
        }
        TestContext.Current.TestOutputHelper!.WriteLine(table.ToString());
    }

    private static async Task<double> Median(Func<FakeAnvizDevice, Func<AnvizDevice, Task>> arrange, TimeSpan delay)
    {
        var times = new double[Runs];
        for (var run = 0; run < Runs; run++)
        {
            await using var fake = await FakeAnvizDevice.StartAsync();
            fake.AnswerDelay = delay;
            var call = arrange(fake);
            using var device = await fake.ConnectAsync();
            var watch = Stopwatch.StartNew();
            await call(device);
            times[run] = watch.Elapsed.TotalMilliseconds;
        }
        Array.Sort(times);
        return times[Runs / 2];
    }
}

/// <summary>Counts the process's live timers, so it runs alone (the timing collection runs without parallel classes).</summary>
[Collection(nameof(TimingTests))]
public class AnswerTimerTests
{
    [Fact]
    public async Task RX7_AnsweredCommands_LeaveNoLiveTimer()
    {
        // RX.7: each command's 10-second answer limit is a timer; it stops when the answer comes, so 500 answered
        // commands do not leave 500 live timers behind.
        await using var fake = await FakeAnvizDevice.StartAsync();
        using var device = await fake.ConnectAsync();
        var before = Timer.ActiveCount;

        for (var i = 0; i < 500; i++) await device.GetDeviceTypeCode();

        Assert.True(Timer.ActiveCount - before < 50, $"{Timer.ActiveCount - before} timers left live by 500 commands.");
    }
}

[CollectionDefinition(nameof(TimingTests), DisableParallelization = true)]
public class TimingTestsCollection;
