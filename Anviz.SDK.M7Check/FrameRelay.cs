using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Anviz.SDK.Check;

/// <summary>
/// A logging relay on 127.0.0.1 between the SDK and the device: the SDK connects here, each connection opens one to the
/// device, and every frame is read whole by the frame rules (0xA5, CH 4, code, RET when code &gt;= 0x80, LEN 2, DATA,
/// CRC 2), logged in hex, then forwarded. The SDK stays unchanged (D3) and the log shows the real wire bytes.
/// A frame the PC sends that is not on the D2-safe list is never forwarded: it is logged BLOCKED and both connections
/// close, so a forbidden command cannot reach the device even through a bug in the program.
/// </summary>
public sealed class FrameRelay : IAsyncDisposable
{
    private readonly string host;
    private readonly int port;
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource stop = new();
    private readonly List<string> frames = [];
    private readonly List<Task> connections = [];
    private readonly Task acceptLoop;
    private long relayTicks;

    public FrameRelay(string host, int port)
    {
        (this.host, this.port) = (host, port);
        listener.Start();
        acceptLoop = AcceptAsync();
    }

    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

    /// <summary>The one user id the user writes (0x73, 0x45, 0x5C, 0x4C) may name; 0 = none yet, every user write is blocked.</summary>
    public ulong TestUserId { get; set; }

    /// <summary>True: frames are not kept (the timing runs); the safe list still holds.</summary>
    public bool Quiet { get; set; }

    /// <summary>The time the relay itself spent on frames (hex text and the forward write), so far.</summary>
    public TimeSpan RelayTime => TimeSpan.FromTicks(Interlocked.Read(ref relayTicks) * TimeSpan.TicksPerSecond / Stopwatch.Frequency);

    /// <summary>The frame lines since the last call (">> " PC to device, "&lt;&lt; " device to PC, "BLOCKED ..."), cleared.</summary>
    public List<string> TakeFrames()
    {
        lock (frames)
        {
            var taken = frames.ToList();
            frames.Clear();
            return taken;
        }
    }

    /// <summary>Why the PC frame may not go to the device; null when it is on the D2-safe list.</summary>
    internal string? Refusal(byte[] frame)
    {
        var code = frame[5];
        var data = frame.AsSpan(code >= 0x80 ? 9 : 8, frame.Length - (code >= 0x80 ? 11 : 10));
        switch (code)
        {
            // Reads: device type, statistics, users, basic/advanced settings, date/time, TCP, device id, serial number,
            // time zone, bells, card inquiry, FP template.
            case 0x48 or 0x3C or 0x72 or 0x30 or 0x32 or 0x38 or 0x3A or 0x46 or 0x24 or 0x50 or 0x54 or 0x7E or 0x44:
                return null;
            case 0xFF: // the SDK's pong to a device ping (an answer frame)
                return null;
            case 0x40: // download records: 1 = all from the start, 0 = next page; 2 ("new records", C6) is not allowed
                return data.Length == 2 && data[0] is 0 or 1 ? null : "0x40 asks for something other than all records";
            case 0x39: // set date/time (D9): only the PC's own time
                return IsNearNow(data) ? null : "0x39 is not the PC's time";
            case 0x73: // one staff record, the test user's (C13)
                return data.Length > 0 && data[0] == 1 && IsTestUser(data[1..]) ? null : "0x73 is not the test user alone";
            case 0x45 or 0x5C or 0x4C: // its template (C13), its enrolment (D10), its removal
                return IsTestUser(data) ? null : $"0x{code:X2} is not for the test user";
            default:
                return $"0x{code:X2} is not on the D2-safe list";
        }
    }

    private bool IsTestUser(ReadOnlySpan<byte> data)
    {
        if (TestUserId == 0 || data.Length < 5) return false;
        ulong id = 0;
        foreach (var b in data[..5]) id = (id << 8) | b;
        return id == TestUserId;
    }

    private static bool IsNearNow(ReadOnlySpan<byte> d)
    {
        if (d.Length != 6) return false;
        try
        {
            return Math.Abs((new DateTime(2000 + d[0], d[1], d[2], d[3], d[4], d[5]) - DateTime.Now).TotalMinutes) < 1;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private async Task AcceptAsync()
    {
        try
        {
            while (true)
            {
                var pc = await listener.AcceptTcpClientAsync(stop.Token);
                lock (connections) connections.Add(ServeAsync(pc));
            }
        }
        // InvalidOperationException: the listener stopped before the loop asked for the next connection.
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException or InvalidOperationException) { }
    }

    private async Task ServeAsync(TcpClient pc)
    {
        using var _ = pc;
        using var device = new TcpClient { NoDelay = true };
        pc.NoDelay = true;
        try
        {
            await device.ConnectAsync(host, port, stop.Token);
            var (pcStream, deviceStream) = (pc.GetStream(), device.GetStream());
            var pumps = new[] { PumpAsync(pcStream, deviceStream, ">> ", check: true), PumpAsync(deviceStream, pcStream, "<< ", check: false) };
            // The first pump to end (a closed side, a bad frame, a blocked frame) ends both.
            await Task.WhenAny(pumps);
            pc.Close();
            device.Close();
            await Task.WhenAll(pumps);
        }
        catch (Exception e) when (IsClosed(e)) { }
    }

    private static bool IsClosed(Exception e) => e is OperationCanceledException or IOException or SocketException or ObjectDisposedException;

    private async Task PumpAsync(NetworkStream from, NetworkStream to, string arrow, bool check)
    {
        try
        {
            while (await ReadFrameAsync(from) is { } frame)
            {
                var started = Stopwatch.GetTimestamp();
                var hex = Quiet ? null : arrow + BitConverter.ToString(frame).Replace('-', ' ');
                var refusal = check ? Refusal(frame) : null;
                if (refusal != null)
                {
                    Keep($"BLOCKED (not sent to the device: {refusal}) {BitConverter.ToString(frame).Replace('-', ' ')}");
                    return;
                }
                if (hex != null) Keep(hex);
                await to.WriteAsync(frame, stop.Token);
                Interlocked.Add(ref relayTicks, Stopwatch.GetTimestamp() - started);
            }
        }
        catch (Exception e) when (IsClosed(e)) { }
    }

    private void Keep(string line)
    {
        lock (frames) frames.Add(line);
    }

    /// <summary>One whole frame; null when the side closed or the frame does not start with 0xA5 (logged).</summary>
    private async Task<byte[]?> ReadFrameAsync(NetworkStream stream)
    {
        var start = new byte[6];
        try
        {
            await stream.ReadExactlyAsync(start, stop.Token);
            if (start[0] != 0xA5)
            {
                Keep($"BAD FRAME (first byte 0x{start[0]:X2}, not 0xA5): connection closed");
                return null;
            }
            var header = start[5] >= 0x80 ? 9 : 8;
            var frame = new byte[header];
            start.CopyTo(frame, 0);
            await stream.ReadExactlyAsync(frame.AsMemory(6), stop.Token);
            var length = (frame[header - 2] << 8) | frame[header - 1];
            Array.Resize(ref frame, header + length + 2);
            await stream.ReadExactlyAsync(frame.AsMemory(header), stop.Token);
            return frame;
        }
        catch (EndOfStreamException)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        stop.Cancel();
        listener.Stop();
        await acceptLoop;
        Task[] open;
        lock (connections) open = connections.ToArray();
        await Task.WhenAll(open);
        stop.Dispose();
    }
}
