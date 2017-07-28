using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    class SetDCalRequest : FW6Packet
    {
        private ushort dcalValue;

        public SetDCalRequest(ushort dcalValue)
        {
            this.dcalValue = dcalValue;
        }

        public override byte PID1
        {
            get
            {
                return 0xF0;
            }
        }

        public override byte PID2
        {
            get
            {
                return 0x0A;
            }
        }

        public override byte[] Data
        {
            get
            {
                byte[] data = new byte[2];
                data[0] = (byte)(dcalValue >> 8);
                data[1] = (byte)(dcalValue & 0xFF);
                return data;
            }
        }
    }
}
