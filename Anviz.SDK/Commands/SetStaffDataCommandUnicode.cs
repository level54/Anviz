using Anviz.SDK.Commands;
using Anviz.SDK.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anviz.SDK.Commands
{
    class SetStaffDataCommandUnicode : Command
    {
        private const byte SET_STAFF_DATA = 0x73;
        public SetStaffDataCommandUnicode(ulong deviceId, List<UserInfo> users) : base(deviceId)
        {
            var userAmount = (byte)Math.Min(users.Count, UserInfo.MAX_RECORDS);
            var payload = new byte[1 + userAmount * UserInfo.RECORD_LENGTH40];
            payload[0] = userAmount;
            for (var i = 0; i < userAmount; i++)
            {
                users[i].ToArray().CopyTo(payload, 1 + i * UserInfo.RECORD_LENGTH40);
            }
            users.RemoveRange(0, userAmount);
            BuildPayload(SET_STAFF_DATA, payload);
        }
    }
}

namespace Anviz.SDK
{
    public partial class AnvizDevice
    {
        public async Task SetEmployeesDataUnicode(UserInfo user)
        {
            await SetEmployeesDataUnicode(new List<UserInfo> { user });
        }

        public async Task SetEmployeesDataUnicode(List<UserInfo> users)
        {
            while (users.Count > 0)
            {
                await DeviceStream.SendCommand(new SetStaffDataCommandUnicode(DeviceId, users));
            }
        }
    }
}