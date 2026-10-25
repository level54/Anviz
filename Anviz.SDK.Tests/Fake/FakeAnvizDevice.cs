using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Anviz.SDK.Tests.Fake;
using Xunit.v3;
using static Anviz.SDK.Tests.Fake.CommandCode;

[assembly: FrameErrorsFailTheTest]

namespace Anviz.SDK.Tests.Fake;

/// <summary>
/// An Anviz device on 127.0.0.1 that answers from the protocol document (Docs/Communication Protocol.pdf), not from
/// the SDK's code. A received frame that breaks the document (header, LEN, CRC, device id) is a frame error: the fake
/// closes that connection; a command with the wrong data length is answered with ACK_FAIL. Either fails the test once
/// it has run (<see cref="FrameErrorsFailTheTestAttribute"/>), beside any failure of the test's own.
/// </summary>
public sealed class FakeAnvizDevice : IAsyncDisposable
{
    public const byte AckSuccess = 0x00, AckFail = 0x01, AckNoUser = 0x06;

    /// <summary>10. Record date and time: "how many seconds is it since the year 2000".</summary>
    public static readonly DateTime RecordEpoch = new(2000, 1, 1);

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
    private readonly TaskCompletionSource clientClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The answer to each command code, from the document's layout for it.</summary>
    private readonly Dictionary<byte, Func<Frame, Frame>> answers;

    private FakeAnvizDevice()
    {
        answers = new()
        {
            // 6. Set the date and time: the answer has no data.
            [SetDateTime] = command => Answer(command, []),
            [GetRecordInformation] = command => Answer(command, RecordInformation()),
            [DownloadRecords] = DownloadRecordsAnswer,
            [DownloadTemplate] = DownloadTemplateAnswer,
            [UploadTemplate] = UploadTemplateAnswer,
            // 18. Get device type code: 8 bytes of type code, ASCII, zero-padded ("TC400"000).
            [GetDeviceTypeCode] = command => Answer(command, Encoding.ASCII.GetBytes(DeviceTypeCode.PadRight(8, '\0'))[..8]),
            [DeleteUser] = DeleteUserAnswer,
            // 23. Initialize the user area: clears all staff info, FP, password and card data; no data either way.
            [InitializeUserArea] = command => { Users.Clear(); Templates.Clear(); return Answer(command, []); },
            [ClearRecords] = ClearRecordsAnswer,
            [EnrollFingerprint] = EnrollFingerprintAnswer,
            [DownloadStaff] = DownloadStaffAnswer,
            [UploadStaff] = UploadStaffAnswer,
        };
        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        acceptLoop = AcceptAsync();
    }

    /// <summary>
    /// The command's data length from the document's "Command: (N Byte)" layout, for every command the fake answers but
    /// 0x73 (1 + N records, checked by its answer). 24 (0x4E): the LEN example and the byte table give 4, the "Data
    /// format: (3 Byte)" heading 3; 4 is taken.
    /// </summary>
    private static readonly Dictionary<byte, int> DataLength = new()
    {
        [SetDateTime] = 6, [GetRecordInformation] = 0, [DownloadRecords] = 2, [DownloadTemplate] = 6, [UploadTemplate] = 344,
        [GetDeviceTypeCode] = 0, [DeleteUser] = 6, [InitializeUserArea] = 0, [ClearRecords] = 4, [EnrollFingerprint] = 7,
        [DownloadStaff] = 2,
    };

    public static Task<FakeAnvizDevice> StartAsync() => Task.FromResult(new FakeAnvizDevice());

    /// <summary>
    /// Connects the SDK. The yield moves the caller off the SDK's receiver thread: when the 0x48 answer is read before
    /// that thread first waits, the caller's code runs on it, and a Dispose there joins the thread to itself and hangs.
    /// </summary>
    public async Task<AnvizDevice> ConnectAsync()
    {
        var device = await new AnvizManager().Connect("127.0.0.1", Port);
        await Task.Yield();
        return device;
    }

    /// <summary>Completes when the SDK side closes a connection.</summary>
    public Task ClientClosed => clientClosed.Task;

    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public ulong DeviceId { get; set; } = 1;
    public string DeviceTypeCode { get; set; } = "M7A+-N";
    public TimeSpan AnswerDelay { get; set; }
    public List<FakeUser> Users { get; } = [];
    public List<FakeRecord> Records { get; } = [];

    /// <summary>Fingerprint templates by user code and backup code (1 = FP1, 2 = FP2).</summary>
    public Dictionary<(ulong UserCode, byte BackupCode), byte[]> Templates { get; } = [];

    private int recordCursor, staffCursor;

    /// <summary>Every well-formed frame received, in order.</summary>
    public IReadOnlyList<Frame> Received
    {
        get { lock (received) return received.ToArray(); }
    }

    /// <summary>The frame errors so far, cleared: a test that expects one takes it, so it does not fail the test.</summary>
    public IReadOnlyList<string> TakeFrameErrors()
    {
        lock (frameErrors)
        {
            var taken = frameErrors.ToArray();
            frameErrors.Clear();
            return taken;
        }
    }

    /// <summary>One-shot: the next <paramref name="command"/> is answered with exactly these bytes (error answers, malformed frames).</summary>
    public void AnswerNext(byte command, Func<Frame, byte[]> rawAnswer) => nextRawAnswers[command] = rawAnswer;

    /// <summary>One-shot: the next answer is written in pieces, cut at these byte offsets, with a short pause between.</summary>
    public void SplitNextAnswer(params int[] cuts) => nextCuts = cuts;

    /// <summary>A success answer to <paramref name="command"/>: ACK = command + 0x80, RET = ACK_SUCCESS.</summary>
    private Frame Answer(Frame command, byte[] data) => new(DeviceId, (byte)(command.Code + 0x80), AckSuccess, data);

    /// <summary>An error answer: "When the RET != ACK_SUCCESS, the DATA and LEN in the response data are always 0."</summary>
    private Frame Fail(Frame command, byte ret = AckFail) => new(DeviceId, (byte)(command.Code + 0x80), ret, []);

    /// <summary>A frame the fake does not answer yet: a frame error, and ACK_FAIL so the SDK does not wait 10 s.</summary>
    private Frame NotAnswered(Frame command, string what)
    {
        FrameError($"The fake does not answer 0x{command.Code:X2} {what}. Data: {Convert.ToHexString(command.Data)}");
        return Fail(command);
    }

    // The document writes no byte order for multi-byte data; every multi-byte example (LEN) is high byte first.
    private static byte[] BigEndian(ulong value, int length)
    {
        var bytes = new byte[length];
        for (var i = length - 1; i >= 0; i--, value >>= 8) bytes[i] = (byte)value;
        return bytes;
    }

    private static ulong ReadBigEndian(ReadOnlySpan<byte> bytes)
    {
        ulong value = 0;
        foreach (var b in bytes) value = (value << 8) | b;
        return value;
    }

    /// <summary>9. Get record information: user, FP, password, card, all record and new record amounts, 3 bytes each.</summary>
    private byte[] RecordInformation() =>
    [
        .. BigEndian((ulong)Users.Count, 3),
        .. BigEndian((ulong)Templates.Count, 3),
        .. BigEndian((ulong)Users.Count(u => u.Password.HasValue), 3),
        .. BigEndian((ulong)Users.Count(u => u.Card.HasValue), 3),
        .. BigEndian((ulong)Records.Count, 3),
        .. BigEndian((ulong)Records.Count(r => r.New), 3),
    ];

    /// <summary>
    /// 10. Download T&amp;A records: parameter 1 restarts (all records), 0 goes on; record amount &lt;= 25. DATA = N, then
    /// N records of 14 bytes: user code (5), seconds since 2000 (4), backup code, record type, work types (3).
    /// </summary>
    private Frame DownloadRecordsAnswer(Frame command)
    {
        var (parameter, amount) = (command.Data[0], command.Data[1]);
        if (parameter == 1) recordCursor = 0;
        else if (parameter != 0) return NotAnswered(command, $"parameter {parameter}");
        if (amount > 25) return NotAnswered(command, $"record amount {amount} (the document allows 25)");

        var page = Records.Skip(recordCursor).Take(amount).ToList();
        recordCursor += page.Count;
        var data = new List<byte> { (byte)page.Count };
        foreach (var record in page)
        {
            data.AddRange(BigEndian(record.UserCode, 5));
            data.AddRange(BigEndian((ulong)(record.DateTime - RecordEpoch).TotalSeconds, 4));
            data.Add(0); // backup code: FP1
            data.Add(record.RecordType);
            data.AddRange(BigEndian(0, 3)); // work type
        }
        return Answer(command, [.. data]);
    }

    /// <summary>
    /// 44. Download employees data (extended), B) Unicode: parameter 1 starts, 0 goes on; at most 8 records per answer.
    /// DATA = N, then N staff records of 40 bytes.
    /// </summary>
    private Frame DownloadStaffAnswer(Frame command)
    {
        var (parameter, amount) = (command.Data[0], command.Data[1]);
        if (parameter == 1) staffCursor = 0;
        else if (parameter != 0) return NotAnswered(command, $"parameter {parameter}");

        var page = Users.Skip(staffCursor).Take(Math.Min((int)amount, 8)).ToList();
        staffCursor += page.Count;
        return Answer(command, [(byte)page.Count, .. page.SelectMany(StaffRecord)]);
    }

    /// <summary>
    /// 44 B) staff information, 40 bytes: user ID (5), password length (high nibble) + password low 20 bits (3), card ID (4),
    /// name (20), department, group, attendance mode, FP enroll state (2: bit 0 FP1, bit 1 FP2), password high 8 bits,
    /// keep, special info (bits 7-6 authority, 1 = normal user; bit 4: 1 = 32-bit card ID, 0 = 24-bit). An empty item is
    /// 0xFF. The document does not say how the name is encoded: UTF-16 high byte first, zero-padded.
    /// </summary>
    private byte[] StaffRecord(FakeUser user)
    {
        var record = new byte[40];
        BigEndian(user.UserCode, 5).CopyTo(record, 0);
        if (user.Password is { } password)
        {
            record[5] = (byte)((password.ToString().Length << 4) | (int)((password >> 16) & 0x0F));
            record[6] = (byte)(password >> 8);
            record[7] = (byte)password;
            record[37] = (byte)(password >> 20);
        }
        else record[5] = record[6] = record[7] = record[37] = 0xFF;
        (user.Card is { } card ? BigEndian(card, 4) : [0xFF, 0xFF, 0xFF, 0xFF]).CopyTo(record, 8);
        Encoding.BigEndianUnicode.GetBytes(user.Name).CopyTo(record, 12);
        record[32] = user.Department;
        record[33] = 1; // group
        record[36] = (byte)((Templates.ContainsKey((user.UserCode, 1)) ? 1 : 0) | (Templates.ContainsKey((user.UserCode, 2)) ? 2 : 0));
        record[38] = 0xFF; // keep
        record[39] = (byte)(0x40 | (user.Card > 0xFFFFFF ? 0x10 : 0));
        return record;
    }

    /// <summary>
    /// 45. Upload staff information (extended): DATA = N, then N records of 30 bytes (A, ANSI, N &lt;= 12) or 40 bytes
    /// (B, Unicode, N &lt;= 8). The answer is a 2-byte flag: bit n = 1 when staff n + 1 was stored.
    /// </summary>
    private Frame UploadStaffAnswer(Frame command)
    {
        var count = command.Data[0];
        var length = count == 0 ? 0 : (command.Data.Length - 1) / count;
        if ((length != 30 && length != 40) || 1 + count * length != command.Data.Length || count > (length == 40 ? 8 : 12))
            return NotAnswered(command, $"with {count} records in {command.Data.Length - 1} bytes");

        for (var i = 0; i < count; i++)
        {
            var user = ParseStaff(command.Data.AsSpan(1 + i * length, length));
            Users.RemoveAll(u => u.UserCode == user.UserCode);
            Users.Add(user);
        }
        var flags = (1 << count) - 1;
        return Answer(command, [(byte)(flags >> 8), (byte)flags]);
    }

    /// <summary>
    /// 45 A) / B) staff information: as <see cref="StaffRecord"/>; the ANSI record has a 10-byte name (ASCII; the
    /// document does not say), so department is byte 23 and the password high 8 bits byte 28.
    /// </summary>
    private static FakeUser ParseStaff(ReadOnlySpan<byte> record)
    {
        var unicode = record.Length == 40;
        var password = record[5..8].IndexOfAnyExcept((byte)0xFF) < 0
            ? (ulong?)null
            : ((ulong)record[unicode ? 37 : 27] << 20) | ((ulong)(record[5] & 0x0F) << 16) | ReadBigEndian(record[6..8]);
        var card = record[8..12].IndexOfAnyExcept((byte)0xFF) < 0 ? (ulong?)null : ReadBigEndian(record[8..12]);
        var name = unicode
            ? Encoding.BigEndianUnicode.GetString(record[12..32])
            : Encoding.ASCII.GetString(record[12..22]);
        return new FakeUser(ReadBigEndian(record[..5]), name.TrimEnd('\0'))
        {
            Password = password, Card = card, Department = record[unicode ? 32 : 22],
        };
    }

    /// <summary>14. Download FP template: user code (5), backup code (1 = FP1, 2 = FP2); the answer is the 338-byte template.</summary>
    private Frame DownloadTemplateAnswer(Frame command) =>
        Templates.TryGetValue((ReadBigEndian(command.Data.AsSpan(0, 5)), command.Data[5]), out var template)
            ? Answer(command, template)
            : Fail(command, AckNoUser);

    /// <summary>15. Upload FP template: user code (5), backup code, template (338) = 344 bytes; no data in the answer.</summary>
    private Frame UploadTemplateAnswer(Frame command)
    {
        Templates[(ReadBigEndian(command.Data.AsSpan(0, 5)), command.Data[5])] = command.Data[6..];
        return Answer(command, []);
    }

    /// <summary>
    /// 38. Enroll user FP online: ID (5), backup ID, enroll times (0 = first, 1 = second); no data in the answer.
    /// After the second capture the fake holds a made-up 338-byte template for that finger.
    /// </summary>
    private Frame EnrollFingerprintAnswer(Frame command)
    {
        var userCode = ReadBigEndian(command.Data.AsSpan(0, 5));
        if (command.Data[6] == 1)
            Templates[(userCode, command.Data[5])] = Enumerable.Range(0, 338).Select(i => (byte)(i + (int)userCode)).ToArray();
        return Answer(command, []);
    }

    /// <summary>
    /// 22. Delete the designated user data: user code (5), backup code (bit 0 FP1, bit 1 FP2, bit 2 password, bit 3 card;
    /// 0xFF deletes all the user's data, staff info included); no data in the answer, ACK_NO_USER for an unknown user.
    /// </summary>
    private Frame DeleteUserAnswer(Frame command)
    {
        var userCode = ReadBigEndian(command.Data.AsSpan(0, 5));
        var backup = command.Data[5];
        var index = Users.FindIndex(u => u.UserCode == userCode);
        if (index < 0) return Fail(command, AckNoUser);

        if (backup == 0xFF) Users.RemoveAt(index);
        else
            Users[index] = Users[index] with
            {
                Password = (backup & 4) != 0 ? null : Users[index].Password,
                Card = (backup & 8) != 0 ? null : Users[index].Card,
            };
        if ((backup & 1) != 0) Templates.Remove((userCode, 1));
        if ((backup & 2) != 0) Templates.Remove((userCode, 2));
        return Answer(command, []);
    }

    /// <summary>24. Clear up records: clear type 0 deletes all records; the answer is the amount deleted (3 bytes).</summary>
    private Frame ClearRecordsAnswer(Frame command)
    {
        if (command.Data[0] != 0) return NotAnswered(command, $"clear type {command.Data[0]}");
        var deleted = Records.Count;
        Records.Clear();
        return Answer(command, BigEndian((ulong)deleted, 3));
    }

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
                if (DataLength.TryGetValue(frame.Code, out var length) && frame.Data.Length != length)
                {
                    FrameError($"Command 0x{frame.Code:X2} has {frame.Data.Length} data bytes, the document says {length}. " +
                               $"Data: {Convert.ToHexString(frame.Data)}");
                    answer = Fail(frame).ToBytes();
                }
                else if (nextRawAnswers.TryRemove(frame.Code, out var raw))
                    answer = raw(frame);
                else if (answers.TryGetValue(frame.Code, out var build))
                    answer = build(frame).ToBytes();
                else
                {
                    FrameError($"The fake has no answer for command 0x{frame.Code:X2}.");
                    answer = Fail(frame).ToBytes();
                }

                if (AnswerDelay > TimeSpan.Zero) await Task.Delay(AnswerDelay, stop.Token);
                await WriteAsync(stream, answer, Interlocked.Exchange(ref nextCuts, null));
            }
        }
        catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
        {
            if (!stop.IsCancellationRequested) clientClosed.TrySetResult();
        }
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
        var errors = TakeFrameErrors();
        if (errors.Count == 0) return;
        var message = "The fake device received frames that break the document:" + Environment.NewLine + string.Join(Environment.NewLine, errors);
        // Thrown here, it would replace an exception already leaving the test; the attribute fails the test after it ran.
        if (TestContext.Current.Test is { } test)
            TestContext.Current.KeyValueStorage.AddOrUpdate(FrameErrorsFailTheTestAttribute.Key(test), message, (_, before) => before + Environment.NewLine + message);
        else
            Assert.Fail(message);
    }
}

/// <summary>
/// Fails a test, after it has run, with the frame errors its fakes kept. xUnit reports this failure beside the test's
/// own, so a frame error never hides an assertion that failed first.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class FrameErrorsFailTheTestAttribute : BeforeAfterTestAttribute
{
    public static string Key(Xunit.Sdk.ITest test) => $"{nameof(FakeAnvizDevice)}.FrameErrors:{test.UniqueID}";

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.KeyValueStorage.TryRemove(Key(test), out var message)) Assert.Fail((string)message!);
    }
}

public sealed record FakeUser(ulong UserCode, string Name)
{
    public byte Department { get; init; } = 1;
    public ulong? Password { get; init; }
    public ulong? Card { get; init; }
}

public sealed record FakeRecord(ulong UserCode, DateTime DateTime, byte RecordType)
{
    /// <summary>Counted in the new record amount (0x3C).</summary>
    public bool New { get; init; }
}
