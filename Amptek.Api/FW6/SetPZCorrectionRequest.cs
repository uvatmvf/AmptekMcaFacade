using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    class SetPZCorrectionRequest : FW6Packet
    {
        private byte pzCorrection;

        public SetPZCorrectionRequest(byte pzCorrection)
        {
            this.pzCorrection = pzCorrection;
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
                return 0x0B;
            }
        }

        public override byte[] Data
        {
            get
            {
                byte[] data = new byte[1];
                data[0] = pzCorrection;
                return data;
            }
        }
    }
}
