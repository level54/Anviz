namespace Anviz.SDK.Tests.Fake;

/// <summary>
/// One frame as the protocol document lays it out (Part I):
/// command <c>STX CH CMD LEN DATA CRC16</c>, answer <c>STX CH ACK RET LEN DATA CRC16</c>.
/// STX is 0xA5; CH is the device id, IDHH first; an answer's code (ACK) is the command + 0x80 and carries RET;
/// the CRC16 covers every byte before it and is written CRCL, CRCH.
/// </summary>
public sealed record Frame(ulong DeviceId, byte Code, byte? Ret, byte[] Data)
{
    public const byte Stx = 0xA5;

    /// <summary>Bytes from STX to the end of LEN: 8 for a command, 9 for an answer (its RET byte).</summary>
    public static int HeaderLength(byte code) => code >= 0x80 ? 9 : 8;

    public static Frame Parse(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes[0] != Stx)
            throw new FormatException($"Header is {(bytes.Length == 0 ? "missing" : $"0x{bytes[0]:X2}")}, the document says 0xA5.");
        if (bytes.Length < 6)
            throw new FormatException($"Frame is {bytes.Length} bytes, too short for STX, CH and the code.");
        var code = bytes[5];
        var header = HeaderLength(code);
        if (bytes.Length < header + 2)
            throw new FormatException($"Frame is {bytes.Length} bytes, too short for its header and CRC.");
        // LEN: the document's examples write the high byte first (0x01 0x52 = 338 bytes), as the SDK sends it.
        var length = (bytes[header - 2] << 8) | bytes[header - 1];
        if (bytes.Length != header + length + 2)
            throw new FormatException($"LEN says {length} data bytes, the frame has {bytes.Length - header - 2}.");
        var crc = Crc16(bytes, bytes.Length - 2);
        if (bytes[^2] != (byte)crc || bytes[^1] != (byte)(crc >> 8))
            throw new FormatException($"CRC is {bytes[^2]:X2} {bytes[^1]:X2} (CRCL CRCH), expected {(byte)crc:X2} {(byte)(crc >> 8):X2}.");
        var deviceId = ((ulong)bytes[1] << 24) | ((ulong)bytes[2] << 16) | ((ulong)bytes[3] << 8) | bytes[4];
        return new Frame(deviceId, code, code >= 0x80 ? bytes[6] : null, bytes[header..^2]);
    }

    public byte[] ToBytes()
    {
        var header = Ret.HasValue ? 9 : 8;
        var bytes = new byte[header + Data.Length + 2];
        bytes[0] = Stx;
        bytes[1] = (byte)(DeviceId >> 24);
        bytes[2] = (byte)(DeviceId >> 16);
        bytes[3] = (byte)(DeviceId >> 8);
        bytes[4] = (byte)DeviceId;
        bytes[5] = Code;
        if (Ret.HasValue) bytes[6] = Ret.Value;
        bytes[header - 2] = (byte)(Data.Length >> 8);
        bytes[header - 1] = (byte)Data.Length;
        Data.CopyTo(bytes, header);
        var crc = Crc16(bytes, bytes.Length - 2);
        bytes[^2] = (byte)crc;
        bytes[^1] = (byte)(crc >> 8);
        return bytes;
    }

    /// <summary>
    /// The document names only "CRC16" (CRCL then CRCH) and gives no table or polynomial. This is CRC-16/MCRF4XX
    /// (polynomial 0x1021 reflected = 0x8408, initial 0xFFFF, no final XOR), worked bit by bit so it shares nothing
    /// with the SDK's table.
    /// </summary>
    public static ushort Crc16(byte[] bytes, int count)
    {
        ushort crc = 0xFFFF;
        for (var i = 0; i < count; i++)
        {
            crc ^= bytes[i];
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0x8408) : (ushort)(crc >> 1);
        }
        return crc;
    }
}
