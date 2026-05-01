using System;
using System.ComponentModel;

namespace Anviz.SDK.Exceptions
{
    public enum AnvizReturnCode
    {
        [Description("General failure")]
        Fail = 0x01,
        [Description("User memory full")]
        Full = 0x04,
        [Description("User memory empty")]
        Empty = 0x05,
        [Description("User does not exist")]
        NoUser = 0x06,
        [Description("Fingerprint capture timeout")]
        Timeout = 0x08,
        [Description("User already exists")]
        UserOccupied = 0x0A,
        [Description("Fingerprint already registered")]
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
