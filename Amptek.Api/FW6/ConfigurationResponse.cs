using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// Configuration response that is returned from a configuration readback request
    /// </summary>
    public class ConfigurationResponse : FW6Packet
    {
        private const byte thisPID1 = 0x82;
        private const byte thisPID2 = 0x07;

        private byte[] packetContent;

        public override byte PID1
        {
            get { return thisPID1; }
        }

        public override byte PID2
        {
            get { return thisPID2; }
        }

        public override byte[] Data
        {
            get
            {
                byte[] returnValue = new byte[packetContent.Length - (PacketHeaderLength+ChecksumLength)];
                Array.Copy(packetContent, PacketHeaderLength, returnValue, 0, returnValue.Length);
                return returnValue;
            }
        }

        public ConfigurationResponse(byte[] packetContent)
        {
            this.packetContent = packetContent;
        }

        public static bool IsMatch(byte pid1, byte pid2)
        {
            if ((thisPID1 == pid1) && (thisPID2 == pid2))
            {
                return true;
            }

            return false;
        }
    }
}
