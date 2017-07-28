using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// Packet used to perform a uC image switch, also finishes up a uC firmware upload
    /// </summary>
    class uCSwitchToImage : FW6Packet
    {
        public override byte PID1
        {
            get { return 0x30; }
        }

        /// <summary>
        /// Switch to image #1
        /// </summary>
        public override byte PID2
        {
            get { return 0x9; }
        }

        public byte[] data;
        public override byte[] Data
        {
            get { return data; }
        }

        public uCSwitchToImage(UInt16 uploadChecksum)
        {
            data = new byte[4];
            data[0] = 0xA5; // firmware unlock code
            data[1] = 0xF1; // firmware unlock code
            data[2] = (byte)(uploadChecksum >> 8);
            data[3] = (byte)(uploadChecksum & 0xFF);
        }
    }
}
