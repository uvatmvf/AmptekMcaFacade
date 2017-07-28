using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;

namespace csRepeat.FW6
{
    public class DPDeviceEthernet : FW6Device
    {
        public const int udpDiscoveryPort = 10001;

        public IPEndPoint deviceEndPoint;

        protected override int DataPacketAckTimeoutMS
        {
            get
            {
                // Ethernet data packets are a special case. Something on the DP5 side is causing
                // ack packets to be delayed, if the pc resends the packet and the previous ack is
                // finally delivered then the pc can interpret the delayed ack as an ack for the next
                // data packet causing a gap in programming. So we wait a long time for the packets
                //
                // Dave@Amptek asked for a delay of 6 seconds here
                return 6000;
            }
        }

        public DPDeviceEthernet(DeviceTypes deviceType,
                                Version firmwareVersion,
                                Version fpgaVersion,
                                string serialNumber,
                                IPEndPoint deviceEndPoint)
            : base(InterfaceTypes.ethernet, deviceType,
                   firmwareVersion,
                   fpgaVersion,
                   serialNumber)
        {
            this.deviceEndPoint = deviceEndPoint;

            // update fpga state, have to call this here because performing it in
            // FW6Device() constructor meas it would happen before
            // the code here so internal state storage wouldn't be set up
            UpdateFpgaState();
        }

        public static List<DPDevice> DetectDevices()
        {
            List<DPDevice> devices = new List<DPDevice>();

            foreach (System.Net.NetworkInformation.NetworkInterface i in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (System.Net.NetworkInformation.UnicastIPAddressInformation ua in i.GetIPProperties().UnicastAddresses)
                {
                    Console.WriteLine(ua.Address);
                    List<DPDeviceEthernet> newDevices = DetectFromAddress(ua.Address);
                    if (newDevices != null)
                    {
                        foreach (DPDeviceEthernet d in newDevices)
                        {
                            devices.Add(d);
                        }
                    }
                }
            }

            return devices;
        }

        private static List<DPDeviceEthernet> DetectFromAddress(IPAddress localIP)
        {
            List<DPDeviceEthernet> devices = new List<DPDeviceEthernet>();

            // skip detection on ipv6 networks for now
            if (localIP.AddressFamily == AddressFamily.InterNetworkV6)
                return null;

            StatusRequest statusRequest = new FW6.StatusRequest();

            // create and bind to the local ip address
            // NOTE: We use a custom socket so we can control its settings and bind it
            int port = 0; // picks any appropriate port
            Socket socket = new Socket(localIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(localIP, port);
                socket.Bind(localEndPoint);
            }
            catch
            {
                return null;
            }

            UdpClient udpClient = new UdpClient();
            udpClient.Client = socket;
            udpClient.Client.ReceiveTimeout = 500; // timeout in milliseconds

            // Creates an IPEndPoint to record the IP Address and port number of the sender.
            IPEndPoint remoteEndPoint;
            remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, udpDiscoveryPort);

            // Creates an IPEndPoint to record the IP Address and port number of the sender.
            // The IPEndPoint will allow you to read datagrams sent from any source.
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receivedBytes = null;

            udpClient.Send(statusRequest.EncodedPacket, statusRequest.EncodedPacket.Length, remoteEndPoint);

            // receive data
            receivedBytes = null;
            try
            {
                // receive all responses and attempt to handle them as new devices
                while (true)
                {
                    receivedBytes = udpClient.Receive(ref remoteIpEndPoint);
                    DPDeviceEthernet newDevice = HandleReceivedBytes(receivedBytes, remoteIpEndPoint);
                    if (newDevice != null)
                    {
                        int iMatchingDevices = 0;
                        int iDev = 0;
                        DPDeviceEthernet d;
                        d = newDevice;
                        for (iDev = 0; iDev < devices.Count; iDev++)
                        {
                            d = devices[iDev];
                            if ((d.SerialNumber == newDevice.SerialNumber) &&
                                              d.udpClient.Client.RemoteEndPoint.Equals(newDevice.udpClient.Client.RemoteEndPoint))
                            {
                                iMatchingDevices++;
                            }
                        }
                        if (iMatchingDevices == 0)
                        {
                            devices.Add(newDevice);
                        }
                    }
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    // Delay for 5 seconds - this is a workaround for the following:
                    //   Once a packet exchange has taken place on this port, the socket is bound to the 
                    //   IP address and source port of the host it exchanged packets with. 
                    //   Once the socket is bound, packets from other IP addresses will be ignored (including the IP address it is bound to).
                    //   After approx. 4-5 seconds of inactivity on the socket, 
                    //   the socket is reset so that it can once again connect to any IP.
                    // This allows repeated discovery of ethernet devices
                    int sleepMS = 5000;
                    System.Threading.Thread.Sleep(sleepMS);
                }
                else
                {
                    throw; // rethrow the exception
                }
            }
            catch (System.Exception)
            {
                // ignore exceptions
            }

            if (receivedBytes == null)
            {
                return null;
            }

            // Return list of discovered devices
            return devices;
        }

        private static DPDeviceEthernet HandleReceivedBytes(byte[] receivedBytes, IPEndPoint remoteIpEndPoint)
        {
            // what command did we get?
            FW6PacketParser.HandleStates state;
            FW6PacketParser packetParser = new FW6PacketParser();
            FW6Packet packetResponse = packetParser.HandleBytes(out state, receivedBytes, receivedBytes.Length);

            if (state == FW6PacketParser.HandleStates.CommandComplete)
            {
                // did we get a status response back?
                if (packetResponse is StatusResponse)
                {
                    StatusResponse statusPacket = packetResponse as StatusResponse;
                    string serialNumber = String.Format("{0:D6}", statusPacket.SerialNumber);
                    DPDeviceEthernet newDevice = new DPDeviceEthernet(statusPacket.DeviceType,
                                                         new Version(statusPacket.FwVersionMajor, statusPacket.FwVersionMinor, statusPacket.FwBuildNumber),
                                                         new Version(statusPacket.FpgaVersionMajor, statusPacket.FpgaVersionMinor),
                                                         serialNumber,
                                                         remoteIpEndPoint);
                    return newDevice;
                }
            }

            return null;
        }

        protected UdpClient udpClient;

        protected override void Connect()
        {
            // open a socket to the device
            udpClient = new UdpClient();
            udpClient.Connect(deviceEndPoint);
            int receiveTimeoutMilliseconds = 100;
            udpClient.Client.ReceiveTimeout = receiveTimeoutMilliseconds;
        }

        protected override void Disconnect()
        {
            udpClient.Close();
            udpClient = null;
        }

        /// <summary>
        /// Send and get a response
        /// </summary>
        /// <param name="packetToSend"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        protected override FW6Packet SendAndGetResponse(FW6Packet packetToSend, int timeoutMS)
        {
            int delayBeforeReadMS = 0;
            return SendAndGetResponse(packetToSend, delayBeforeReadMS, timeoutMS);
        }

        /// <summary>
        /// Returns true if an ack was found, false if timeout with some retries
        /// </summary>
        /// <param name="port"></param>
        /// <param name="packetToSend"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        protected override FW6Packet SendAndGetResponse(FW6Packet packetToSend, int delayBeforeReadMS, int timeoutMS)
        {
            int retries = 5;

            do
            {
                FW6Packet response = null;
                try
                {
                    response = _SendAndGetResponse(udpClient, packetToSend, delayBeforeReadMS, timeoutMS);
                } catch
                {
                }

                if (response != null)
                {
                    return response;
                }

                retries--;
            } while (retries > 0);

            return null;
        }

        /// <summary>
        /// Returns true if an ack was found, false if timeout
        /// </summary>
        /// <param name="port"></param>
        /// <param name="packetToSend"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        private FW6Packet _SendAndGetResponse(UdpClient udpClient, FW6Packet packetToSend, int delayBeforeReadMS, int timeoutMS)
        {
            udpClient.Send(packetToSend.EncodedPacket, packetToSend.EncodedPacket.Length);

            // delay before reading the response
            System.Threading.Thread.Sleep(delayBeforeReadMS);

            // read the response
            FW6PacketParser fw6PacketParser = new FW6.FW6PacketParser();

            DateTime start = DateTime.Now;
            int timeoutMilliseconds = timeoutMS;
            TimeSpan timeoutSpan = new TimeSpan(0, 0, 0, 0, timeoutMilliseconds);
            DateTime timeoutTime = start + timeoutSpan;
            byte[] bytes = null;
            FW6.FW6PacketParser.HandleStates state = FW6PacketParser.HandleStates.NeedMoreData;

            FW6Packet packet = null;
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            do
            {
                try
                {
                    bytes = udpClient.Receive(ref remoteEndPoint);
                }
                catch (SocketException)
                {
                }

                // pass the bytes to the packet parser
                if (bytes != null)
                {
                    packet = fw6PacketParser.HandleBytes(out state, bytes, bytes.Length);
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
            } while (DateTime.Now < timeoutTime);

            return null;
        }

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

        public override bool GetRawStatus(ref byte[] readBuffer, out uint bytesRead)
        {
            FW6Packet respPacket;
            bool success = false;
            int delayBeforeReadMS = 20;
            int timeoutMS = 30;
            int idx = 0;

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

        ////private const int mcaChannels = 1024;
        ////public string mcaChannels { get; set; }
        private int _mcaChannels;
        public override int mcaChannels
        {
            get { return _mcaChannels; }
            set { _mcaChannels = value; }
        }


    }
}
