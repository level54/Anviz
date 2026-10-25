using System.Diagnostics;
using System.Text;
using Anviz.SDK.Responses;
using Anviz.SDK.Tests.Fake;
using Anviz.SDK.Utils;

namespace Anviz.SDK.Tests;

/// <summary>
/// The timing baseline later slices compare against: fixed data sizes, 3 runs each on a fresh fake and connection,
/// the median in milliseconds (the call only, not the connect). Prints a markdown table; never fails on a number.
/// </summary>
public class TimingTests
{
    private const int Runs = 3;

    private static readonly (string Name, Func<FakeAnvizDevice, Func<AnvizDevice, Task>> Arrange)[] Cases =
    [
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
            for (var i = 1; i <= 200; i++) fake.Templates[((ulong)i, 1)] = Enumerable.Range(0, 338).Select(b => (byte)(b * i)).ToArray();
            return async device =>
            {
                for (var i = 1; i <= 200; i++) await device.GetFingerprintTemplate((ulong)i, Finger.RightThumb);
            };
        }),
        ("Set 200 users (Unicode)", _ => device =>
            device.SetEmployeesDataUnicode(Enumerable.Range(1, 200).Select(i => new UserInfo((ulong)i, $"Jürg Ñ{i:000}")).ToList())),
    ];

    [Fact]
    public async Task Baseline()
    {
        var table = new StringBuilder("| Case | AnswerDelay 0 ms | AnswerDelay 5 ms |\n|---|---:|---:|\n");
        foreach (var (name, arrange) in Cases)
        {
            var zero = await Median(arrange, TimeSpan.Zero);
            var five = await Median(arrange, TimeSpan.FromMilliseconds(5));
            table.Append($"| {name} | {zero} | {five} |\n");
        }
        TestContext.Current.TestOutputHelper!.WriteLine(table.ToString());
    }

    private static async Task<long> Median(Func<FakeAnvizDevice, Func<AnvizDevice, Task>> arrange, TimeSpan delay)
    {
        var times = new long[Runs];
        for (var run = 0; run < Runs; run++)
        {
            await using var fake = await FakeAnvizDevice.StartAsync();
            fake.AnswerDelay = delay;
            var call = arrange(fake);
            using var device = await new AnvizManager().Connect("127.0.0.1", fake.Port);
            var watch = Stopwatch.StartNew();
            await call(device);
            times[run] = watch.ElapsedMilliseconds;
        }
        Array.Sort(times);
        return times[Runs / 2];
    }
}
