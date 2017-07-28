using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// v2 uC data packet, requires firmware newer than v6.06.04
    /// </summary>
    class uCDataPacketv2 : FW6Packet
    {
        public override byte PID1
        {
            get { return 0x30; }
        }

        public override byte PID2
        {
            get { return 0x0C; }
        }

        private byte[] data;
        public override byte[] Data
        {
            get { return data; }
        }

        public uCDataPacketv2(byte[] data)
        {
            this.data = data;
        }
    }
}
