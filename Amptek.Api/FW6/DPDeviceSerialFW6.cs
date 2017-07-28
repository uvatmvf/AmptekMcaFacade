using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace csRepeat.FW6
{
    public class DPDeviceSerialFW6 : FW6Device
    {

        //public string PortName { get; set; }
        private string _PortName;
        public string PortName
        {
            get { return _PortName; }
            set { _PortName= value; }
        }

        ////private const int mcaChannels = 1024;
        ////public string mcaChannels { get; set; }
        private int _mcaChannels;
        public override int mcaChannels
        {
            get { return _mcaChannels; }
            set { _mcaChannels = value; }
        }

        protected override int DataPacketAckTimeoutMS
        {
            get { return 750; }
        }

        public DPDeviceSerialFW6(DeviceTypes deviceType,
                                 Version firmwareVersion,
                                 Version fpgaVersion,
                                 string serialNumber,
                                 string portName) : base(InterfaceTypes.rs232,
                                                         deviceType,
                                                         firmwareVersion,
                                                         fpgaVersion,
                                                         serialNumber)
        {
            this.PortName = portName;

            // update fpga state, have to call this here because performing it in
            // FW6Device() constructor meas it would happen before
            // the code here so internal state storage wouldn't be set up
            UpdateFpgaState();
        }

        private static object detectLock = new Object();

        public override bool ConnectForTesting()
        {
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;
            Connect();
            StatusRequest statusRequest = new FW6.StatusRequest();
            respPacket = SendAndGetResponse(statusRequest, delayBeforeReadMS, timeoutMS);
            success = FW6.StatusResponse.IsMatch(respPacket.PID1, respPacket.PID2);
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
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;
            int idx=0;

            readBuffer = new byte[512];
            bytesRead = 0;
            //Connect();
            StatusRequest statusRequest = new FW6.StatusRequest();
            respPacket = SendAndGetResponse(statusRequest, delayBeforeReadMS, timeoutMS);
            success = FW6.StatusResponse.IsMatch(respPacket.PID1, respPacket.PID2);
            if (!success)
            {
                bytesRead = 0;
                Disconnect();
            }
            else
            {
                // read the response
                bytesRead = 64;
                for (idx = 0; idx < 64; idx++)
                {
                    readBuffer[idx] = respPacket.Data[idx];
                }
            }
            return success;
        }

        //10 MSec per 100 bytes (no less than 10) each way + 10 MSec for Request/Response
        private int CalcMsgDelay(int ibytesTotal)
        {
            int iMsgDelay = 0;
            double dblBytes = 0;
            double dblMSecPerByte = 0.1;
            dblBytes = (double)ibytesTotal;
            iMsgDelay = (int)(dblBytes * dblMSecPerByte) + 10;
            return iMsgDelay;
        }

        public override string GetFullConfiguration(byte DppType, bool PC5_PRESENT)
        {
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;
            string strCommands = GetFullReadBackCmdString(DppType, PC5_PRESENT);
            byte[] readBuffer = new byte[520];
            byte[] cfgCommands = Encoding.ASCII.GetBytes(strCommands);
            string strCmdReadback = "";
            //Connect();
            ConfigurationReadback cfgReadback = new FW6.ConfigurationReadback(cfgCommands);

            int ibytesTotal = 1024;
            delayBeforeReadMS = CalcMsgDelay(ibytesTotal);
            timeoutMS = delayBeforeReadMS + 20;
            respPacket = SendAndGetResponse(cfgReadback, delayBeforeReadMS, timeoutMS);
            success = FW6.ConfigurationResponse.IsMatch(respPacket.PID1, respPacket.PID2);
            if (!success)
            {
                Disconnect();
            }
            else           // read the response
            {
                int lMsgLen = respPacket.Data.GetLength(0);
                if (success)
                {
                    strCmdReadback = ASCIIEncoding.ASCII.GetString(respPacket.Data);
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
            FW6Packet respPacket; 
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;

            readBuffer = new byte[520];
            bytesRead = 0;
            if (strCommands.Length == 0)
            {
                return success;
            }
            byte[] cfgCommands = Encoding.ASCII.GetBytes(strCommands);
            //Connect();
            ConfigurationReadback cfgReadback = new FW6.ConfigurationReadback(cfgCommands);
            int ibytesTotal = 520;
            delayBeforeReadMS = CalcMsgDelay(ibytesTotal);
            timeoutMS = delayBeforeReadMS + 20;
            respPacket = SendAndGetResponse(cfgReadback, delayBeforeReadMS, timeoutMS);
            success = FW6.ConfigurationResponse.IsMatch(respPacket.PID1, respPacket.PID2);
            if (!success)
            {
                Disconnect();
            }
            else
            {
                int lMsgLen = respPacket.Data.GetLength(0);
                bytesRead = (uint)lMsgLen;
                for (int i = 0; i < lMsgLen; i++)
                {
                    readBuffer[i] = respPacket.Data[i];
                }
            }
            return success;
        }

        public override bool SendConfiguration(string strCommands)
        {
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;
            if (strCommands.Length == 0)
            {
                return success;
            }
            byte[] cfgCommands = Encoding.ASCII.GetBytes(strCommands);
            //Connect();
            Configuration cfgSend = new FW6.Configuration(cfgCommands);
            int ibytesTotal = 512;
            delayBeforeReadMS = CalcMsgDelay(ibytesTotal);
            timeoutMS = delayBeforeReadMS + 20;
            respPacket = SendAndGetResponse(cfgSend, delayBeforeReadMS, timeoutMS);
            success = FW6.AckResponse.IsMatch(respPacket.PID1, respPacket.PID2);
            success = (success && (respPacket.PID2 == 0x00));
            return success;
        }

        public override bool ClearDppData()
        {
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;
            //Connect();
            ClearSpectrum dppRequest = new FW6.ClearSpectrum();
            respPacket = SendAndGetResponse(dppRequest, delayBeforeReadMS, timeoutMS);
            success = FW6.AckResponse.IsMatch(respPacket.PID1, respPacket.PID2);
            success = (success && (respPacket.PID2 == 0x00));
            return success;
        }

        public override bool StartDppData()
        {
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20; 
            int timeoutMS = 30; 
            //Connect();
            EnableMCA dppRequest = new FW6.EnableMCA();
            respPacket = SendAndGetResponse(dppRequest, delayBeforeReadMS, timeoutMS);
            success = FW6.AckResponse.IsMatch(respPacket.PID1, respPacket.PID2);
            success = (success && (respPacket.PID2 == 0x00));
            return success;
        }

        public override bool PauseDppData()
        {
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;
            //Connect();
            DisableMCA dppRequest = new FW6.DisableMCA();
            respPacket = SendAndGetResponse(dppRequest, delayBeforeReadMS, timeoutMS);
            success = FW6.AckResponse.IsMatch(respPacket.PID1, respPacket.PID2);
            success = (success && (respPacket.PID2 == 0x00));
            return success;
        }

        public override bool GetSpectrumData(ref byte[] readBuffer, out uint bytesRead)
        {
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;
            readBuffer = new byte[24648];
            bytesRead = 0;
            //Connect();
            SpectrumRequest spectrumRequest = new FW6.SpectrumRequest();
            int ibytesTotal = mcaChannels * 3;
            delayBeforeReadMS = CalcMsgDelay(ibytesTotal);
            timeoutMS = delayBeforeReadMS + 20;
            respPacket = SendAndGetResponse(spectrumRequest, delayBeforeReadMS, timeoutMS);
            success = FW6.SpectrumResponse.IsMatch(respPacket.PID1, respPacket.PID2);
            if (!success)
            {
                Disconnect();
            }
            else
            {
                int lMsgLen = respPacket.Data.GetLength(0);
                bytesRead = (uint)lMsgLen;
                for (int i = 0; i < lMsgLen; i++)
                {
                    readBuffer[i] = respPacket.Data[i];
                }
            }
            return success;
        }

        public static List<DPDevice> DetectDevices()
        {
            string[] ports = SerialPort.GetPortNames();
            List<DPDevice> detectedDevices = new List<DPDevice>();

            // prevent multiple calls to DetectDevices at the same time
            lock (detectLock)
            {
                foreach (string portName in ports)
                {
                    FW6Packet packet = TryToDetectDevice(portName);
                    if ((packet != null) && (packet is StatusResponse))
                    {
                        StatusResponse statusPacket = packet as StatusResponse;
                        string firmwareVersion = String.Format("{0}.{1}",
                                                                            statusPacket.FwVersionMajor, statusPacket.FwVersionMinor);
                        string fpgaVersion = String.Format("{0}.{1}",
                                                                                statusPacket.FpgaVersionMajor, statusPacket.FpgaVersionMinor);
                        string serialNumber = String.Format("{0:D6}", statusPacket.SerialNumber);
                        DPDeviceSerialFW6 newDevice = new DPDeviceSerialFW6(statusPacket.DeviceType,
                                                                                      new Version(statusPacket.FwVersionMajor, statusPacket.FwVersionMinor, statusPacket.FwBuildNumber),
                                                                                      new Version(statusPacket.FpgaVersionMajor, statusPacket.FpgaVersionMinor),
                                                                                      serialNumber,
                                                                                      portName);
                        detectedDevices.Add(newDevice);
                    }
                }
            }

            return detectedDevices;
        }

        private static SerialPort GetPort(string portName)
        {
            SerialPort port = new SerialPort();
            port.PortName = portName;
            port.BaudRate = 115200;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Parity = Parity.None;
            port.Handshake = Handshake.None;
            port.WriteTimeout = 10000;
            port.ReadTimeout = 10000;

            return port;
        }

        private static FW6Packet TryToDetectDevice(string portName)
        {
            SerialPort port = GetPort(portName);

            try
            {
                port.Open();

                // send the status request
                FW6.StatusRequest statusRequest = new FW6.StatusRequest();
                port.Write(statusRequest.EncodedPacket, 0, statusRequest.EncodedPacket.Length);

                // read the response
                FW6.FW6PacketParser fw6PacketParser = new FW6.FW6PacketParser();

                // read data until we get a timeout
                DateTime start = DateTime.Now;
                int timeoutMilliseconds = 300;
                TimeSpan timeoutSpan = new TimeSpan(0, 0, 0, 0, timeoutMilliseconds);
                DateTime timeoutTime = start + timeoutSpan;
                int bytesToRead = 128;
                byte[] bytes = new byte[bytesToRead];
                int bytesRead = 0;
                FW6.FW6PacketParser.HandleStates state;

                try
                {
                    do
                    {
                        bytesRead = port.Read(bytes, 0, bytesToRead);

                        // pass the bytes to the packet parser
                        FW6Packet packet;
                        try
                        {
                            packet = fw6PacketParser.HandleBytes(out state, bytes, bytesRead);
                        }
                        catch
                        {
                            // parsing error, may be a v5.x device
                            return null;
                        }

                        if (state == FW6.FW6PacketParser.HandleStates.CommandComplete)
                        {
                            return packet;
                        }
                        else if (state == FW6.FW6PacketParser.HandleStates.InvalidChecksum)
                        {
                            return null;
                        }
                    } while (DateTime.Now < timeoutTime);
                }
                catch (System.TimeoutException)
                {
                    if (bytesRead != 0)
                    {
                        // pass the bytes to the packet parser
                        try
                        {
                            FW6Packet packet = fw6PacketParser.HandleBytes(out state, bytes, bytesRead);
                            return packet;
                        }
                        catch
                        {
                            // parsing error, may be a v5.x device
                            return null;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                // close the port when we exit this routine
                port.Close();
            }

            return null;
        }

        private SerialPort port;

        protected override void Connect()
        {
            port = GetPort(PortName);
            port.Open();
        }

        protected override void Disconnect()
        {
            port.Close();
            port = null;
        }

        protected override FW6Packet SendAndGetResponse(FW6Packet packetToSend, int timeoutMS)
        {
            int delayBeforeReadMS = 0;
            return SendAndGetResponse(packetToSend, delayBeforeReadMS, timeoutMS);
        }

        protected override FW6Packet SendAndGetResponse(FW6Packet packetToSend, int delayBeforeReadMS, int timeoutMS)
        {
            port.ReadTimeout = timeoutMS;
            port.Write(packetToSend.EncodedPacket, 0, packetToSend.EncodedPacket.Length);

            // sleep before reading the response
            System.Threading.Thread.Sleep(delayBeforeReadMS);

            // read the response
            FW6.FW6PacketParser fw6PacketParser = new FW6.FW6PacketParser();

            DateTime start = DateTime.Now;
            int timeoutMilliseconds = timeoutMS;
            TimeSpan timeoutSpan = new TimeSpan(0, 0, 0, 0, timeoutMilliseconds);
            DateTime timeoutTime = start + timeoutSpan;
            int bytesToRead = 128;
            byte[] bytes = new byte[bytesToRead];
            int bytesRead = 0;
            FW6.FW6PacketParser.HandleStates state = FW6PacketParser.HandleStates.NeedMoreData;

            FW6Packet packet = null;

            do
            {
                try
                {
                    bytesRead = port.Read(bytes, 0, bytesToRead);
                }
                catch (System.TimeoutException ex)
                {
                    string strErr = ex.ToString();
                    return null;
                }

                try
                {
                    // pass the bytes to the packet parser
                    if (bytesRead != 0)
                    {
                        packet = fw6PacketParser.HandleBytes(out state, bytes, bytesRead);
                    }

                    if (state == FW6.FW6PacketParser.HandleStates.CommandComplete)
                    {
                        return packet;
                    }
                    else if (state == FW6.FW6PacketParser.HandleStates.InvalidChecksum)
                    {
                        return null;
                    }
                    else if (state == FW6.FW6PacketParser.HandleStates.NeedMoreData)
                    {
                    }
                }
                catch(System.Exception ex)
                {
                    string strErr = ex.ToString();
                    return null;
                }
            } while (DateTime.Now < timeoutTime);

            return null;
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(", PortName: {0}", PortName);
        }
    }
}
