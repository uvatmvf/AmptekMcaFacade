using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using WinUsb;

namespace csRepeat.FW6
{
    //    internal class MessageHandler : NativeWindow
    //    {
    ////        public event EventHandler<MessageData> MessageReceived;

    //        public MessageHandler()
    //        {
    //            CreateHandle(new CreateParams());
    //        }

    //        protected override void WndProc(ref Message msg)
    //        {
    //            // filter messages here for your purposes
    ////            EventHandler<MessageData> = MessageReceived;
    ////            if (handler != null) handler(ref msg);
    //			try
    //			{
    //				// The OnDeviceChange routine processes WM_DEVICECHANGE messages.

    //				if (msg.Msg == DeviceManagement.WM_DEVICECHANGE)
    //				{
    //                    Console.WriteLine("on change");
    //					//OnDeviceChange(m);
    //				}
    //				// Let the base form process the message.

    //				base.WndProc(ref msg);
    //			}
    //			catch (Exception ex)
    //			{
    //                string strEx;
    //                strEx = ex.Message;
    //                throw;
    //            }
    //        }
    //    }

    public class DPDeviceUsb : FW6Device
    {
        /// <summary>
        /// NOTE: This guid string doesn't appear to be present in any of the device properties
        /// listed in the Windows device manager, not sure where this value comes from
        /// </summary>
        private const String WINUSB_GUID_STRING = "{5A8ED6A1-7FC3-4b6a-A536-95DF35D03448}";

        private WinUsbDevice theWinUsbDevice;
        private string DevicePath;

        protected override int DataPacketAckTimeoutMS
        {
            get
            {
                return 200;
            }
        }

        private int _mcaChannels;
        public override int mcaChannels
        {
            get { return _mcaChannels; }
            set { _mcaChannels = value; }
        }
        
        public DPDeviceUsb(DeviceTypes deviceType,
                           Version firmwareVersion,
                           Version fpgaVersion,
                           string serialNumber,
                           string devicePath)
            : base(InterfaceTypes.usb, deviceType,
                   firmwareVersion,
                   fpgaVersion,
                   serialNumber)
        {
            DevicePath = devicePath;

            // update fpga state, have to call this here because performing it in
            // FW6Device() constructor meas it would happen before
            // the code here so internal state storage wouldn't be set up
            UpdateFpgaState();
        }

        ~DPDeviceUsb()
        {
            Disconnect();
        }

        //  Convert the device interface GUID String to a GUID object: 
        private static Guid winUsbGuid = new System.Guid(WINUSB_GUID_STRING);

        private DeviceManagement myDeviceManagement = new DeviceManagement();

        // Create control to handle windows messages
        //private MessageHandler messageHandler = new MessageHandler();

#if false
        // Notification handle returned by DeviceManagement.RegisterForDeviceNotifications
        private IntPtr deviceNotificationHandle = IntPtr.Zero;
#endif

        /// <summary>
        ///
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>True if sending was successful, false if not</returns>
        public bool SendCommand(FW6Packet packet)
        {
            byte[] writeData = new byte[packet.EncodedPacket.Length];
            Array.Copy(packet.EncodedPacket, writeData, writeData.Length);
            bool success = theWinUsbDevice.SendViaBulkTransfer(ref writeData,
                                                                          (uint)writeData.Length);
            return success;
        }

        public bool ReadData(ref byte[] readBuffer, out uint bytesRead)
        {
            bytesRead = 0;
            bool success = false;
            theWinUsbDevice.ReadViaBulkTransfer(System.Convert.ToByte(theWinUsbDevice.myDevInfo.bulkInPipe),
                                                        (uint)readBuffer.Length,
                                                        ref readBuffer,
                                                        ref bytesRead,
                                                        ref success);

            return success;
        }

        public override bool ConnectForTesting()
        {
            bool success = false;
            Connect();
            StatusRequest statusRequest = new FW6.StatusRequest();
            success = SendCommand(statusRequest);
            if (!success)
            {
                Disconnect();
            }
            return success;
        }

        public override void DisconnectForTesting()
        {
            Disconnect();
        }

        public override bool GetRawStatus(ref byte[] readBuffer, out uint bytesRead)
        {
            bool success = false;
            
            readBuffer = new byte[512];
            bytesRead = 0;
            Connect();
            StatusRequest statusRequest = new FW6.StatusRequest();
            success = SendCommand(statusRequest);
            if (!success)
            {
                Disconnect();
            }
            else
            {
                // read the response
                success = ReadData(ref readBuffer, out bytesRead);
                if (success)
                {
                    if (bytesRead == 72)
                    {
                        for (int idx = 0; idx < 64; idx++)
                        {
                            readBuffer[idx] = readBuffer[idx+6];
                        }
                        for (int idx = 64; idx < 72; idx++)
                        {
                            readBuffer[idx] = 0;
                        }
                        bytesRead = 64;
                    }
                }
            }
            return success;
        }

        public override string GetFullConfiguration(byte DppType, bool PC5_PRESENT)
        {
            bool success = false;
            string strCommands = GetFullReadBackCmdString(DppType, PC5_PRESENT);
            byte[] readBuffer = new byte[520];
            uint bytesRead = 0;
            byte[] cfgCommands = Encoding.ASCII.GetBytes(strCommands);
            string strCmdReadback = "";
            Connect();
            ConfigurationReadback cfgReadback = new FW6.ConfigurationReadback(cfgCommands);
            success = SendCommand(cfgReadback);
            if (!success)
            {
                Disconnect();
            }
            else
            {
                success = ReadData(ref readBuffer, out bytesRead);
                if (success)
                {
                    byte[] cfgBuffer = new byte[bytesRead - 8];
                    for (int idxCfg = 0; idxCfg < (bytesRead - 8); idxCfg++)
                    {
                        cfgBuffer[idxCfg] = readBuffer[idxCfg + 6];
                    }
                    strCmdReadback = ASCIIEncoding.ASCII.GetString(cfgBuffer);
                    return strCmdReadback;
                }
            }
            return "";
        }

        public override string DisplayConfiguration(string strCommands)
        {
            string strDisplay = "";
            string strChar = "";
            for (int idxChar = 0; idxChar < strCommands.Length; idxChar++)
            {
                strChar = strCommands.Substring(idxChar, 1);
                strDisplay += strChar;
                if (strChar == ";")
                {
                    strDisplay += "\r\n";
                }
            }
            return strDisplay;
        }

        public override bool GetConfiguration(string strCommands, ref byte[] readBuffer, out uint bytesRead)
        {
            bool success = false;

            readBuffer = new byte[520];
            bytesRead = 0;
            if (strCommands.Length == 0)
            {
                return success;
            }
            byte[] cfgCommands = Encoding.ASCII.GetBytes(strCommands);
            Connect();
            ConfigurationReadback cfgReadback = new FW6.ConfigurationReadback(cfgCommands);
            success = SendCommand(cfgReadback);
            //Application.DoEvents();
            Thread.Sleep(100);


            if (!success)
            {
                Disconnect();
            }
            else
            {
                success = ReadData(ref readBuffer, out bytesRead);
                if (success)
                {
                    bytesRead -= 8;
                    for (int idx = 0; idx < bytesRead; idx++)
                    {
                        readBuffer[idx] = readBuffer[idx + 6];
                    }
                    for (int idx = ((int)bytesRead); idx < ((int)bytesRead + 8); idx++)
                    {
                        readBuffer[idx] = 0;
                    }
                }
            }
            return success;
        }

        public override bool SendConfiguration(string strCommands)
        {
            bool success = false;
            if (strCommands.Length == 0)
            {
                return success;
            }
            byte[] cfgCommands = Encoding.ASCII.GetBytes(strCommands);
            Connect();
            Configuration cfgSend = new FW6.Configuration(cfgCommands);
            success = SendCommand(cfgSend);
            return success;
        }

        public override bool ClearDppData()
        {
            bool success = false;
            Connect();
            ClearSpectrum dppRequest = new FW6.ClearSpectrum();
            success = SendCommand(dppRequest);
            return success;
        }

        public override bool StartDppData()
        {
            bool success = false;
            Connect();
            EnableMCA dppRequest = new FW6.EnableMCA();
            success = SendCommand(dppRequest);
            return success;
        }

        public override bool PauseDppData()
        {
            bool success = false;
            Connect();
            DisableMCA dppRequest = new FW6.DisableMCA();
            success = SendCommand(dppRequest);
            return success;
        }

        public override bool GetSpectrumData(ref byte[] readBuffer, out uint bytesRead)
        {
            bool success = false;

            readBuffer = new byte[24648];
            bytesRead = 0;
            Connect();
            SpectrumRequest spectrumRequest = new FW6.SpectrumRequest();
            success = SendCommand(spectrumRequest);
            //Application.DoEvents();
            Thread.Sleep(10);
            if (!success)
            {
                Disconnect();
            }
            else
            {
                success = ReadData(ref readBuffer, out bytesRead);
                if (success)
                {
                    bytesRead -= 8;
                    for (int idx = 0; idx < bytesRead; idx++)
                    {
                        readBuffer[idx] = readBuffer[idx + 6];
                    }
                    for (int idx = ((int)bytesRead); idx < ((int)bytesRead + 8); idx++)
                    {
                        readBuffer[idx] = 0;
                    }
                }

            }
            return success;
        }

        public static List<DPDevice> DetectDevices()
        {
            List<DPDevice> devices = new List<DPDevice>();
            List<String> devicePathNames;

            // Fill an array with the device path names of all attached devices with matching GUIDs.
            bool devicesFound = DeviceManagement.FindDeviceFromGuid(winUsbGuid, out devicePathNames);

            if(devicesFound)
            {
                foreach (string devicePathName in devicePathNames)
                {
                    // attempt to connect to this device
                    DPDeviceUsb newDevice;

                    try
                    {
                        newDevice = new DPDeviceUsb(DeviceTypes.DP5, new Version(0, 0), new Version(0, 0), null, devicePathName);
                        newDevice.Connect();
                    }
                    catch
                    {
                        return null;
                    }

                    //Commented out due to unreliable response from WinUsb_QueryDeviceInformation.                            
                    //DisplayDeviceSpeed();
                    //Application.DoEvents();
                    Thread.Sleep(20);

                    // build the command
                    StatusRequest statusRequest = new FW6.StatusRequest();
                    bool success = newDevice.SendCommand(statusRequest);

                    // send the command to the device
                    if (!success)
                    {
                        newDevice.Disconnect();
                        return null;
                    }

                    //Application.DoEvents();
                    Thread.Sleep(20);

                    // read the response
                    byte[] readBuffer = new byte[512];
                    uint bytesRead;
                    success = newDevice.ReadData(ref readBuffer, out bytesRead);

                    // disconnect, this device might not be used and if it is left open
                    // it won't be disconnected until gc runs the finalizer (destructor) and
                    // if a list of devices is retrieved the device can't be opened again
                    newDevice.Disconnect();

                    if (!(success))
                    {
                        return null;
                    }

                    // what command did we get?
                    FW6PacketParser.HandleStates state;
                    FW6PacketParser packetParser = new FW6PacketParser();
                    FW6Packet packetResponse = packetParser.HandleBytes(out state, readBuffer, (int)bytesRead);

                    if (state == FW6PacketParser.HandleStates.CommandComplete)
                    {
                        // did we get a status response back?
                        if (packetResponse is StatusResponse)
                        {
                            StatusResponse statusPacket = packetResponse as StatusResponse;
                            string serialNumber = String.Format("{0:D6}", statusPacket.SerialNumber);

                            newDevice.DeviceType = statusPacket.DeviceType;
                            newDevice.SerialNumber = serialNumber;
                            newDevice.FirmwareVersion = new Version(statusPacket.FwVersionMajor, statusPacket.FwVersionMinor, statusPacket.FwBuildNumber);
                            newDevice.FpgaVersion = new Version(statusPacket.FpgaVersionMajor, statusPacket.FpgaVersionMinor);

                            devices.Add(newDevice);
                        }
                    }
                }
            }

            return devices;
        }

        protected override void Connect()
        {
            // if we have a valid device return, we are already connected
            if (theWinUsbDevice != null)
            {
                return;
            }

            theWinUsbDevice = new WinUsbDevice();

            bool success = theWinUsbDevice.GetDeviceHandle(DevicePath);
            if (!success)
            {
                theWinUsbDevice.CloseDeviceHandle();

                InvalidOperationException exception = new System.InvalidOperationException("cannot find device");
                throw exception;
            }

#if false
            if (deviceDetected)
            {
                // The device was detected.
                // Register to receive notifications if the device is removed or attached.
                success = myDeviceManagement.RegisterForDeviceNotifications
                                                    (DevicePath,
                                                    messageHandler.Handle,
                                                    winUsbGuid,
                                                    ref deviceNotificationHandle);

                if (success)
                {
                    theWinUsbDevice.InitializeDevice();
                }
                else
                {
                    Disconnect();
                    throw new System.InvalidOperationException("unable to register notifications");
                }
            }
#else
            if (!theWinUsbDevice.InitializeDevice())
            {
                InvalidOperationException exception = new System.InvalidOperationException("InitializeDevice() failed");
                throw exception;
            }

#endif
        }

        protected override void Disconnect()
        {
            if (theWinUsbDevice != null)
            {
                theWinUsbDevice.CloseDeviceHandle();
                theWinUsbDevice = null;
            }
            else
            {
                //
            }

#if false
            if(deviceNotificationHandle != IntPtr.Zero)
            {
                myDeviceManagement.StopReceivingDeviceNotifications(deviceNotificationHandle);
                deviceNotificationHandle = IntPtr.Zero;
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packetToSend"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        protected override FW6Packet SendAndGetResponse(FW6Packet packetToSend, int timeoutMS)
        {
            int delayBeforeReadMS = 20;
            return SendAndGetResponse(packetToSend, delayBeforeReadMS, timeoutMS);
        }

        /// <summary>
        /// Returns a packet if one was returned
        /// </summary>
        protected override FW6Packet SendAndGetResponse(FW6Packet packetToSend, int delayBeforeReadMS, int timeoutMS)
        {
            // send the command to the device
            byte[] writeData = new byte[packetToSend.EncodedPacket.Length];
            Array.Copy(packetToSend.EncodedPacket, writeData, writeData.Length);
            bool success = theWinUsbDevice.SendViaBulkTransfer(ref writeData,
                                                                          (uint)writeData.Length);
            if (!success)
            {
                return null;
            }
            
            // delay before reading the response
            //Application.DoEvents();
            System.Threading.Thread.Sleep(delayBeforeReadMS);

            // read the response
            byte[] readBuffer = new byte[1024 * 8];
            uint bytesRead = 0;
            theWinUsbDevice.ReadViaBulkTransfer(System.Convert.ToByte(theWinUsbDevice.myDevInfo.bulkInPipe),
                                                (uint)readBuffer.Length,
                                                ref readBuffer,
                                                ref bytesRead,
                                                ref success);

            if (!(success))
            {
                return null;
            }

            // what command did we get?
            FW6PacketParser.HandleStates state;
            FW6PacketParser packetParser = new FW6PacketParser();
            FW6Packet packet = packetParser.HandleBytes(out state, readBuffer, (int)bytesRead);

            if (state == FW6PacketParser.HandleStates.CommandComplete)
            {
                return packet;
            }
            return null;
        }
    }
}
