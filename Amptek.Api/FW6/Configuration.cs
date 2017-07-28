using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// Sends a Text Configuration packet
    /// </summary>
    class Configuration : FW6Packet
    {
        public override byte PID1
        {
            get { return 0x20; }
        }

        public override byte PID2
        {
            get { return 0x2; }
        }

        private byte[] data;
        public override byte[] Data
        {
            get { return data; }
        }

        public Configuration(byte[] data)
        {
            this.data = data;
        }
    }
}
