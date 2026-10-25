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
        private volatile bool closed;
        private readonly SemaphoreSlim _commandLock = new SemaphoreSlim(1, 1); // one command at a time, until its answer
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);   // one frame written at a time

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
            Task.Run(ReceiveAsync);
        }

        private async Task ReceiveAsync()
        {
            using var _op = AnvizDevice.BeginOperation("DeviceReceiver");
            try
            {
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    var frameStart = Interlocked.Read(ref received);
                    var response = await Response.FromStream(DeviceStream, CancellationTokenSource.Token, n => Interlocked.Add(ref received, n));
                    if (response == null)
                    {
                        break; // the connection closed
                    }
                    if (response.ResponseCode == 0x7F)
                    {
                        await WriteAsync(new PongCommand(response.DeviceID));
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
            catch (Exception ex)
            {
                // RX.1: whatever fails here (a bad frame, the pong write, a caller's event handler) is reported and ends
                // the connection; nothing leaves the reader.
                if (!closed) // not the read that Dispose or a timeout broke off
                {
                    try
                    {
                        DeviceError?.Invoke(this, ex);
                    }
                    catch (Exception handlerError)
                    {
                        AnvizDevice.log.Error("A DeviceError handler failed.", handlerError);
                    }
                }
            }
            finally
            {
                Close();
            }
        }

        /// <summary>Ends the connection; the waiting call gets <see cref="DeviceConnectionLostException"/>.</summary>
        private void Close()
        {
            closed = true;
            DeviceStream.Close();
            DeviceSocket.Close();
            Interlocked.Exchange(ref pending, null)?.Answer.TrySetResult(null);
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
            await _commandLock.WaitAsync();
            PendingCall call = null;
            try
            {
                if (closed)
                {
                    throw new DeviceConnectionLostException();
                }
                // Bytes already received, or waiting in the socket, before the command is written belong to earlier
                // frames. Available is read first, so a byte the reader takes meanwhile is counted, not missed.
                // ponytail: a repeat still on the wire when the command is written is taken as its answer; the
                // protocol has no sequence number to tell them apart.
                var available = DeviceSocket.Available;
                call = new PendingCall(cmd.ResponseCode, available + Interlocked.Read(ref received));
                Interlocked.Exchange(ref pending, call);
                if (closed) // closed before the call was armed: Close did not see it
                {
                    throw new DeviceConnectionLostException();
                }
                await WriteAsync(cmd);
                if (await Task.WhenAny(call.Answer.Task, Task.Delay(DEVICE_TIMEOUT)) != call.Answer.Task)
                {
                    Close();
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

        /// <summary>
        /// RX.3: a command and the reader's pong share the stream; the write lock keeps their bytes apart. It is held for
        /// the write only, so the reader never waits for a command's answer.
        /// </summary>
        private async Task WriteAsync(Command cmd)
        {
            await _writeLock.WaitAsync();
            try
            {
                await cmd.Send(DeviceStream);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Dispose()
        {
            // RX.2: never waits for the reader, which may be the caller (an event handler); closing the socket ends its
            // read. The token source and the lock stay undisposed: the reader or a waiting call may still touch them.
            CancellationTokenSource.Cancel();
            Close();
        }
    }
}
