using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// Request device spectrum data.
    /// </summary>
    public class SpectrumRequest : FW6Packet
    {
        public SpectrumRequest()
        {
        }

        /// <summary>
        /// PID1
        /// </summary>
        public override byte PID1
        {
            get
            {
                return 0x2;
            }
        }

        /// <summary>
        /// PID2
        /// </summary>
        public override byte PID2
        {
            get
            {
                return 0x1;
            }
        }

        /// <summary>
        /// Packet data
        /// </summary>
        public override byte[] Data
        {
            get
            {
                return null;
            }
        }
    }
}
