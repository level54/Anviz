using System;

namespace Anviz.SDK.Exceptions
{
    public enum AnvizReturnCode
    {
        Fail = 0x01,
        Full = 0x04,
        Empty = 0x05,
        NoUser = 0x06,
        Timeout = 0x08,
        UserOccupied = 0x0A,
        FingerOccupied = 0x0B,
    }

    public class AnvizException : Exception
    {
        public AnvizReturnCode ReturnCode { get; }

        public AnvizException(AnvizReturnCode code)
            : base($"Device returned error: {code}")
        {
            ReturnCode = code;
        }
    }
}
