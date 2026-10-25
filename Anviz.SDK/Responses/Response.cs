using Anviz.SDK.Exceptions;
using Anviz.SDK.Utils;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Anviz.SDK.Responses
{
    public class Response
    {
        enum RetVal
        {
            SUCCESS = 0x00, // operation successful
            FAIL = 0x01, // operation failed
            FULL = 0x04, // user full
            EMPTY = 0x05, // user empty
            NO_USER = 0x06, // user not exist
            TIME_OUT = 0x08, // capture timeout
            USER_OCCUPIED = 0x0A, // user already exists
            FINGER_OCCUPIED = 0x0B, // fingerprint already exists
        }

        public ulong DeviceID { get; }

        public byte[] DATA { get; }

        public byte ResponseCode { get; }

        /// <summary>RET of an answer (0 = success); 0 for a frame the device starts, which has no RET.</summary>
        internal byte ReturnCode { get; }

        internal Response(byte[] data, ulong deviceId, byte ACK, byte ret)
        {
            DATA = data;
            DeviceID = deviceId;
            ResponseCode = ACK;
            ReturnCode = ret;
        }

        /// <param name="bytesRead">Told the size of every read from the stream (the reader counts the bytes it took).</param>
        internal static async Task<Response> FromStream(NetworkStream stream, CancellationToken ct, Action<int> bytesRead)
        {
            var data = new byte[16000]; //for facepass7 template size is 15360
            try
            {
                await ReadAsyncContinuously(stream, data, 0, 6, ct, bytesRead);
            }
            catch (Exception)
            {
                return null;
            }

            var STX = data[0];
            if (STX != 0xA5)
            {
                throw new Exception("Invalid header");
            }
            var CH = Bytes.Split(data, 1, 4);
            var ACK = data[5];

            // A frame the device starts (code below 0x80) has no RET: LEN is bytes 6-7. An answer has RET at byte 6
            // and LEN at bytes 7-8.
            var base_offset = ACK < 0x80 ? 8 : 9;
            await ReadAsyncContinuously(stream, data, 6, base_offset - 6, ct, bytesRead);
            var LEN = (int)Bytes.Read(Bytes.Split(data, base_offset - 2, 2));

            // Read the frame to its end (an error answer too: LEN 0, then CRC) so the next frame starts in step.
            await ReadAsyncContinuously(stream, data, base_offset, LEN + 2, ct, bytesRead);

            var PacketData = Bytes.Split(data, base_offset, LEN);
            var CRC = Bytes.Split(data, LEN + base_offset, 2);
            var ComputedCRC = (CRC[1] << 8) + CRC[0];
            var ExpectedCRC = CRC16.Compute(data, LEN + base_offset);
            if (ComputedCRC != ExpectedCRC)
            {
                throw new Exception("Invalid CRC");
            }

            // An error answer is returned too: the reader gives it to the call waiting for its code (RF.11).
            var ret = ACK >= 0x80 ? data[6] : (byte)RetVal.SUCCESS;
            return new Response(PacketData, Bytes.Read(CH), ACK, ret);
        }

        private static async Task ReadAsyncContinuously(NetworkStream stream, byte[] data, int offset, int size, CancellationToken ct, Action<int> bytesRead)
        {
            int readed = 0;
            do
            {
                var amount = await stream.ReadAsync(data, offset + readed, size - readed, ct);
                if (amount == 0)
                {
                    throw new Exception("Partial packet read");
                }
                bytesRead?.Invoke(amount);
                readed += amount;
            } while (readed < size);
        }
    }
}
