using System;
using System.Collections.Generic;
using System.Text;

namespace Anviz.SDK.Exceptions
{
    public class DeviceConnectionLostException : Exception
    {
        public DeviceConnectionLostException() : base("Device connection lost.") { }

        public DeviceConnectionLostException(string message) : base(message) { }

        public DeviceConnectionLostException(string message, System.Exception innerException) : base(message, innerException) { }

        public DeviceConnectionLostException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
