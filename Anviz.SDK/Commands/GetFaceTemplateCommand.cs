using Anviz.SDK.Commands;
using Anviz.SDK.Utils;
using System.Threading.Tasks;

namespace Anviz.SDK.Commands
{
    class GetFaceTemplateCommand : Command
    {
        private const byte GET_FACETEMPLATE = 0x44;
        public GetFaceTemplateCommand(ulong deviceId, ulong employeeID) : base(deviceId)
        {
            var payload = new byte[6];
            Bytes.Write(5, employeeID).CopyTo(payload, 0);
            // M7 deviation (CHR27-1 S6): the document has no face template in 0x44; on the M7, a finger device, this frame returns the FP1 template (338 bytes).
            payload[5] = 1;
            BuildPayload(GET_FACETEMPLATE, payload);
        }
    }
}

namespace Anviz.SDK
{
    public partial class AnvizDevice
    {
        public async Task<byte[]> GetFaceTemplate(ulong employeeID)
        {
            var response = await DeviceStream.SendCommand(new GetFaceTemplateCommand(DeviceId, employeeID));
            return response.DATA;
        }
    }
}