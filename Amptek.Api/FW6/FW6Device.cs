using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace csRepeat.FW6
{
    public abstract class FW6Device : DPDevice
    {

        protected abstract void Connect();
        protected abstract void Disconnect();

        /// <summary>
        /// Send a packet and get the response
        /// </summary>
        /// <param name="packetToSend"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        protected AckResponse SendAndWaitForAck(FW6Packet packetToSend, int timeoutMS)
        {
            int delayBeforeReadMS = 0;
            return SendAndWaitForAck(packetToSend, delayBeforeReadMS, timeoutMS);
        }

        /// <summary>
        /// Send a packet and get the response
        /// </summary>
        /// <param name="packetToSend"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        protected AckResponse SendAndWaitForAck(FW6Packet packetToSend, int delayBeforeReadMS, int timeoutMS)
        {
            FW6Packet packet = SendAndGetResponse(packetToSend, delayBeforeReadMS, timeoutMS);

            if (packet is AckResponse)
            {
                return packet as AckResponse;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Send a command and read the response
        /// </summary>
        /// <param name="packetToSend"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        protected abstract FW6Packet SendAndGetResponse(FW6Packet packetToSend, int timeoutMS);

        /// <summary>
        /// Send a command, wait delayBeforeReadMS, and then attempt to read the response for timeoutMS
        /// </summary>
        /// <param name="packetToSend"></param>
        /// <param name="delayBeforeReadMS"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        protected abstract FW6Packet SendAndGetResponse(FW6Packet packetToSend, int delayBeforeReadMS, int timeoutMS);

        /// <summary>
        /// The maximum time to wait for an ack after sending a data packet
        /// </summary>
        protected abstract int DataPacketAckTimeoutMS
        {
            get;
        }

        public abstract int mcaChannels
        {
            get;
            set;
        }

        public FW6Device(InterfaceTypes interfaceType,
                         DeviceTypes deviceType,
                         Version firmwareVersion,
                         Version fpgaVersion,
                         string serialNumber)
            : base(interfaceType, deviceType,
                   firmwareVersion, fpgaVersion,
                   serialNumber)
        {
        }

        /// <summary>
        /// Call to update the FpgaState
        /// </summary>
        internal void UpdateFpgaState()
        {
            try
            {
                // connect
                Connect();

                // we can detect unprogrammed FPGAs for FW6 devices by attempting to retrieve the spectrum
                SpectrumRequest spectrumRequest = new SpectrumRequest();
                FW6Packet response = SendAndGetResponse(spectrumRequest, DataPacketAckTimeoutMS);
                if (!(response is SpectrumResponse))
                {
                    FpgaState = FpgaStates.Unprogrammed;
                }
            }
            finally
            {
                Disconnect();
            }
        }

        public DiagnosticResponse GetDiagnosticResponse
        {
            get
            {
                try
                {
                    Connect();

                    // retrieve the device calibration values via the DiagnosticRequest command
                    DiagnosticRequest diagnosticRequest = new DiagnosticRequest();

                    int responseTimeoutMS = 2500; // spec says maximum of 2.5 seconds before the response
                    FW6Packet response = SendAndGetResponse(diagnosticRequest, responseTimeoutMS);
                    if (response is DiagnosticResponse)
                    {
                        return response as DiagnosticResponse;
                    }
                    else
                    {
                        return null;
                    }
                }
                finally
                {
                    Disconnect();
                }
            }
        }

        public void SetAdcCalibration(byte adcGain, byte adcOffset)
        {
            try
            {
                Connect();

                SetADCCalibrationRequest setAdcCalibrationRequest = new SetADCCalibrationRequest(adcGain, adcOffset);

                AckResponse ackResponse = SendAndWaitForAck(setAdcCalibrationRequest, DataPacketAckTimeoutMS);

                if (ackResponse.AckType != AckResponse.AckTypes.Ok)
                {
                    InvalidOperationException exception = new System.InvalidOperationException("ack response of " + ackResponse.AckType);
                    throw exception;
                }
            }
            finally
            {
                Disconnect();
            }
        }

        public void SetuCTemperatureCalibration(byte uCTempCalibration)
        {
            try
            {
                Connect();

                SetuCTemperatureCalibrationRequest setuCTemperatureCalibrationRequest = new SetuCTemperatureCalibrationRequest(uCTempCalibration);

                AckResponse ackResponse = SendAndWaitForAck(setuCTemperatureCalibrationRequest, DataPacketAckTimeoutMS);

                if (ackResponse.AckType != AckResponse.AckTypes.Ok)
                {
                    InvalidOperationException exception = new System.InvalidOperationException("ack response of " + ackResponse.AckType);
                    throw exception;
                }
            }
            finally
            {
                Disconnect();
            }
        }

        public void SetPZCorrection(byte pzCorrection)
        {
            try
            {
                Connect();

                SetPZCorrectionRequest setPZCorrection = new SetPZCorrectionRequest(pzCorrection);

                AckResponse ackResponse = SendAndWaitForAck(setPZCorrection, DataPacketAckTimeoutMS);

                if (ackResponse.AckType != AckResponse.AckTypes.Ok)
                {
                    InvalidOperationException exception = new System.InvalidOperationException("ack response of " + ackResponse.AckType);
                    throw exception;
                }
            }
            finally
            {
                Disconnect();
            }
        }

        public void SetDCalCalibration(ushort dcalValue)
        {
            try
            {
                Connect();

                SetDCalRequest setDCalCalibration = new SetDCalRequest(dcalValue);

                AckResponse ackResponse = SendAndWaitForAck(setDCalCalibration, DataPacketAckTimeoutMS);

                if (ackResponse.AckType != AckResponse.AckTypes.Ok)
                {
                    InvalidOperationException exception = new System.InvalidOperationException("ack response of " + ackResponse.AckType);
                    throw exception;
                }
            }
            finally
            {
                Disconnect();
            }
        }

        public string GetFullReadBackCmdString(byte DppType, bool PC5_PRESENT)
        {
            string cstrCfg = "";
            bool isHVSE;
            bool isPAPS;
            bool isTECS;
            bool isVOLU;
            bool isCON1;
            bool isCON2;
            bool isINOF;
            bool isBOOT;
            bool isGATE;
            bool isPAPZ;

            //DEVICE_ID 0=DP5,1=PX5,2=DP5G,3=MCA8000D,4=TB5
            //public const byte dppDP5 = 0;
            //public const byte dppPX5 = 1;
            //public const byte dppDP5G = 2;
            //public const byte dppMCA8000D = 3;
            //public const byte dppTB5 = 4;

            isHVSE = (((DppType != FW6DppStatus.dppPX5) && PC5_PRESENT) || DppType == FW6DppStatus.dppPX5);
            isPAPS = (DppType != FW6DppStatus.dppDP5G);
            isTECS = (((DppType == FW6DppStatus.dppDP5) && PC5_PRESENT) || (DppType != FW6DppStatus.dppDP5G));
            isVOLU = (DppType == FW6DppStatus.dppPX5);
            isCON1 = (DppType != FW6DppStatus.dppDP5);
            isCON2 = (DppType != FW6DppStatus.dppDP5);
            isINOF = (DppType != FW6DppStatus.dppDP5G);
            isBOOT = (DppType == FW6DppStatus.dppDP5);
            isGATE = (DppType == FW6DppStatus.dppDP5);
            isPAPZ = (DppType == FW6DppStatus.dppPX5);

            cstrCfg = "";
            cstrCfg += "RESC=?;";
            cstrCfg += "CLCK=?;";
            cstrCfg += "TPEA=?;";
            cstrCfg += "GAIF=?;";
            cstrCfg += "GAIN=?;";
            cstrCfg += "RESL=?;";
            cstrCfg += "TFLA=?;";
            cstrCfg += "TPFA=?;";
            cstrCfg += "PURE=?;";
            cstrCfg += "RTDE=?;";
            cstrCfg += "MCAS=?;";
            cstrCfg += "MCAC=?;";
            cstrCfg += "SOFF=?;";
            cstrCfg += "AINP=?;";
            if (isINOF) { cstrCfg += "INOF=?;"; }
            cstrCfg += "GAIA=?;";
            cstrCfg += "CUSP=?;";
            cstrCfg += "PDMD=?;";
            cstrCfg += "THSL=?;";
            cstrCfg += "TLLD=?;";
            cstrCfg += "THFA=?;";
            cstrCfg += "DACO=?;";
            cstrCfg += "DACF=?;";
            cstrCfg += "RTDS=?;";
            cstrCfg += "RTDT=?;";
            cstrCfg += "BLRM=?;";
            cstrCfg += "BLRD=?;";
            cstrCfg += "BLRU=?;";
            if (isGATE) { cstrCfg += "GATE=?;"; }
            cstrCfg += "AUO1=?;";
            cstrCfg += "PRET=?;";
            cstrCfg += "PRER=?;";
            cstrCfg += "PREC=?;";
            cstrCfg += "PRCL=?;";
            cstrCfg += "PRCH=?;";
            if (isHVSE) { cstrCfg += "HVSE=?;"; }
            if (isTECS) { cstrCfg += "TECS=?;"; }
            if (isPAPZ) { cstrCfg += "PAPZ=?;"; }
            if (isPAPS) { cstrCfg += "PAPS=?;"; }
            cstrCfg += "SCOE=?;";
            cstrCfg += "SCOT=?;";
            cstrCfg += "SCOG=?;";
            cstrCfg += "MCSL=?;";
            cstrCfg += "MCSH=?;";
            cstrCfg += "MCST=?;";
            cstrCfg += "AUO2=?;";
            cstrCfg += "TPMO=?;";
            cstrCfg += "GPED=?;";
            cstrCfg += "GPIN=?;";
            cstrCfg += "GPME=?;";
            cstrCfg += "GPGA=?;";
            cstrCfg += "GPMC=?;";
            cstrCfg += "MCAE=?;";
            if (isVOLU) { cstrCfg += "VOLU=?;"; }
            if (isCON1) { cstrCfg += "CON1=?;"; }
            if (isCON2) { cstrCfg += "CON2=?;"; }
            if (isBOOT) { cstrCfg += "BOOT=?;"; }
            return cstrCfg;
        }

        public string GetCmdDesc(string cstrCmd)
        {
            string cstrCmdName = "";
            if (cstrCmd == "RESC")
            {
                cstrCmdName = "Reset Configuration";
            }
            else if (cstrCmd == "CLCK")
            {
                cstrCmdName = "20MHz/80MHz";
            }
            else if (cstrCmd == "TPEA")
            {
                cstrCmdName = "Peaking Time";
            }
            else if (cstrCmd == "GAIF")
            {
                cstrCmdName = "Fine Gain";
            }
            else if (cstrCmd == "GAIN")
            {
                cstrCmdName = "Total Gain (Analog * Fine)";
            }
            else if (cstrCmd == "RESL")
            {
                cstrCmdName = "Detector Reset Lockout";
            }
            else if (cstrCmd == "TFLA")
            {
                cstrCmdName = "Flat Top";
            }
            else if (cstrCmd == "TPFA")
            {
                cstrCmdName = "Fast Channel Peaking Time";
            }
            else if (cstrCmd == "PURE")
            {
                cstrCmdName = "PUR Interval On/Off";
            }
            else if (cstrCmd == "RTDE")
            {
                cstrCmdName = "RTD On/Off";
            }
            else if (cstrCmd == "MCAS")
            {
                cstrCmdName = "MCA Source";
            }
            else if (cstrCmd == "MCAC")
            {
                cstrCmdName = "MCA/MCS Channels";
            }
            else if (cstrCmd == "SOFF")
            {
                cstrCmdName = "Set Spectrum Offset";
            }
            else if (cstrCmd == "AINP")
            {
                cstrCmdName = "Analog Input Pos/Neg";
            }
            else if (cstrCmd == "INOF")
            {
                cstrCmdName = "Input Offset";
            }
            else if (cstrCmd == "GAIA")
            {
                cstrCmdName = "Analog Gain Index";
            }
            else if (cstrCmd == "CUSP")
            {
                cstrCmdName = "Non-Trapezoidal Shaping";
            }
            else if (cstrCmd == "PDMD")
            {
                cstrCmdName = "Peak Detect Mode (Min/Max)";
            }
            else if (cstrCmd == "THSL")
            {
                cstrCmdName = "Slow Threshold";
            }
            else if (cstrCmd == "TLLD")
            {
                cstrCmdName = "LLD Threshold";
            }
            else if (cstrCmd == "THFA")
            {
                cstrCmdName = "Fast Threshold";
            }
            else if (cstrCmd == "DACO")
            {
                cstrCmdName = "DAC Output";
            }
            else if (cstrCmd == "DACF")
            {
                cstrCmdName = "DAC Offset";
            }
            else if (cstrCmd == "RTDS")
            {
                cstrCmdName = "RTD Sensitivity";
            }
            else if (cstrCmd == "RTDT")
            {
                cstrCmdName = "RTD Threshold";
            }
            else if (cstrCmd == "RTDD")
            {
                cstrCmdName = "Custom RTD Oneshot Delay";
            }
            else if (cstrCmd == "RTDW")
            {
                cstrCmdName = "Custom RTD Oneshot Width";
            }
            else if (cstrCmd == "BLRM")
            {
                cstrCmdName = "BLR Mode";
            }
            else if (cstrCmd == "BLRD")
            {
                cstrCmdName = "BLR Down Correction";
            }
            else if (cstrCmd == "BLRU")
            {
                cstrCmdName = "BLR Up Correction";
            }
            else if (cstrCmd == "GATE")
            {
                cstrCmdName = "Gate Control";
            }
            else if (cstrCmd == "AUO1")
            {
                cstrCmdName = "AUX_OUT Selection";
            }
            else if (cstrCmd == "PRET")
            {
                cstrCmdName = "Preset Time";
            }
            else if (cstrCmd == "PRER")
            {
                cstrCmdName = "Preset Real Time";
            }
            else if (cstrCmd == "PREL")
            {
                cstrCmdName = "Preset Live Time";
            }
            else if (cstrCmd == "PREC")
            {
                cstrCmdName = "Preset Counts";
            }
            else if (cstrCmd == "PRCL")
            {
                cstrCmdName = "Preset Counts Low Threshold";
            }
            else if (cstrCmd == "PRCH")
            {
                cstrCmdName = "Preset Counts High Threshold";
            }
            else if (cstrCmd == "HVSE")
            {
                cstrCmdName = "HV Set";
            }
            else if (cstrCmd == "TECS")
            {
                cstrCmdName = "TEC Set";
            }
            else if (cstrCmd == "PAPZ")
            {
                cstrCmdName = "Pole-Zero";
            }
            else if (cstrCmd == "PAPS")
            {
                cstrCmdName = "Preamp 8.5/5 (N/A)";
            }
            else if (cstrCmd == "SCOE")
            {
                cstrCmdName = "Scope Trigger Edge";
            }
            else if (cstrCmd == "SCOT")
            {
                cstrCmdName = "Scope Trigger Position";
            }
            else if (cstrCmd == "SCOG")
            {
                cstrCmdName = "Digital Scope Gain";
            }
            else if (cstrCmd == "MCSL")
            {
                cstrCmdName = "MCS Low Threshold";
            }
            else if (cstrCmd == "MCSH")
            {
                cstrCmdName = "MCS High Threshold";
            }
            else if (cstrCmd == "MCST")
            {
                cstrCmdName = "MCS Timebase";
            }
            else if (cstrCmd == "AUO2")
            {
                cstrCmdName = "AUX_OUT2 Selection";
            }
            else if (cstrCmd == "TPMO")
            {
                cstrCmdName = "Test Pulser On/Off";
            }
            else if (cstrCmd == "GPED")
            {
                cstrCmdName = "G.P. Counter Edge";
            }
            else if (cstrCmd == "GPIN")
            {
                cstrCmdName = "G.P. Counter Input";
            }
            else if (cstrCmd == "GPME")
            {
                cstrCmdName = "G.P. Counter Uses MCA_EN?";
            }
            else if (cstrCmd == "GPGA")
            {
                cstrCmdName = "G.P. Counter Uses GATE?";
            }
            else if (cstrCmd == "GPMC")
            {
                cstrCmdName = "G.P. Counter Cleared With MCA Counters?";
            }
            else if (cstrCmd == "MCAE")
            {
                cstrCmdName = "MCA/MCS Enable";
            }
            else if (cstrCmd == "VOLU")
            {
                cstrCmdName = "Speaker On/Off";
            }
            else if (cstrCmd == "CON1")
            {
                cstrCmdName = "Connector 1";
            }
            else if (cstrCmd == "CON2")
            {
                cstrCmdName = "Connector 2";
            }
            else if (cstrCmd == "SCAH")
            {
                cstrCmdName = "SCAx High Threshold";
            }
            else if (cstrCmd == "SCAI")
            {
                cstrCmdName = "SCA Index";
            }
            else if (cstrCmd == "SCAL")
            {
                cstrCmdName = "SCAx Low Theshold";
            }
            else if (cstrCmd == "SCAO")
            {
                cstrCmdName = "SCAx Output (SCA1-8 Only)";
            }
            else if (cstrCmd == "SCAW")
            {
                cstrCmdName = "SCA Pulse Width (Not Indexed - SCA1-8)";
            }
            else if (cstrCmd == "BOOT")
            {
                cstrCmdName = "Turn Supplies On/Off At Power Up";
            }
            return cstrCmdName;
        }

        public abstract bool ConnectForTesting();
        public abstract void DisconnectForTesting();

        public abstract bool StartDppData();
        public abstract bool PauseDppData();
        public abstract bool ClearDppData();

        public abstract bool GetConfiguration(string strCommands, ref byte[] readBuffer, out uint bytesRead);
        public abstract string GetFullConfiguration(byte DppType, bool PC5_PRESENT);
        public abstract string DisplayConfiguration(string strCommands);
        public abstract bool SendConfiguration(string strCommands);

        public abstract bool GetRawStatus(ref byte[] readBuffer, out uint bytesRead);
        public abstract bool GetSpectrumData(ref byte[] readBuffer, out uint bytesRead);

    }
}
