using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    /// <summary>
    /// Sends a Text Configuration Readback Request
    /// </summary>
    class ConfigurationReadback : FW6Packet
    {
        public override byte PID1
        {
            get { return 0x20; }
        }

        public override byte PID2
        {
            get { return 0x3; }
        }

        private byte[] data;
        public override byte[] Data
        {
            get { return data; }
        }

        public ConfigurationReadback(byte[] data)
        {
            this.data = data;
        }
    }
}
