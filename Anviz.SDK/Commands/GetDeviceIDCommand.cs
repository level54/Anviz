using Anviz.SDK.Commands;
using Anviz.SDK.Utils;
using System.Threading.Tasks;

namespace Anviz.SDK.Commands
{
    class GetDeviceIDCommand : Command
    {
        private const byte GET_DEVICE_ID = 0x46;
        public GetDeviceIDCommand(ulong deviceId) : base(deviceId)
        {
            BuildPayload(GET_DEVICE_ID, new byte[] { });
        }
    }
}

namespace Anviz.SDK
{
    public partial class AnvizDevice
    {
        [System.Obsolete]
        public async Task<ulong> GetDeviceID()
        {
            var response = await DeviceStream.SendCommand(new GetDeviceIDCommand(DeviceId));
            // M7 (CHR27-1 S6): 0x46 answers the id set in the device menu (0x31353930 on the M7), not the CH it answers
            // with (1); a command sent to that id gets no answer. So the CH from Connect stays.
            return Bytes.Read(response.DATA);
        }
    }
}