using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    public class AckResponse : FW6Packet
    {
        private byte[] packetContent;

        public AckResponse(byte[] packetContent)
        {
            this.packetContent = packetContent;
        }

        private const byte thisPID1 = 0xFF;

        public override byte PID1
        {
            get { return packetContent[PID1Offset]; }
        }

        public override byte PID2
        {
            get { return packetContent[PID2Offset]; }
        }

        /// <summary>
        /// First byte is address msb
        /// </summary>
        public const int AddressMsbOffset = 0;

        /// <summary>
        /// Second byte is address lsb
        /// </summary>
        public const int AddressLsbOffset = 1;

        /// <summary>
        /// Third byte is record type
        /// </summary>
        public const int RecordTypeOffset = 2;

        /// <summary>
        /// Intel hex record address
        /// Field available if PID2 is UploadAckPacket
        /// </summary>
        public int Address
        {
            get
            {
                // field is only valid if given the specific type
                if (AckType != AckTypes.UploadAckPacket)
                {
                    throw new System.InvalidOperationException();
                }

                return (Data[AddressMsbOffset] << 8) + Data[AddressLsbOffset];
            }
        }

        public override byte[] Data
        {
            get
            {
                byte[] returnValue = new byte[packetContent.Length - PacketHeaderLength];
                Array.Copy(packetContent, PacketHeaderLength, returnValue, 0, returnValue.Length);
                return returnValue;
            }
        }

        public enum AckTypes
        {
            Ok = 0x0,
            SyncError,
            PidError,
            LenError,
            ChecksumError,
            BadParameter,
            BadHexRecord,
            UnrecognizedCommand,
            FpgaError,
            Cp2201NotFound,
            ScopeDataNotAvailable,
            Pc5NotPresent,
            OkInterfaceSharingRequest,
            BusyAnotherInterfaceIsInUse,
            I2cError,
            UploadAckPacket = 0x0F
        }

        public AckTypes AckType
        {
            get
            {
                return (AckTypes)PID2;
            }
        }

        public static bool IsMatch(byte pid1, byte pid2)
        {
            if (thisPID1 == pid1)
            {
                return true;
            }

            return false;
        }
    }
}
