using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat.FW6
{
    class StatusRequest : FW6Packet
    {
        public StatusRequest()
        {
        }

        public override byte PID1
        {
            get
            {
                return 0x1;
            }
        }

        public override byte PID2
        {
            get
            {
                return 0x1;
            }
        }

        public override byte[] Data
        {
            get
            {
                return null;
            }
        }
    }

    class ClearSpectrum : FW6Packet
    {
        public ClearSpectrum()
        {
        }

        public override byte PID1
        {
            get
            {
                return 0xF0;
            }
        }

        public override byte PID2
        {
            get
            {
                return 0x1;
            }
        }

        public override byte[] Data
        {
            get
            {
                return null;
            }
        }
    }

    class EnableMCA : FW6Packet
    {
        public EnableMCA()
        {
        }

        public override byte PID1
        {
            get
            {
                return 0xF0;
            }
        }

        public override byte PID2
        {
            get
            {
                return 0x2;
            }
        }

        public override byte[] Data
        {
            get
            {
                return null;
            }
        }
    }

    class DisableMCA : FW6Packet
    {
        public DisableMCA()
        {
        }

        public override byte PID1
        {
            get
            {
                return 0xF0;
            }
        }

        public override byte PID2
        {
            get
            {
                return 0x3;
            }
        }

        public override byte[] Data
        {
            get
            {
                return null;
            }
        }
    }
}
