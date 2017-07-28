using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace csRepeat.FW6
{
    class FW6PacketParser
    {
        private MemoryStream memoryStream;
        private BinaryWriter binaryWriter;

        public enum HandleStates
        {
            NeedMoreData,
            CommandComplete,
            InvalidChecksum
        }

        private const int lengthMsbOffset = 4;
        private const int lengthLsbOffset = 5;

        public FW6PacketParser()
        {
            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream);
        }

        public FW6Packet HandleBytes(out HandleStates state, byte[] data, int dataLength)
        {
            binaryWriter.Write(data, 0, dataLength);

            // do we have enough bytes to determine its overall length?
            byte[] array = memoryStream.ToArray();
            int packetDataLength = 0;
            if (memoryStream.Length > 6)
            {
                packetDataLength = (array[lengthMsbOffset] << 8) + array[lengthLsbOffset];
            }
            else
            {
                state = HandleStates.NeedMoreData;
                return null;
            }

            // do we have all of the bytes?
            int expectedTotalLength = FW6Packet.PacketHeaderLength + packetDataLength + FW6Packet.ChecksumLength;
            if (expectedTotalLength == array.Length)
            {
                // make sure the checksum is valid
                ushort packetChecksum = FW6Packet.Calculate16bitChecksum(array, expectedTotalLength - 2);
                UInt16 checksumEntry = (UInt16)((array[expectedTotalLength - 2] << 8) + array[expectedTotalLength - 1]);
                packetChecksum += checksumEntry;

                if (packetChecksum != 0)
                {
                    state = HandleStates.InvalidChecksum;
                    return null;
                }

                // parse this packet and return it
                byte pid1 = array[FW6Packet.PID1Offset];
                byte pid2 = array[FW6Packet.PID2Offset];

                if (ConfigurationResponse.IsMatch(pid1, pid2))
                {
                    state = HandleStates.CommandComplete;
                    return new ConfigurationResponse(array);
                }
                else if (StatusResponse.IsMatch(pid1, pid2))
                {
                    state = HandleStates.CommandComplete;
                    return new StatusResponse(array);
                }
                else if (AckResponse.IsMatch(pid1, pid2))
                {
                    state = HandleStates.CommandComplete;
                    return new AckResponse(array);
                }
                else if (DiagnosticResponse.IsMatch(pid1, pid2))
                {
                    state = HandleStates.CommandComplete;
                    return new DiagnosticResponse(array);
                }
                else if (SpectrumResponse.IsMatch(pid1, pid2))
                {
                    state = HandleStates.CommandComplete;
                    return new SpectrumResponse(array);
                }
                else
                {
                    throw new System.NotImplementedException(string.Format("unable to parse pid1 {0:x}, pid2 {1:x}",
                                                             pid1, pid2));
                }
            }
            else if (array.Length > expectedTotalLength)
            {
                throw new System.InvalidOperationException("too many bytes");
            }
            else
            {
                state = HandleStates.NeedMoreData;
                return null;
            }
        }
    }
}
