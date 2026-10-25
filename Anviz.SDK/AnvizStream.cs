using Anviz.SDK.Commands;
using Anviz.SDK.Exceptions;
using Anviz.SDK.Responses;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Anviz.SDK
{
    internal class AnvizStream : IDisposable
    {
        private const int DEVICE_TIMEOUT = 10000; //10 seconds

        private readonly TcpClient DeviceSocket;
        private readonly NetworkStream DeviceStream;
        private readonly Thread ReceiverThread;
        private readonly CancellationTokenSource CancellationTokenSource;

        /// <summary>The call waiting for an answer: its answer code, and the bytes received before its command was written.</summary>
        private sealed class PendingCall
        {
            public PendingCall(byte code, long sentAt)
            {
                Code = code;
                SentAt = sentAt;
            }

            public byte Code { get; }
            public long SentAt { get; }
            // RX.2: the caller never goes on on the reader, where a Dispose would wait for the reader itself.
            public TaskCompletionSource<Response> Answer { get; } = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private PendingCall pending;
        private long received; // bytes the reader has taken from the stream
        private readonly SemaphoreSlim _commandLock = new SemaphoreSlim(1, 1);

        public event EventHandler<Response> ReceivedPacket;
        public event EventHandler<Exception> DeviceError;

        public AnvizStream(TcpClient socket)
        {
            CancellationTokenSource = new CancellationTokenSource();
            DeviceSocket = socket;
            DeviceStream = socket.GetStream();
            DeviceSocket.ReceiveTimeout = DEVICE_TIMEOUT;
            DeviceSocket.SendTimeout = DEVICE_TIMEOUT;
            DeviceStream.WriteTimeout = DEVICE_TIMEOUT;
            DeviceStream.ReadTimeout = DEVICE_TIMEOUT;
            ReceiverThread = new Thread(ReceiverThreadFunc)
            {
                IsBackground = true
            };
            ReceiverThread.Start();
        }

        private async void ReceiverThreadFunc()
        {
            using var _op = AnvizDevice.BeginOperation("DeviceReceiver");
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                Response response = null;
                var frameStart = Interlocked.Read(ref received);
                try
                {
                    response = await Response.FromStream(DeviceStream, CancellationTokenSource.Token, n => Interlocked.Add(ref received, n));
                }
                catch (Exception ex)
                {
                    if (!CancellationTokenSource.IsCancellationRequested) // not the read Dispose broke off
                        DeviceError?.Invoke(this, ex);
                }
                if (response == null)
                {
                    DeviceSocket.Close();
                    Interlocked.Exchange(ref pending, null)?.Answer.TrySetResult(null);
                    break;
                }
                if (response.ResponseCode == 0x7F)
                {
                    await new PongCommand(response.DeviceID).Send(DeviceStream);
                    ReceivedPacket?.Invoke(this, response);
                }
                else if (TakePending(response.ResponseCode, frameStart) is PendingCall call)
                {
                    if (response.ReturnCode == 0)
                        call.Answer.TrySetResult(response);
                    else
                        call.Answer.TrySetException(new AnvizException((AnvizReturnCode)response.ReturnCode));
                }
                else if (response.ReturnCode != 0)
                {
                    // RF.11: an error answer for a code nobody waits for is not the waiting call's; the connection stays.
                    DeviceError?.Invoke(this, new AnvizException((AnvizReturnCode)response.ReturnCode));
                }
                else
                {
                    ReceivedPacket?.Invoke(this, response);
                }
            }
        }

        /// <summary>
        /// The waiting call, taken, when this frame is its answer: the same answer code, and the frame's first byte
        /// came after the command was written (RF.17: a repeat of an earlier answer is not the next call's).
        /// </summary>
        private PendingCall TakePending(byte code, long frameStart)
        {
            var call = Volatile.Read(ref pending);
            return call != null && call.Code == code && frameStart >= call.SentAt
                && Interlocked.CompareExchange(ref pending, null, call) == call ? call : null;
        }

        public async Task<Response> SendCommand(Command cmd)
        {
            if (!DeviceSocket.Connected)
            {
                throw new DeviceConnectionLostException();
            }
            await _commandLock.WaitAsync();
            PendingCall call = null;
            try
            {
                // Bytes already received, or waiting in the socket, before the command is written belong to earlier
                // frames. Available is read first, so a byte the reader takes meanwhile is counted, not missed.
                // ponytail: a repeat still on the wire when the command is written is taken as its answer; the
                // protocol has no sequence number to tell them apart.
                var available = DeviceSocket.Available;
                call = new PendingCall(cmd.ResponseCode, available + Interlocked.Read(ref received));
                Interlocked.Exchange(ref pending, call);
                await cmd.Send(DeviceStream);
                if (await Task.WhenAny(call.Answer.Task, Task.Delay(DEVICE_TIMEOUT)) != call.Answer.Task)
                {
                    if (DeviceSocket.Connected)
                    {
                        DeviceSocket.Close();
                    }
                    throw new DeviceConnectionLostException("Device timeout waiting response.");
                }
                var result = await call.Answer.Task;
                if (result == null)
                {
                    throw new DeviceConnectionLostException();
                }
                return result;
            }
            finally
            {
                Interlocked.CompareExchange(ref pending, null, call);
                _commandLock.Release();
            }
        }

        public void Dispose()
        {
            // RX.2: never waits for the reader, which may be the caller (an event handler); closing the socket ends its
            // read. The token source and the lock stay undisposed: the reader or a waiting call may still touch them.
            CancellationTokenSource.Cancel();
            DeviceStream.Close();
            DeviceSocket.Close();
            Interlocked.Exchange(ref pending, null)?.Answer.TrySetResult(null);
        }
    }
}
