using csRepeat;
using csRepeat.FW6;
using System;
using System.Collections.Generic;
using System.Text;
using WinUsb;

namespace Amptek.Api
{
    public class AmptekFacade
    {
        private const String WINUSB_GUID_STRING = "{5A8ED6A1-7FC3-4b6a-A536-95DF35D03448}";
        private static Guid winUsbGuid = new System.Guid(WINUSB_GUID_STRING);
        private const int BAD_DPP_READ_BYTE_NUMBER = 64; // number of bytes returned from calls that mean read failed

        //private static FW6Device GetTypedDevice(DPDevice device)
        //{
        //    FW6Device request = null;
        //    //device.InterfaceType == DPDevice.InterfaceTypes.usb ? (DPDeviceUsb)device :
        //    //    device.InterfaceType == DPDevice.InterfaceTypes.rs232 ? (DPDeviceSerialFW6) device  : 
        //    //    null;

        //    if (device.InterfaceType == DPDevice.InterfaceTypes.usb)
        //    {
        //        request = (DPDeviceUsb)device;
        //    }
        //    else if (device.InterfaceType == DPDevice.InterfaceTypes.rs232)
        //    {
        //        request = (DPDeviceSerialFW6)device;
        //        request.mcaChannels = 1024;     // set default
        //    }
        //    else if (device.InterfaceType == DPDevice.InterfaceTypes.ethernet)
        //    {
        //        request = (DPDeviceEthernet)device;
        //        request.mcaChannels = 1024;     // set default
        //    }
        //    return request;
        //}

        public static List<string> FindAllUsbDevices()
        {
            List<String> devicePathNames;            
            DeviceManagement.FindDeviceFromGuid(winUsbGuid, out devicePathNames);
            return devicePathNames;
        }

        public static List<DPDevice> FindAllDevices()
        {
            return DPDevice.PerformDeviceDetection();
        }

        public static bool PingDevice(DPDevice device)
        {
            if (device is DPDeviceUsb)
            {
                return ((DPDeviceUsb)device).ConnectForTesting();
            }
            return false;
        }

        public static bool GetDevicePresetTime(DPDevice device, 
            ref string setting,
            int maxRetry = 0)
        {
            if (PingDevice(device))
            {
                FW6Device fwDevice = device as FW6Device ;
                //if (device is DPDeviceUsb)
                //{
                //    var usbAt = ((DPDeviceUsb)device);
                    var comm = "PRET=?;";
                    byte[] buffer = null;
                    uint numBytes = 0;
                    bool success = fwDevice.GetConfiguration(comm, ref buffer, out numBytes) && 
                        numBytes >= comm.Length && numBytes < BAD_DPP_READ_BYTE_NUMBER; // success is device read and reasonable number of bytes in return
                    int attempts = 0;
                    while (!success && attempts < maxRetry)
                    { // try to get the configuration on several attempts if fail
                        // upper bound condition of 64 bytes is based on testing, sometimes the device returns a large number of bytes
                        // and has to be re-read
                        success = fwDevice.GetConfiguration(comm, ref buffer, out numBytes); // Get the configuration again
                        attempts++; // increment attempt counter
                    }
                    if (success)
                    {
                        byte[] cfgBuffer = new byte[numBytes];
                        Array.Copy(buffer, cfgBuffer, numBytes);
                        setting = ASCIIEncoding.ASCII.GetString(cfgBuffer);
                    }
                    return success;
                //}
            }
            return false;
        }

        public static bool SetPresetTime(DPDevice device, double preset)
        {
            if (PingDevice(device))
            {
                FW6Device fwDevice = device as FW6Device;
                //if (device is DPDeviceUsb)
                //{
                   // var usbAt = ((DPDeviceUsb)device);
                    return fwDevice.SendConfiguration(string.Format("PRET={0};", preset));
                //}
            }            
            return false;
        }

        public static FW6DppStatus GetStatus(DPDevice device)
        {
            FW6DppStatus DppStatus = new FW6DppStatus();
            if (PingDevice(device))
            {
                //if (device is DPDeviceUsb)
                //{
                    FW6Device deviceDppIO = device as FW6Device;
                    byte[] buffer = null;
                    uint numBytes = 0;
                    if (deviceDppIO.GetRawStatus(ref buffer, out numBytes))
                    {
                        if (numBytes == BAD_DPP_READ_BYTE_NUMBER)
                        {
                            DppStatus.Process_Status(buffer);                            
                        }
                    }
                //}
            }
            return DppStatus;
        }

        public static string GetStatusValueAsText(DPDevice device)
        {
            FW6DppStatus DppStatus = new FW6DppStatus();
            string request = " Get Status failed "; 
            if (PingDevice(device))
            {
                //if (device is DPDeviceUsb)
                //{
                    FW6Device deviceDppIO = (FW6Device)device;
                    byte[] buffer = null;
                    uint numBytes = 0;
                    if (deviceDppIO.GetRawStatus(ref buffer, out numBytes))
                    {
                        if (numBytes == BAD_DPP_READ_BYTE_NUMBER)
                        {
                            DppStatus.Process_Status(buffer);
                            request = DppStatus.GetStatusValueStrings();
                        }
                        else
                        {
                            request = "Status bytes less than 72 -> " + numBytes.ToString();
                        }
                    }

                //}
            }
            return request;
        }

        public static bool PauseDppData(DPDevice device)
        { // wrap pause function call
            if (PingDevice(device))
            {
                //if (device is DPDeviceUsb)
                //{
                    return (device as FW6Device).PauseDppData();
                //}
            }
            return false;
        }

        public static bool ClearDppData(DPDevice device)
        { // wrap function call to clear
            if (PingDevice(device))
            {
                //if (device is DPDeviceUsb)
                //{
                    return (device as FW6Device).ClearDppData();
                //}
            }
            return false;
        }

        public static bool StartDppData(DPDevice device)
        {
            if (PingDevice(device))
            {
                //if (device is DPDeviceUsb)
                //{
                    return (device as FW6Device).StartDppData();
                //}
            }
            return false;
        }

        public static McaReadyEventArgs GetSpectrumData(DPDevice device)
        {
            McaReadyEventArgs request = new McaReadyEventArgs()
            {
                Spectrum = new Spectrum(),
                RequestContext =  null
            }; // created read request object
               // fill spectrum from read
            
            if (device is DPDeviceUsb)
            {
                byte[] reads = null;
                uint bytesRead = 0;
                request.Success = ((device as DPDeviceUsb) as FW6Device).GetSpectrumData(ref reads, out bytesRead);
                request.Spectrum.ReadBuffer = reads;
                request.Spectrum.BytesRead = bytesRead;
                if (request.Success)
                {
                    request.Spectrum.Channels = (int)bytesRead / 3;
                    int idxData = 0;
                    for (int i = 0; i < request.Spectrum.Channels; i++)
                    {
                        idxData = i + 6;
                        request.Spectrum.DataBuffer[i] = (int)(request.Spectrum.ReadBuffer[idxData * 3]) +
                            (int)(request.Spectrum.ReadBuffer[idxData * 3 + 1]) * 256 +
                            (int)(request.Spectrum.ReadBuffer[idxData * 3 + 2]) * 65536;
                    }
                    for (int i = 0; i < request.Spectrum.Channels * .03; i++)
                    { // do not plot upper 3 percent of spectrum
                        request.Spectrum.DataBuffer[(request.Spectrum.Channels - 1) - i] = 0;
                    }
                    // get the status
                    request.Spectrum.Status = AmptekFacade.GetStatus(device);

                }
            }
            return request;
        }
    }

    public class McaReadyEventArgs : EventArgs
    {
        /// <summary>
        /// Data from the spectrum read
        /// </summary>
        public Spectrum Spectrum { get; set; }
        /// <summary>
        /// boolean flag returned from api wrapped function
        /// </summary>
        public bool Success { get; set; }
        public object RequestContext { get; set; }
    }
    public class Spectrum
    {
        public const int MAX_RESOLUTION = 8192;
        public Spectrum()
        {
            DataBuffer = new int[MAX_RESOLUTION];

        }
        public int[] DataBuffer { get; set; }
        public byte[] ReadBuffer { get; set; }
        public uint BytesRead { get; set; }
        public int Channels { get; set; }
        public FW6DppStatus Status { get; set; }
    }
}
