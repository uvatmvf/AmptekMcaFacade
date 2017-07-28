using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    class SetADCCalibrationRequest : FW6Packet
    {
        private byte adcGain;
        private byte adcOffset;

        public SetADCCalibrationRequest(byte adcGain, byte adcOffset)
        {
            this.adcGain = adcGain;
            this.adcOffset = adcOffset;
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
                return 0x0E;
            }
        }

        public override byte[] Data
        {
            get
            {
                byte[] data = new byte[2];
                data[0] = adcGain;
                data[1] = adcOffset;
                return data;
            }
        }
    }
}
