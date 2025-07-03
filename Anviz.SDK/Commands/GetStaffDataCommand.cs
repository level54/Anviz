using Anviz.SDK.Commands;
using Anviz.SDK.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anviz.SDK.Commands
{
    class GetStaffDataCommand : Command
    {
        private const byte GET_STAFF_DATA = 0x72;
        public GetStaffDataCommand(ulong deviceId, bool isFirst, ulong amount) : base(deviceId)
        {
            var kind = (byte)(isFirst ? 1 : 0);
            BuildPayload(GET_STAFF_DATA, new byte[] { kind, (byte)Math.Min(amount, UserInfo.MAX_RECORDS) });
        }
    }
}

namespace Anviz.SDK
{
    public partial class AnvizDevice
    {
        public async Task<List<UserInfo>> GetEmployeesData()
        {
            var statistics = await GetDownloadInformation();
            var userAmount = statistics.UserAmount;
            var users = new List<UserInfo>((int)userAmount);
            var isFirst = true;
            while (userAmount > 0)
            {
                var response = await DeviceStream.SendCommand(new GetStaffDataCommand(DeviceId, isFirst, userAmount));
                var counter = response.DATA[0];

                if (counter == 0)
                {
                    log.Debug($"No user records found on device {DeviceId}.");
                    return users;
                }

                var reclen = (response.DATA.Length - 1) / counter;

                log.Debug($"Received {counter} user records from device {DeviceId}.");
                log.Debug($"response.DATA.Length: {response.DATA.Length}.");
                log.Debug($"UserInfo.RECORD_LENGTH: {reclen}.");

                userAmount -= counter;
                for (var i = 0; i < counter; i++)
                {
                    try
                    {
                        users.Add(new UserInfo(response.DATA, 1 + i * reclen, reclen));
                    }
                    catch (Exception)
                    {
                    }
                }
                isFirst = false;
            }
            return users;
        }
    }
}