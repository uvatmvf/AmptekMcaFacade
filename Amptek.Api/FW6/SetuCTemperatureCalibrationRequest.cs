using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    class SetuCTemperatureCalibrationRequest : FW6Packet
    {
        private byte uCTempCalibration;

        public SetuCTemperatureCalibrationRequest(byte uCTempCalibration)
        {
            this.uCTempCalibration = uCTempCalibration;
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
                return 0x0C;
            }
        }

        public override byte[] Data
        {
            get
            {
                byte[] data = new byte[1];
                data[0] = uCTempCalibration;
                return data;
            }
        }
    }
}
