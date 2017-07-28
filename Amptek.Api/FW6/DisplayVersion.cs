using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat
{
    // Display the version in Amptek format M.NN.BB
    //
    //    Where M = Major version #
    //          N = Minor version #
    //          B = Build (if available)
    //
    class DisplayVersion
    {
        //public static string VersionString(DPDevice device, Version version, bool isFPGA = false)
        public static string VersionString(DPDevice device, Version version, bool isFPGA)
        {
            try
            {
                //if ((device != null) && isFPGA)
                //{
                //    if (device.FpgaState == DPDevice.FpgaStates.Unprogrammed)
                //    {
                //        return "Unprogrammed";
                //    }
                //    else if (device.FpgaState == DPDevice.FpgaStates.EncryptionFailure)
                //    {
                //        return "Encryption failure";
                //    }
                //}

                if (version.Build > 0)
                {
                    return string.Format("{0}.{1:D2}.{2:D2}", version.Major, version.Minor, version.Build);
                }
                {
                    return string.Format("{0}.{1:D2}", version.Major, version.Minor);
                }
            }
            catch
            {
                return ("");
            }
        }
    }
}
