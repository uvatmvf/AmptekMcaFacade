using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    class StartFpgaUpload : FW6Packet
    {
        public override byte[] Data
        {
            get
            {
                byte[] data = new byte[2];
                data[0] = 0x12;
                data[1] = 0x34;
                return data;
            }
        }

        public override byte PID1
        {
            get { return 0x30; }
        }

        public override byte PID2
        {
            get { return 0x1; }
        }
    }
}
