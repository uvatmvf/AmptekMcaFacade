using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// Diagnostic data request packet
    /// </summary>
    class DiagnosticRequest : FW6Packet
    {
        /// <summary>
        /// No data in a diagnostic data request packet
        /// </summary>
        public override byte[] Data
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Request diagnostic data
        /// </summary>
        public override byte PID1
        {
            get { return 0x3; }
        }

        /// <summary>
        /// Request diagnostic data
        /// </summary>
        public override byte PID2
        {
            get { return 0x5; }
        }
    }
}
