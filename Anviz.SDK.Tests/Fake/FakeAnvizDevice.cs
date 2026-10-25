using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Anviz.SDK.Tests.Fake;

/// <summary>
/// An Anviz device on 127.0.0.1 that answers from the protocol document (Docs/Communication Protocol.pdf), not from
/// the SDK's code. A received frame that breaks the document (header, LEN, CRC, device id) is a frame error: the fake
/// closes that connection and fails the test when it is disposed.
/// </summary>
public sealed class FakeAnvizDevice : IAsyncDisposable
{
    public const byte AckSuccess = 0x00, AckFail = 0x01;

    // The pause between the pieces of a split answer, long enough for each piece to arrive as its own read.
    private static readonly TimeSpan SplitPause = TimeSpan.FromMilliseconds(50);

    private readonly TcpListener listener;
    private readonly CancellationTokenSource stop = new();
    private readonly Task acceptLoop;
    private readonly ConcurrentBag<Task> connections = [];
    private readonly List<Frame> received = [];
    private readonly List<string> frameErrors = [];
    private readonly ConcurrentDictionary<byte, Func<Frame, byte[]>> nextRawAnswers = new();
    private int[]? nextCuts;

    /// <summary>The answer to each command code, from the document's layout for it.</summary>
    private readonly Dictionary<byte, Func<Frame, Frame>> answers;

    private FakeAnvizDevice()
    {
        answers = new()
        {
            // 18. Get device type code: 8 bytes of type code, ASCII, zero-padded ("TC400"000).
            [0x48] = command => Answer(command, Encoding.ASCII.GetBytes(DeviceTypeCode.PadRight(8, '\0'))[..8]),
        };
        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        acceptLoop = AcceptAsync();
    }

    public static Task<FakeAnvizDevice> StartAsync() => Task.FromResult(new FakeAnvizDevice());

    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public ulong DeviceId { get; set; } = 1;
    public string DeviceTypeCode { get; set; } = "M7A+-N";
    public TimeSpan AnswerDelay { get; set; }
    public List<FakeUser> Users { get; } = [];
    public List<FakeRecord> Records { get; } = [];

    /// <summary>Every well-formed frame received, in order.</summary>
    public IReadOnlyList<Frame> Received
    {
        get { lock (received) return received.ToArray(); }
    }

    /// <summary>One-shot: the next <paramref name="command"/> is answered with exactly these bytes (error answers, malformed frames).</summary>
    public void AnswerNext(byte command, Func<Frame, byte[]> rawAnswer) => nextRawAnswers[command] = rawAnswer;

    /// <summary>One-shot: the next answer is written in pieces, cut at these byte offsets, with a short pause between.</summary>
    public void SplitNextAnswer(params int[] cuts) => nextCuts = cuts;

    /// <summary>A success answer to <paramref name="command"/>: ACK = command + 0x80, RET = ACK_SUCCESS.</summary>
    private Frame Answer(Frame command, byte[] data) => new(DeviceId, (byte)(command.Code + 0x80), AckSuccess, data);

    private async Task AcceptAsync()
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync(stop.Token);
                connections.Add(ServeAsync(client));
            }
        }
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException) { }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using var _ = client;
        client.NoDelay = true;
        var stream = client.GetStream();
        try
        {
            while (true)
            {
                var frame = await ReadFrameAsync(stream);
                if (frame == null) return;
                lock (received) received.Add(frame);

                byte[] answer;
                if (nextRawAnswers.TryRemove(frame.Code, out var raw))
                    answer = raw(frame);
                else if (answers.TryGetValue(frame.Code, out var build))
                    answer = build(frame).ToBytes();
                else
                {
                    FrameError($"The fake has no answer for command 0x{frame.Code:X2}.");
                    answer = new Frame(DeviceId, (byte)(frame.Code + 0x80), AckFail, []).ToBytes();
                }

                if (AnswerDelay > TimeSpan.Zero) await Task.Delay(AnswerDelay, stop.Token);
                await WriteAsync(stream, answer, Interlocked.Exchange(ref nextCuts, null));
            }
        }
        catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException) { }
    }

    /// <summary>Reads one frame; null when it broke the document (a frame error). A closed connection throws EndOfStreamException.</summary>
    private async Task<Frame?> ReadFrameAsync(NetworkStream stream)
    {
        var start = new byte[6]; // STX, CH (4), code
        await stream.ReadExactlyAsync(start, stop.Token);
        if (start[0] != Frame.Stx)
        {
            FrameError($"Header is 0x{start[0]:X2}, the document says 0xA5.");
            return null;
        }
        var header = Frame.HeaderLength(start[5]);
        var bytes = new byte[header];
        start.CopyTo(bytes, 0);
        await stream.ReadExactlyAsync(bytes.AsMemory(6), stop.Token);
        var length = (bytes[header - 2] << 8) | bytes[header - 1];
        Array.Resize(ref bytes, header + length + 2);
        await stream.ReadExactlyAsync(bytes.AsMemory(header), stop.Token);

        Frame frame;
        try
        {
            frame = Frame.Parse(bytes);
        }
        catch (FormatException e)
        {
            FrameError($"{e.Message} Frame: {Convert.ToHexString(bytes)}");
            return null;
        }
        // Document Part I, 3: a device answers its own id, or 0 (all devices).
        if (frame.DeviceId != 0 && frame.DeviceId != DeviceId)
        {
            FrameError($"Frame for device {frame.DeviceId}; this device is {DeviceId} and answers only its id or 0.");
            return null;
        }
        return frame;
    }

    private async Task WriteAsync(NetworkStream stream, byte[] answer, int[]? cuts)
    {
        var from = 0;
        foreach (var cut in cuts ?? [])
        {
            await stream.WriteAsync(answer.AsMemory(from, cut - from), stop.Token);
            await Task.Delay(SplitPause, stop.Token);
            from = cut;
        }
        await stream.WriteAsync(answer.AsMemory(from), stop.Token);
    }

    private void FrameError(string message)
    {
        lock (frameErrors) frameErrors.Add(message);
    }

    public async ValueTask DisposeAsync()
    {
        stop.Cancel();
        listener.Stop();
        await acceptLoop;
        await Task.WhenAll(connections);
        stop.Dispose();
        lock (frameErrors)
        {
            if (frameErrors.Count > 0)
                Assert.Fail("The fake device received frames that break the document:" + Environment.NewLine +
                            string.Join(Environment.NewLine, frameErrors));
        }
    }
}

public sealed record FakeUser(ulong UserCode, string Name)
{
    public ulong? Password { get; init; }
    public ulong? Card { get; init; }
}

public sealed record FakeRecord(ulong UserCode, DateTime DateTime, byte RecordType);
