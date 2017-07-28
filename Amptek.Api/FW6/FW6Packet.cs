using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace csRepeat.FW6
{
    /// <summary>
    /// Packet layout looks like:
    /// [SYNC1][SYNC2][PID1][PID2][LEN1][LEN2][data bytes, length][CHECKSUM MSB][CHECKSUM LSB]
    ///
    /// Length is the number of 'Data bytes', and does not include the header or checksum bytes
    /// </summary>
    public abstract class FW6Packet
    {
        /// <summary>
        /// Offset from the start of the packet
        /// </summary>
        public const int PID1Offset = 2;

        /// <summary>
        /// Offset from the start of the packet
        /// </summary>
        public const int PID2Offset = 3;

        /// <summary>
        /// Bytes in the header of the packet
        /// </summary>
        public const int PacketHeaderLength = 2 + 2 + 2; // 2 sync bytes + 2 pid bytes + 2 length bytes

        /// <summary>
        /// Trailing checksum bytes
        /// </summary>
        public const int ChecksumLength = 2;

        public static UInt16 Calculate16bitChecksum(byte[] array, int length)
        {
            UInt16 sum = 0;

            for (int x = 0; x < length; x++)
            {
                sum += array[x];
            }

            return (UInt16)(sum & 0xFFFF);
        }

        public abstract byte PID1
        {
            get;
        }

        public abstract byte PID2
        {
            get;
        }

        /// <summary>
        /// Payload data
        /// </summary>
        public abstract byte[] Data
        {
            get;
        }

        public byte SYNC1
        {
            get
            {
                return 0xF5;
            }
        }

        public byte SYNC2
        {
            get
            {
                return 0xFA;
            }
        }

        public byte[] EncodedPacket
        {
            get
            {
                // build the output packet
                MemoryStream outputMemoryStream = new MemoryStream();
                BinaryWriter outputBinaryWriter = new BinaryWriter(outputMemoryStream);

                outputBinaryWriter.Write(SYNC1);
                outputBinaryWriter.Write(SYNC2);
                outputBinaryWriter.Write(PID1);
                outputBinaryWriter.Write(PID2);

                if (Data == null)
                {
                    outputBinaryWriter.Write((byte)0x0);
                    outputBinaryWriter.Write((byte)0x0);
                }
                else
                {
                    outputBinaryWriter.Write((byte)(Data.Length >> 8)); // length msb
                    outputBinaryWriter.Write((byte)(Data.Length & 0xFF)); // length lsb
                }

                if (Data != null)
                {
                    outputBinaryWriter.Write(Data);
                }

                // calculate the checksum of the packet thus far
                byte[] packetArray = outputMemoryStream.ToArray();
                ushort checksum = Calculate16bitChecksum(packetArray, packetArray.Length);

                // generate the output value
                checksum = (UInt16)((Int16)(-checksum));

                // append the checksum
                outputBinaryWriter.Write((byte)(checksum >> 8));
                outputBinaryWriter.Write((byte)(checksum & 0xFF));

                return outputMemoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(" PID1: {0}, PID2: {1}", PID1, PID2);
        }
    }
}
