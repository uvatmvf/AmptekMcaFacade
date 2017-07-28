using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// Diagnostic data response packet
    /// </summary>
    public class DiagnosticResponse : FW6Packet
    {
        private byte[] packetContent;

        public override byte[] Data
        {
            get { throw new NotImplementedException(); }
        }

        private int FirmwareVersionOffset = 0;

        public int FirmwareMajor
        {
            get
            {
                return packetContent[FirmwareVersionOffset] >> 8;
            }
        }

        public int FirmwareMinor
        {
            get
            {
                return packetContent[FirmwareVersionOffset] & 0xF;
            }
        }

        private int FpgaVersionOffset = 1;

        public int FpgaMajor
        {
            get
            {
                return packetContent[FpgaVersionOffset] >> 8;
            }
        }

        public int FpgaMinor
        {
            get
            {
                return packetContent[FpgaVersionOffset] & 0xF;
            }
        }

        private int DCalSettingOffset = 177;

        public UInt16 DCalSetting
        {
            get
            {
                return (UInt16)((packetContent[DCalSettingOffset] << 8) | packetContent[DCalSettingOffset + 1]);
            }
        }

        private int pzCorrectionOffset = 179;

        public byte pzCorrection
        {
            get
            {
                return packetContent[pzCorrectionOffset];
            }
        }

        private int uCTemperatureCalibrationOffset = 180;

        public byte uCTemperatureCalibration
        {
            get
            {
                return packetContent[uCTemperatureCalibrationOffset];
            }
        }

        private int ADCGainCalibrationOffset = 181;

        public byte ADCGainCalibration
        {
            get
            {
                return packetContent[ADCGainCalibrationOffset];
            }
        }

        private int ADCOffsetCalibrationOffset = 182;

        public byte ADCOffsetCalibration
        {
            get
            {
                return packetContent[ADCOffsetCalibrationOffset];
            }
        }

        private const byte thisPID1 = 0x82;
        private const byte thisPID2 = 0x5;

        public override byte PID1
        {
            get { return thisPID1; }
        }

        public override byte PID2
        {
            get { return thisPID2; }
        }

        public static bool IsMatch(byte pid1, byte pid2)
        {
            if (thisPID1 == pid1)
            {
                return true;
            }

            return false;
        }

        public DiagnosticResponse(byte[] packetContent)
        {
            this.packetContent = packetContent;
        }
    }
}
