using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// Encompases the several kinds of spectrum responses that can be returned from a spectrum request
    /// NOTE: The spectrum responses with status are handled elsewhere as they have a status field that
    /// the responses handled here do not have
    /// </summary>
    public class SpectrumResponse : FW6Packet
    {
        private const byte thisPID1 = 0x81;
        private byte thisPID2 = 0x5;
        private byte[] packetContent;

        /// <summary>
        /// The possible spectrum responses
        /// NOTE: The type is a single byte thats why it is defined as 'byte'
        /// </summary>
        public enum SpectrumResponseTypePids : byte
        {
            Channel256Spectrum = 0x01,
            Channel512Spectrum = 0x3,
            Channel1024Spectrum = 0x5,
            Channel2048Spectrum = 0x7,
            Channel4096Spectrum = 0x9,
            Channel8192Spectrum = 0xB
        }

        public override byte PID1
        {
            get { return thisPID1; }
        }

        /// <summary>
        /// See SpectrumResponseTypePids as this command maps
        /// to multiple responses
        /// </summary>
        public override byte PID2
        {
            get
            {
                byte returnValue = packetContent[3];
                thisPID2 = packetContent[3];
                return returnValue;
            }
        }

        public override byte[] Data
        {
            get
            {
                byte[] returnValue = new byte[packetContent.Length - (PacketHeaderLength+ChecksumLength)];
                Array.Copy(packetContent, PacketHeaderLength, returnValue, 0, returnValue.Length);
                return returnValue;
            }
        }

        public SpectrumResponse(byte[] packetContent)
        {
            this.packetContent = packetContent;
        }

        public static bool IsMatch(byte pid1, byte pid2)
        {
            // if this is the correct command, check if the sub command is one of the possibilities
            if(thisPID1 == pid1)
            {
                // see if the value is one of those in the type of spectrum responses that we expect
                if(Enum.IsDefined(typeof(SpectrumResponseTypePids), pid2))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
