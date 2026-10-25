using Anviz.SDK.Commands;
using Anviz.SDK.Exceptions;
using Anviz.SDK.Responses;
using System;
using System.IO;
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
                        // RX.14: a pong write stalled with no command running would hold the reader for ever; at the
                        // limit it is raised as DeviceError and the connection closes (it is gone).
                        var pong = WriteAsync(new PongCommand(response.DeviceID));
                        if (!pong.IsCompleted && !await WrittenBefore(pong, Task.Delay(DEVICE_TIMEOUT)))
                        {
                            throw new TimeoutException("Device timeout writing the pong.");
                        }
                        await pong;
                        ReceivedPacket?.Invoke(this, response);
                    }
                    else if (TakePending(response.ResponseCode, frameStart) is PendingCall call)
                    {
                        if (response.ReturnCode == 0)
                            call.Answer.TrySetResult(response);
                        else
                            call.Answer.TrySetException(new AnvizException((AnvizReturnCode)response.ReturnCode));
                    }
                    else
                    {
                        // RF.11, RF.17: an answer nobody waits for, an error answer too, is not the waiting call's; it is
                        // raised as a packet and the connection stays (DeviceError means the connection is gone).
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
                // RF.17: bytes the reader has counted, or waiting in the socket, before the command is written belong to
                // earlier frames. The count is read first, so a byte the reader takes in between is never counted twice
                // (twice would make the real answer look earlier than the command, and the call would time out).
                // ponytail: a repeat whose bytes the reader has read but not yet counted, or that is still on the wire,
                // when the command is written is taken as its answer; the protocol has no sequence number to tell them apart.
                var counted = Interlocked.Read(ref received);
                call = new PendingCall(cmd.ResponseCode, counted + DeviceSocket.Available);
                Interlocked.Exchange(ref pending, call);
                if (closed) // closed before the call was armed: Close did not see it
                {
                    throw new DeviceConnectionLostException();
                }
                // One limit for the write and the answer: a write stalled in the socket, or behind a stalled pong, ends
                // the connection too, so it cannot hold the pong and the reader for ever.
                var timeout = Task.Delay(DEVICE_TIMEOUT);
                if (!await WrittenBefore(WriteAsync(cmd), timeout))
                {
                    Close();
                    throw new DeviceConnectionLostException("Device timeout waiting response.");
                }
                if (await Task.WhenAny(call.Answer.Task, timeout) != call.Answer.Task)
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
            catch (Exception ex) when (ex is IOException || (closed && ex is ObjectDisposedException))
            {
                // The write failed: a Close between the closed checks and the write, or the device reset the connection
                // before the reader saw it. The socket is gone, as for any lost connection.
                Close();
                throw new DeviceConnectionLostException("Device connection lost.", ex);
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

        /// <summary>
        /// RX.14: an async socket write has no timeout of its own (WriteTimeout covers synchronous writes only). False when
        /// <paramref name="write"/> has not ended by <paramref name="timeout"/>; the write left behind fails once the
        /// connection closes, and that failure is observed here.
        /// </summary>
        private static async Task<bool> WrittenBefore(Task write, Task timeout)
        {
            if (await Task.WhenAny(write, timeout) != write)
            {
                _ = write.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                return false;
            }
            await write;
            return true;
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
