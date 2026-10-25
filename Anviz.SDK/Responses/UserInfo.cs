using Anviz.SDK.Utils;
using System.Collections.Generic;
using System.Text;

namespace Anviz.SDK.Responses
{
    public class UserInfo
    {
        internal static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal const int RECORD_LENGTH30 = 30;
        internal const int RECORD_LENGTH40 = 40;
        internal const int MAX_RECORDS = 8;

        public ulong Id { get; set; }
        public ulong? Password { get; set; }
        public ulong? Card { get; set; }
        public string Name { get; set; }
        public List<Finger> EnrolledFingerprints { get; }
        public byte Department { get; set; }
        public byte Group { get; set; }
        public byte Mode { get; set; }
        public byte PWDH8 { get; set; }
        public byte Keep { get; set; }
        public byte Message { get; set; }

        internal UserInfo(byte[] data, int offset, int datalen = RECORD_LENGTH40)
        {
            log.Debug($"UserInfo: data.Length = {data.Length}, offset = {offset}, datalen = {datalen}");

            try
            {
                Id = Bytes.Read(Bytes.Split(data, offset, 5));
                var pwd = Bytes.Split(data, offset + 5, 3);
                if (pwd[0] == 0xFF && pwd[1] == 0xFF && pwd[2] == 0xFF)
                {
                    Password = null;
                }
                else
                {
                    Password = Bytes.PasswordRead(pwd);
                }
                var card = Bytes.Split(data, offset + 8, 4);
                if (card[0] == 0xFF && card[1] == 0xFF && card[2] == 0xFF && card[3] == 0xFF)
                {
                    Card = null;
                }
                else
                {
                    Card = Bytes.Read(card);
                }
                if (datalen == RECORD_LENGTH30)
                {
                    Name = Bytes.GetAsciiString(Bytes.Split(data, offset + 12, 10));
                    Department = data[offset + 22];
                    Group = data[offset + 23];
                    Mode = data[offset + 24];
                    EnrolledFingerprints = Fingers.DecodeFingers(Bytes.Read(Bytes.Split(data, offset + 25, 2)));
                    PWDH8 = data[offset + 27];
                    Keep = data[offset + 28];
                    Message = data[offset + 29];
                }
                else
                if (datalen == RECORD_LENGTH40)
                {
                    Name = Bytes.GetUnicodeString(Bytes.Split(data, offset + 12, 20));
                    Department = data[offset + 32];
                    Group = data[offset + 33];
                    Mode = data[offset + 34];
                    EnrolledFingerprints = Fingers.DecodeFingers(Bytes.Read(Bytes.Split(data, offset + 35, 2)));
                    PWDH8 = data[offset + 37];
                    Keep = data[offset + 38];
                    Message = data[offset + 39];
                }
            }
            catch (System.Exception ex)
            {
                log.Error("Error parsing UserInfo from data.", ex);
                throw;
            }

        }

        internal byte[] ToArray(int datalen = RECORD_LENGTH40)
        {
            var ret = new byte[datalen];
            Bytes.Write(5, Id).CopyTo(ret, 0);
            Bytes.PasswordWrite(Password).CopyTo(ret, 5);
            if (Card.HasValue)
            {
                Bytes.Write(4, Card.Value).CopyTo(ret, 8);
            }
            else
            {
                ret[8] = 0xFF;
                ret[9] = 0xFF;
                ret[10] = 0xFF;
                ret[11] = 0xFF;
            }
            if (datalen == RECORD_LENGTH30)
            {
                CopyName(Encoding.ASCII.GetBytes(Name), ret, 10);
                ret[22] = Department;
                ret[23] = Group;
                ret[24] = Mode;
                ret[27] = PWDH8;
                ret[28] = Keep;
                ret[29] = Message;
            }
            else if (datalen == RECORD_LENGTH40)
            { 
                CopyName(Encoding.BigEndianUnicode.GetBytes(Name), ret, 20);
                ret[32] = Department;
                ret[33] = Group;
                ret[34] = Mode;
                ret[37] = PWDH8;
                ret[38] = Keep;
                ret[39] = Message;
            }
            return ret;
        }

        // A name longer than its field is cut to the field: it never runs into department, group, mode or FP state.
        private static void CopyName(byte[] name, byte[] record, int field)
        {
            System.Array.Copy(name, 0, record, 12, System.Math.Min(name.Length, field));
        }

        public UserInfo(ulong id, string name)
        {
            Id = id;
            Name = name;
            Department = 1;
            Group = 1;
            Mode = 6;
            EnrolledFingerprints = new List<Finger>();
            PWDH8 = 0xFF;
            Keep = 0xFF;
            Message = 0x40;
        }

        public override string ToString()
        {
            return $"Id: {Id}\r\nName: {Name}\r\nPassword: {Password}\r\nCard: {Card}\r\nFingers: {string.Join(", ", EnrolledFingerprints)}";
        }
    }
}