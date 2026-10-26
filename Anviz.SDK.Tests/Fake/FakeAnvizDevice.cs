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
    private NetworkStream? latestStream;

    /// <summary>The answer to each command code, from the document's layout for it.</summary>
    private readonly Dictionary<byte, Func<Frame, Frame>> answers;

    private FakeAnvizDevice()
    {
        answers = new()
        {
            // 6. Set the date and time: the 0x38 layout (5.); the answer has no data.
            [SetDateTime] = command =>
            {
                var d = command.Data;
                Clock = new DateTime(2000 + d[0], d[1], d[2], d[3], d[4], d[5]);
                return Answer(command, []);
            },
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
            [GetBasicSettings] = command => Answer(command, BasicSettings),
            // 2. Set basic settings: the 10 bytes are the get answer's bytes 9-17 (password to language flag) and a
            // reserved byte; no data in the answer. (The document's "0xFF = leave this item" is not modelled.)
            [SetBasicSettings] = command => { command.Data[..9].CopyTo(BasicSettings, 8); return Answer(command, []); },
            [GetAdvancedSettings] = command => Answer(command, AdvancedSettings),
            // 4. Set advanced settings: the get answer's 15-byte layout; no data in the answer.
            [SetAdvancedSettings] = command => { command.Data.CopyTo(AdvancedSettings, 0); return Answer(command, []); },
            [GetDateTime] = command => Answer(command, DateTimeBytes(Clock)),
            [GetTcpParameters] = command => Answer(command, TcpParameters),
            // 8. Set TCP/IP parameters: the get answer's 27-byte layout; no data in the answer.
            [SetTcpParameters] = command => { command.Data.CopyTo(TcpParameters, 0); return Answer(command, []); },
            [UploadRecord] = UploadRecordAnswer,
            // 16. Get the device id "which we set in device": 4 bytes.
            [GetDeviceId] = command => Answer(command, BigEndian(DeviceId, 4)),
            [SetDeviceId] = SetDeviceIdAnswer,
            // 25. Initialize System: factory settings back; no data either way (users and records stay).
            [InitializeSystem] = command => Answer(command, []),
            [GetTimeZone] = GetTimeZoneAnswer,
            [SetTimeZone] = SetTimeZoneAnswer,
            [GetBells] = command => Answer(command, Bells),
            [SetBell] = SetBellAnswer,
            // 40. Output signal to open lock without verifying user: no data either way.
            [OpenLock] = command => Answer(command, []),
            // 61. Get device serial number: 16 bytes (the document does not say how they are encoded: ASCII, zero-padded).
            [GetSerialNumber] = command => Answer(command, Encoding.ASCII.GetBytes(SerialNumber.PadRight(16, '\0'))[..16]),
            // 62. Modify device serial number: 16 bytes; no data in the answer.
            [SetSerialNumber] = command => { SerialNumber = Encoding.ASCII.GetString(command.Data).TrimEnd('\0'); return Answer(command, []); },
            // 82. Inquire the punched card number (T5S): 4 bytes, 0 when no card was punched.
            [InquireCard] = command => Answer(command, BigEndian(Cards.TryDequeue(out var card) ? card : 0, 4)),
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
        [DownloadStaff] = 2, [GetBasicSettings] = 0, [SetBasicSettings] = 10, [GetAdvancedSettings] = 0,
        [SetAdvancedSettings] = 15, [GetDateTime] = 0, [GetTcpParameters] = 0, [SetTcpParameters] = 27, [UploadRecord] = 14,
        [GetDeviceId] = 0, [SetDeviceId] = 4, [InitializeSystem] = 0, [GetTimeZone] = 1, [SetTimeZone] = 29, [GetBells] = 0,
        [SetBell] = 4, [OpenLock] = 0, [GetSerialNumber] = 0, [SetSerialNumber] = 16, [InquireCard] = 0,
    };

    public static Task<FakeAnvizDevice> StartAsync() => Task.FromResult(new FakeAnvizDevice());

    /// <summary>Connects the SDK to this fake.</summary>
    public Task<AnvizDevice> ConnectAsync() => new AnvizManager().Connect("127.0.0.1", Port);

    /// <summary>Completes when the SDK side closes a connection.</summary>
    public Task ClientClosed => clientClosed.Task;

    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public ulong DeviceId { get; set; } = 1;
    public string DeviceTypeCode { get; set; } = "M7A+-N";
    public TimeSpan AnswerDelay { get; set; }
    public List<FakeUser> Users { get; } = [];
    public List<FakeRecord> Records { get; } = [];

    /// <summary>
    /// 1. Get basic settings, 18 bytes: firmware (8, ASCII), communication password (3: length in the high nibble),
    /// sleep time, volume, language, date/time format (bits 7-4 date: 0 Chinese, 1 America, 2 English; bits 3-0 time:
    /// 0 = 24 h, 1 = 12 h), attendance state, language setting flag, command version.
    /// </summary>
    public byte[] BasicSettings { get; } = [.. "V2.1.0\0\0"u8, 0x40, 0x04, 0xD2, 5, 3, 2, 0x20, 1, 0x10, 0x02];

    /// <summary>
    /// 3. Get advanced settings, 15 bytes: FP precision, Wiegand head, Wiegand option, work code, real-time mode,
    /// FP auto update, relay mode, lock delay, memory full alarm (3), repeat attendance delay, door sensor delay,
    /// scheduled bell delay, reserved.
    /// </summary>
    public byte[] AdvancedSettings { get; } = [1, 0x11, 2, 1, 0, 1, 1, 5, 0x00, 0x01, 0xF4, 30, 10, 3, 0];

    /// <summary>
    /// 7. Get TCP/IP parameters, 27 bytes: IP (4), subnet mask (4), MAC (6), gateway (4), server IP (4), far limit,
    /// com port (2), TCP/IP mode (0 server, 1 client), DHCP limit. The document does not give the octet order: first first.
    /// </summary>
    public byte[] TcpParameters { get; } =
        [192, 168, 1, 218, 255, 255, 255, 0, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 192, 168, 1, 1, 192, 168, 1, 10, 0, 0x13, 0x92, 0, 0];

    /// <summary>5. The device's clock, answered by 0x38.</summary>
    public DateTime Clock { get; set; } = new(2026, 10, 25, 14, 30, 15);

    public string SerialNumber { get; set; } = "M7A0123456789";

    /// <summary>Time zones 1-30 by number, 28 bytes each: Monday to Sunday, begin hour, begin minute, end hour, end minute.</summary>
    public Dictionary<byte, byte[]> TimeZones { get; } = [];

    /// <summary>30. Scheduled bells 1-30, 3 bytes each: hour, minute, weekday bits (bits 6-1 Saturday to Monday).</summary>
    public byte[] Bells { get; } = new byte[90];

    /// <summary>The cards punched on the reader, in order; 0x7E answers 0 when none is left.</summary>
    public Queue<uint> Cards { get; } = [];

    /// <summary>Fingerprint templates by user code and backup code (1 = FP1, 2 = FP2).</summary>
    public Dictionary<(ulong UserCode, byte BackupCode), byte[]> Templates { get; } = [];

    private int recordCursor, staffCursor;
    private bool newRecordsOnly;

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

    /// <summary>
    /// Sends a device ping, 0x7F with no data, on the latest connection. Not in the document (its 0x7F is the C5's
    /// "Sending Email"); the SDK takes it as a ping and answers 0xFF.
    /// </summary>
    public Task PingAsync() => latestStream!.WriteAsync(new Frame(DeviceId, 0x7F, null, []).ToBytes(), stop.Token).AsTask();

    /// <summary>
    /// 41. Sent T&amp;A record in real time: the device sends, on the latest connection, an answer-format frame (ACK 0xDF,
    /// RET ACK_SUCCESS, LEN 14) holding one record; "only response message", so the PC sends nothing back.
    /// </summary>
    public Task SendRealTimeRecordAsync(FakeRecord record) =>
        latestStream!.WriteAsync(new Frame(DeviceId, RealTimeRecord + 0x80, AckSuccess, RecordBytes(record)).ToBytes(), stop.Token).AsTask();

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
    internal static byte[] BigEndian(ulong value, int length)
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
    /// 10. Download T&amp;A records: parameter 1 restarts on all records, 2 restarts on the new records only, 0 goes on
    /// (0x10, send the last packet again, is not modelled); record amount &lt;= 25. DATA = N, then N records of 14 bytes
    /// (<see cref="RecordBytes"/>). The document does not say a download clears the new marks: it does not.
    /// </summary>
    private Frame DownloadRecordsAnswer(Frame command)
    {
        var (parameter, amount) = (command.Data[0], command.Data[1]);
        if (parameter is 1 or 2) (recordCursor, newRecordsOnly) = (0, parameter == 2);
        else if (parameter != 0) return NotAnswered(command, $"parameter {parameter}");
        if (amount > 25) return NotAnswered(command, $"record amount {amount} (the document allows 25)");

        var page = (newRecordsOnly ? Records.Where(r => r.New) : Records).Skip(recordCursor).Take(amount).ToList();
        recordCursor += page.Count;
        return Answer(command, [(byte)page.Count, .. page.SelectMany(RecordBytes)]);
    }

    /// <summary>10. / 41. A record, 14 bytes: user code (5), seconds since 2000 (4), backup code, record type, work types (3).</summary>
    private static byte[] RecordBytes(FakeRecord record) =>
    [
        .. BigEndian(record.UserCode, 5),
        .. BigEndian((ulong)(record.DateTime - RecordEpoch).TotalSeconds, 4),
        record.BackupCode,
        record.RecordType,
        .. BigEndian(record.WorkType, 3),
    ];

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
    /// After the second capture the fake holds a made-up 338-byte template for that finger. A first capture for a finger
    /// that already holds a template is ACK_FAIL, as the M7 answers (CHR27-1 S6; Chronos deletes the fingers first).
    /// </summary>
    private Frame EnrollFingerprintAnswer(Frame command)
    {
        var userCode = ReadBigEndian(command.Data.AsSpan(0, 5));
        if (command.Data[6] == 0 && Templates.ContainsKey((userCode, command.Data[5])))
            return Fail(command);
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

    /// <summary>
    /// 24. Clear up records: clear type 0 deletes all records, 1 clears every new-record mark, 2 clears the marks of the
    /// first N new records (N in bytes 2-4); the answer is the amount deleted or cleared (3 bytes).
    /// </summary>
    private Frame ClearRecordsAnswer(Frame command)
    {
        switch (command.Data[0])
        {
            case 0:
                var deleted = Records.Count;
                Records.Clear();
                return Answer(command, BigEndian((ulong)deleted, 3));
            case 1 or 2:
                var limit = command.Data[0] == 1 ? int.MaxValue : (int)ReadBigEndian(command.Data.AsSpan(1, 3));
                var cleared = 0;
                for (var i = 0; i < Records.Count && cleared < limit; i++)
                    if (Records[i].New)
                    {
                        Records[i] = Records[i] with { New = false };
                        cleared++;
                    }
                return Answer(command, BigEndian((ulong)cleared, 3));
            default:
                return NotAnswered(command, $"clear type {command.Data[0]}");
        }
    }

    /// <summary>5. Date and time: year, month, day, hour, minute, second, a byte each. The year's base is not stated: year - 2000.</summary>
    private static byte[] DateTimeBytes(DateTime d) =>
        [(byte)(d.Year - 2000), (byte)d.Month, (byte)d.Day, (byte)d.Hour, (byte)d.Minute, (byte)d.Second];

    /// <summary>
    /// 11. Upload T&amp;A records: one 14-byte record as 10 lays it out (user code, seconds since 2000, backup code,
    /// record type, work types); no data in the answer.
    /// </summary>
    private Frame UploadRecordAnswer(Frame command)
    {
        var data = command.Data.AsSpan();
        Records.Add(new FakeRecord(ReadBigEndian(data[..5]), RecordEpoch.AddSeconds(ReadBigEndian(data[5..9])), data[10]));
        return Answer(command, []);
    }

    /// <summary>17. Modify the device id: 4 bytes; no data in the answer, which still carries the old id.</summary>
    private Frame SetDeviceIdAnswer(Frame command)
    {
        var answer = Answer(command, []);
        DeviceId = ReadBigEndian(command.Data);
        return answer;
    }

    /// <summary>
    /// 26. Get the time zone info: NO. (1 byte; "the total time zone amount is 30", the first number is not stated:
    /// the fake numbers them 1-30), then the 28 bytes. An unset zone is all zero; another number is refused.
    /// </summary>
    private Frame GetTimeZoneAnswer(Frame command) =>
        command.Data[0] is >= 1 and <= 30
            ? Answer(command, TimeZones.GetValueOrDefault(command.Data[0]) ?? new byte[28])
            : Fail(command);

    /// <summary>27. Set time zone info: NO. (1-30 as 26), then the 28 bytes; no data in the answer.</summary>
    private Frame SetTimeZoneAnswer(Frame command)
    {
        if (command.Data[0] is < 1 or > 30) return Fail(command);
        TimeZones[command.Data[0]] = command.Data[1..];
        return Answer(command, []);
    }

    /// <summary>
    /// 31. Set ring info: NO., hour, minute, weekday bits; no data in the answer. 30 bells, the first number not stated:
    /// the fake numbers them 1-30, as the time zones, and refuses another number.
    /// </summary>
    private Frame SetBellAnswer(Frame command)
    {
        var number = command.Data[0];
        if (number is < 1 or > 30) return Fail(command);
        command.Data[1..].CopyTo(Bells, (number - 1) * 3);
        return Answer(command, []);
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
        // InvalidOperationException: the listener stopped before the loop asked for the next connection.
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException or InvalidOperationException) { }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using var _ = client;
        client.NoDelay = true;
        var stream = latestStream = client.GetStream();
        try
        {
            while (true)
            {
                var frame = await ReadFrameAsync(stream);
                if (frame == null) return;
                lock (received) received.Add(frame);
                if (frame.Ret.HasValue) continue; // an answer from the SDK (the pong): the device does not answer it

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
    /// <summary>10. Backup code: 0 FP1, 1 FP2, 2 password, 3 card.</summary>
    public byte BackupCode { get; init; }

    /// <summary>10. Work types, 3 bytes.</summary>
    public ulong WorkType { get; init; }

    /// <summary>Counted in the new record amount (0x3C).</summary>
    public bool New { get; init; }
}
