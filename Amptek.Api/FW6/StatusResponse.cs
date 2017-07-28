using System;
using System.Collections.Generic;
using System.Text;
using MiscUtil.Conversion;

namespace csRepeat.FW6
{
    public class StatusResponse : FW6Packet
    {
        private byte[] packetContent;

        private int FwVersionMajorMinorOffset = 24;
        public int FwVersionMajor
        {
            get
            {
                return (packetContent[PacketHeaderLength + FwVersionMajorMinorOffset] >> 4) & 0xF;
            }
        }

        public int FwVersionMinor
        {
            get
            {
                return packetContent[PacketHeaderLength + FwVersionMajorMinorOffset] & 0xF;
            }
        }

        private int FwBuildNumberOffset = 37;
        public int FwBuildNumber
        {
            get
            {
                return (packetContent[PacketHeaderLength + FwBuildNumberOffset] & 0x0F);
            }
        }

        private int FpgaMajorMinorVersionOffset = 25;
        public int FpgaVersionMajor
        {
            get
            {
                return (packetContent[PacketHeaderLength + FpgaMajorMinorVersionOffset] >> 4) & 0xF;
            }
        }

        public int FpgaVersionMinor
        {
            get
            {
                return packetContent[PacketHeaderLength + FpgaMajorMinorVersionOffset] & 0xF;
            }
        }

        private int SerialNumberOffset = 26;
        public uint SerialNumber
        {
            get
            {
                return EndianBitConverter.Little.ToUInt32(packetContent, PacketHeaderLength + SerialNumberOffset);
            }
        }

        private int DeviceTypeOffset = 39;
        public DeviceTypes DeviceType
        {
            get
            {
                return (DeviceTypes)packetContent[PacketHeaderLength + DeviceTypeOffset];
            }
        }

        private const byte thisPID1 = 0x80;
        private const byte thisPID2 = 0x1;

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
                byte[] returnValue = new byte[packetContent.Length - (PacketHeaderLength + ChecksumLength)];
                Array.Copy(packetContent, PacketHeaderLength, returnValue, 0, returnValue.Length);
                return returnValue;
            }
        }

        public StatusResponse(byte[] packetContent)
        {
            this.packetContent = packetContent;
        }

        public static bool IsMatch(byte pid1, byte pid2)
        {
            if((thisPID1 == pid1) && (thisPID2 == 0x1))
            {
                return true;
            }

            return false;
        }
    }
}
