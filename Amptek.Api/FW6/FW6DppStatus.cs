using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat
{
    /// <summary>
    /// Parse FW6 Dpp Status data packets
    /// </summary>
    public class FW6DppStatus
    {

        bool bHaveStatus = false;
        byte[] RAW = new byte[64];
        ulong SerialNumber;
        ulong FastCount;
        ulong SlowCount;
        byte FPGA;
        byte Firmware;
        byte Build;
        public double AccumulationTime;
        double RealTime;
        double LiveTime;
        double HV;
        double DET_TEMP;
        double DP5_TEMP;
        //bool PX4;
        bool AFAST_LOCKED;
        public bool MCA_EN;
        public bool PRECNT_REACHED;
        public bool PresetRtDone;
        public bool PresetLtDone;
        //bool SUPPLIES_ON;
        bool SCOPE_DR;
        bool DP5_CONFIGURED;
        ulong GP_COUNTER;
        bool AOFFSET_LOCKED;
        bool MCS_DONE;
        //bool RAM_TEST_RUN;
        //bool RAM_TEST_ERROR;
        //double DCAL;
        //byte PZCORR;				// or single?
        //byte UC_TEMP_OFFSET;
        //double AN_IN;
        //double VREF_IN;
        //ulong PC5_SN;
        public bool PC5_PRESENT;
        bool PC5_HV_POL;
        bool PC5_8_5V;
        //double ADC_GAIN_CAL;
        //byte ADC_OFFSET_CAL;
        //long SPECTRUM_OFFSET;     // or single?
        bool b80MHzMode;
        bool bFPGAAutoClock;
        public byte DEVICE_ID;
        bool ReBootFlag;
        byte DPP_options;
        bool HPGe_HV_INH;
        bool HPGe_HV_INH_POL;

        double TEC_Voltage;
        byte DPP_ECO;
	    //bool AU34_2;                  // uncomment for automatic source changer (ASC) operation
        //bool isAscInstalled;          // uncomment for automatic source changer (ASC) operation
        //bool isAscEnabled;            // uncomment for automatic source changer (ASC) operation
        //bool bScintHas80MHzOption;    // uncomment for DP5G/TB5 80MHz FPGA 


        //DEVICE_ID 0=DP5,1=PX5,2=DP5G,3=MCA8000D,4=TB5
        public const byte dppDP5 = 0;
        public const byte dppPX5 = 1;
        public const byte dppDP5G = 2;
        public const byte dppMCA8000D = 3;
        public const byte dppTB5 = 4;

        //typedef enum _PX5_OPTIONS
        //{
	    public const byte PX5_OPTION_NONE=0;
	    public const byte PX5_OPTION_HPGe_HVPS=1;
	    public const byte PX5_OPTION_TEST_TEK=4;
	    public const byte PX5_OPTION_TEST_MOX=5;
	    public const byte PX5_OPTION_TEST_AMP=6;
	    public const byte PX5_OPTION_TEST_MODE_1=8;
        public const byte PX5_OPTION_TEST_MODE_2 = 9;
        //} PX5_OPTIONS;

        public ulong GetULong(int ulStart, byte[] buffer)
        {
            ulong ulValue = 0;
            for(int index=0;index<4;index++) {		// build 4 bytes (lwStart-lwStart+3) into double
                ulValue += buffer[(ulStart + index)] * (ulong)Math.Pow(2.0, 8.0 * (double)index);
            }
            return ulValue;
        }

        public void Process_Status(byte[] buffer)
        {
            for (int iRaw = 0; iRaw < 64; iRaw++) {
                RAW[iRaw] = buffer[iRaw];
            }
            bool bDMCA_LiveTime = false;
            bool bRebootFlagNagFix = false;

            DEVICE_ID = RAW[39];
            FastCount = GetULong(0, RAW);
            SlowCount = GetULong(4, RAW);
            GP_COUNTER = GetULong(8, RAW);
            AccumulationTime = (float)RAW[12] * 0.001 + (float)(RAW[13] + (float)RAW[14] * 256.0 + (float)RAW[15] * 65536.0) * 0.1;
            RealTime = ((double)RAW[20] + ((double)RAW[21] * 256.0) + ((double)RAW[22] * 65536.0) + ((double)RAW[23] * 16777216.0)) * 0.001;

            Firmware = RAW[24];
            FPGA = RAW[25];

            if (Firmware > 0x65) {
                Build = (byte)(RAW[37] & 0x0F);		//Build # added in FW6.06
            } else {
                Build = 0;
            }

            //Firmware Version:  6.07  Build:  0 has LiveTime and PREL
            //DEVICE_ID 0=DP5,1=PX5,2=DP5G,3=MCA8000D
            if (DEVICE_ID == dppMCA8000D) {
                if (Firmware >= 0x67) {
                    bDMCA_LiveTime = true;
                }
            }

            if (bDMCA_LiveTime) {
                LiveTime = ((double)RAW[16] + ((double)RAW[17] * 256.0) + ((double)RAW[18] * 65536.0) + ((double)RAW[19] * 16777216.0)) * 0.001;
            } else {
                LiveTime = 0;
            }

            if (RAW[29] < 128) {
                SerialNumber = GetULong(26, RAW);
            } else {
                SerialNumber = 0;
            }

            // HV = (double)(RAW[31] + (RAW[30] & 15) * 256) * 0.5;					// 0.5V/count

            if (RAW[30] < 128) {        // not negative
                HV = ((double)RAW[31] + ((double)RAW[30] * 256.0)) * 0.5;  // 0.5V/count
            } else {
                HV = (((double)RAW[31] + ((double)RAW[30] * 256)) - 65536.0) * 0.5; // 0.5V/count
            }

            DET_TEMP = (double)((RAW[33]) + (RAW[32] & 15) * 256) * 0.1; // - 273.16;		// 0.1K/count
            DP5_TEMP = RAW[34] - ((RAW[34] & 128) * 2);

            PresetRtDone = ((RAW[35] & 128) == 128);
            //byte:35 BIT:D6 
            // == Preset LiveTime Done for MCA8000D
            // == FAST Thresh locked for other dpp devices
            PresetLtDone = false;
            AFAST_LOCKED = false;
            if (bDMCA_LiveTime)
            {		// test for MCA8000D
                PresetLtDone = ((RAW[35] & 64) == 64);
            } else {
                AFAST_LOCKED = ((RAW[35] & 64) == 64);
            }
            MCA_EN = ((RAW[35] & 32) == 32);
            PRECNT_REACHED = ((RAW[35] & 16) == 16);
            SCOPE_DR = ((RAW[35] & 4) == 4);
            DP5_CONFIGURED = ((RAW[35] & 2) == 2);

            AOFFSET_LOCKED = ((RAW[36] & 128) == 0);  // 0=locked, 1=searching
            MCS_DONE = ((RAW[36] & 64) == 64);

            b80MHzMode = ((RAW[36] & 2) == 2);
            bFPGAAutoClock = ((RAW[36] & 1) == 1);

            PC5_PRESENT = ((RAW[38] & 128) == 128);
            if (PC5_PRESENT) {
                PC5_HV_POL = ((RAW[43] & 64) == 64);
                PC5_8_5V = ((RAW[43] & 32) == 32);
            } else {
                PC5_HV_POL = false;
                PC5_8_5V = false;
            }

            DPP_options = 0;
            if (DEVICE_ID == 1) {		// test for px5 options
                if ((RAW[42] & 1) == 1) {   // HPGe HVPS installed
                    DPP_options = 1;
                }
            }

            if (Firmware >= 0x65) {		// reboot flag added FW6.05
                if ((RAW[36] & 32) == 32) {
                    ReBootFlag = true;
                } else {
                    ReBootFlag = false;
                }
            } else {
                ReBootFlag = false;
            }
            bRebootFlagNagFix = ReBootFlag;

            //DPP_options = (byte)(RAW[42] & 15);
            //if (DPP_options > 0)
            //{
            //    HPGe_HV_INH = ((RAW[42] & 32) == 32);
            //    HPGe_HV_INH_POL = ((RAW[42] & 16) == 16);
            //}

	        TEC_Voltage = (((double)(RAW[40] & 15) * 256.0) + (double)(RAW[41])) / 758.5;
	        DPP_ECO = RAW[49];
            //bScintHas80MHzOption = false;         // uncomment for DP5G/TB5 80MHz FPGA 
	        DPP_options = (byte)(RAW[42] & 15);
	        HPGe_HV_INH = false;
	        HPGe_HV_INH_POL = false;
            //AU34_2 = false;                       // uncomment for automatic source changer (ASC) operation
            //isAscInstalled = false;               // uncomment for automatic source changer (ASC) operation
	        if (DEVICE_ID == dppPX5) {
		        if (DPP_options == PX5_OPTION_HPGe_HVPS) {
			        HPGe_HV_INH = ((RAW[42] & 32) == 32);
			        HPGe_HV_INH_POL = ((RAW[42] & 16) == 16);
			        if (DPP_ECO == 1) {     // 
                        //isAscInstalled = true;            // uncomment for automatic source changer (ASC) operation
                        //AU34_2 = ((RAW[42] & 64) == 64);  // uncomment for automatic source changer (ASC) operation
			        }
		        }
	        } else if ((DEVICE_ID == dppDP5G) || (DEVICE_ID == dppTB5)) {
		        if (DPP_ECO == 1) {
                    //bScintHas80MHzOption = true;  // uncomment for DP5G/TB5 80MHz FPGA 
		        }
	        }

            bHaveStatus = true;
        }

        // ShowStatusValueStrings does not include board and detector monitor data
        public string ShowStatusValueStrings()
        {
            string strConfig = "";
            string strTemp;

            if (!bHaveStatus)
            {
                return strConfig;
            }

            strTemp = GetDeviceNameFromVal(DEVICE_ID);
            strConfig = "Device Type: " + strTemp + "\r\n";
            strTemp = String.Format("Serial Number: {0:d}\r\n", SerialNumber);	//SerialNumber
            strConfig += strTemp;
            strTemp = "Firmware: " + VersionToStr(Firmware);
            strConfig += strTemp;
            if (Firmware > 0x65)
            {
                strTemp = String.Format("  Build:  {0:d}\r\n", Build);
                strConfig += strTemp;
            }
            else
            {
                strConfig += "\r\n";
            }
            strTemp = "FPGA: " + VersionToStr(FPGA) + "\r\n";
            strConfig += strTemp;
            if (DEVICE_ID != dppMCA8000D)
            {
                strTemp = String.Format("Fast Count: {0:f}\r\n", FastCount);   	//FastCount
                strConfig += strTemp;
            }
            strTemp = String.Format("Slow Count: {0:f}\r\n", SlowCount);   	//SlowCount
            strConfig += strTemp;

            if (DEVICE_ID != dppMCA8000D)
            {
                strTemp = String.Format("Accumulation Time: {0:f}\r\n", AccumulationTime);	    //AccumulationTime
                strConfig += strTemp;
            }

            strTemp = String.Format("Real Time: {0:f}\r\n", RealTime);	    //RealTime
            strConfig += strTemp;

            if (DEVICE_ID == dppMCA8000D)
            {
                strTemp = String.Format("Live Time: {0:f}\r\n", LiveTime);	    //LiveTime
                strConfig += strTemp;
            }

            return strConfig;
        }

        public string PX5_OptionsString()
        {
            string strOptions = "";
            if (DEVICE_ID == dppPX5) {
                //DPP_options = 1;
                //HPGe_HV_INH = true;
                //HPGe_HV_INH_POL = true;
                if (DPP_options > 0) {
                    //===============PX5 Options==================
                    strOptions += "PX5 Options: ";
                    if ((DPP_options & 1) == 1) {
                        strOptions += "HPGe HVPS\r\n";
                    } else {
                        strOptions += "Unknown\r\n";
                    }
                    //===============HPGe HVPS HV Status==================
                    strOptions += "HPGe HV: ";
                    if (HPGe_HV_INH) {
                        strOptions += "not inhibited\r\n";
                    } else {
                        strOptions += "inhibited\r\n";
                    }
                    //===============HPGe HVPS Inhibit Status==================
                    strOptions += "INH Polarity: ";
                    if (HPGe_HV_INH_POL) {
                        strOptions += "high\r\n";
                    } else {
                        strOptions += "low\r\n";
                    }
                //} else {
                //    strOptions += "PX5 Options: None\r\n";  //           strOptions += "No Options Installed"
                }
            }
            return strOptions; 
        }

        // Use GetStatusValueStrings includes values for detector and board monitors
        public string GetStatusValueStrings() 
        { 
            string strConfig="";
            string strTemp;

            if (!bHaveStatus)
            {
                return strConfig;
            }

            strTemp = GetDeviceNameFromVal(DEVICE_ID);
            strConfig = "Device Type: " + strTemp + "\r\n";
            strTemp = String.Format("Serial Number: {0:d}\r\n",SerialNumber);	//SerialNumber
            strConfig += strTemp;
            strTemp = "Firmware: " + VersionToStr(Firmware);   
            strConfig += strTemp;
            if (Firmware > 0x65) {
                strTemp = String.Format("  Build:  {0:d}\r\n", Build);
                strConfig += strTemp;
            } else {
                strConfig += "\r\n";
            }
            strTemp = "FPGA: " + VersionToStr(FPGA) + "\r\n"; 
            strConfig += strTemp;
            if (DEVICE_ID != 3) {
                strTemp = String.Format("Fast Count: {0:f}\r\n",FastCount);   	//FastCount
                strConfig += strTemp;
            }
            strTemp = String.Format("Slow Count: {0:f}\r\n",SlowCount);   	//SlowCount
            strConfig += strTemp;

            if (DEVICE_ID != dppMCA8000D) {
                strTemp = String.Format("Accumulation Time: {0:f2}\r\n",AccumulationTime);	    //AccumulationTime
                strConfig += strTemp;
            }

            strTemp = String.Format("Real Time: {0:f2}\r\n",RealTime);	    //RealTime
            strConfig += strTemp;

            if (DEVICE_ID == dppMCA8000D) {
                strTemp = String.Format("Live Time: {0:f2}\r\n",LiveTime);	    //RealTime
                strConfig += strTemp;
            }

           if ((DEVICE_ID != dppDP5G) && (DEVICE_ID != dppMCA8000D)) {
                strTemp = String.Format("Detector Temp: {0:f}K\r\n",DET_TEMP);		//"##0°C") ' round to nearest degree
                strConfig += strTemp;
                strTemp = String.Format("Detector HV: {0:f}V\r\n",HV);
                strConfig += strTemp;
                strTemp = String.Format("Board Temp: {0:d}°C\r\n",(int)DP5_TEMP);
                strConfig += strTemp;
            } else if (DEVICE_ID == dppDP5G) {		// GAMMARAD5
                if (DET_TEMP > 0) {
                    strTemp = String.Format("Detector Temp: {0:f1}K\r\n", DET_TEMP);
                    strConfig += strTemp;
                } else {
                    strConfig += "";
                }
                strTemp = String.Format("HV Set: {0:f}V\r\n",HV);
                strConfig += strTemp;
            } else if (DEVICE_ID == dppMCA8000D) {		// Digital MCA
                strTemp = String.Format("Board Temp: {0:d}°C\r\n", (int)DP5_TEMP);
                strConfig += strTemp;
            }

            if (DEVICE_ID == dppPX5) {
                strTemp = PX5_OptionsString();
                strConfig += strTemp;
            }
            return strConfig;
        }

        //void CDP5Status::Process_Diagnostics(Packet_In PIN, DiagDataType *dd, int device_type)
        //{
        //    long idxVal;
        //    string strVal;
        //    double DP5_ADC_Gain[10];  // convert each ADC count to engineering units - values calculated in FORM.LOAD
        //    double PC5_ADC_Gain[3];
        //    double PX5_ADC_Gain[12];

        //    DP5_ADC_Gain[0] = 1.0 / 0.00286;                // 2.86mV/C
        //    DP5_ADC_Gain[1] = 1.0;                          // Vdd mon (out-of-scale)
        //    DP5_ADC_Gain[2] = (30.1 + 20.0) / 20.0;           // PWR
        //    DP5_ADC_Gain[3] = (13.0 + 20.0) / 20.0;            // 3.3V
        //    DP5_ADC_Gain[4] = (4.99 + 20.0) / 20.0;         // 2.5V
        //    DP5_ADC_Gain[5] = 1.0;                          // 1.2V
        //    DP5_ADC_Gain[6] = (35.7 + 20.0) / 20.0;          // 5.5V
        //    DP5_ADC_Gain[7] = (35.7 + 75.0) / 35.7;        // -5.5V (this one is tricky)
        //    DP5_ADC_Gain[8] = 1.0;                          // AN_IN
        //    DP5_ADC_Gain[9] = 1.0;                          // VREF_IN

        //    PC5_ADC_Gain[0] = 500.0;                        // HV: 1500V/3V
        //    PC5_ADC_Gain[1] = 100.0;                        // TEC: 300K/3V
        //    PC5_ADC_Gain[2] = (20.0 + 10.0) / 10.0;            // +8.5/5V

        //    //PX5_ADC_Gain[0] = (30.1 + 20.0) / 20.0;          // PWR
        //    PX5_ADC_Gain[0] = (69.8 + 20.0) / 20.0;          // 9V (was originally PWR)
        //    PX5_ADC_Gain[1] = (13.0 + 20.0) / 20.0;            // 3.3V
        //    PX5_ADC_Gain[2] = (4.99 + 20.0) / 20.0;          // 2.5V
        //    PX5_ADC_Gain[3] = 1.0;                         // 1.2V
        //    PX5_ADC_Gain[4] = (30.1 + 20.0) / 20.0;          // 5V
        //    PX5_ADC_Gain[5] = (10.7 + 75.0) / 10.7;         // -5V (this one is tricky)
        //    PX5_ADC_Gain[6] = (64.9 + 20.0) / 20.0;          // +PA
        //    PX5_ADC_Gain[7] = (10.7 + 75) / 10.7;        // -PA
        //    PX5_ADC_Gain[8] = (16.0 + 20.0) / 20.0;            // +TEC
        //    PX5_ADC_Gain[9] = 500.0;                       // HV: 1500V/3V
        //    PX5_ADC_Gain[10] = 100.0;                       // TEC: 300K/3V
        //    PX5_ADC_Gain[11] = 1.0 / 0.00286;               // 2.86mV/C

        //    dd->Firmware = PIN.DATA[0];
        //    dd->FPGA = PIN.DATA[1];
        //    strVal = "0x0" + FmtHex(PIN.DATA[2], 2) + FmtHex(PIN.DATA[3], 2) + FmtHex(PIN.DATA[4], 2);
        //    dd->SRAMTestData = strtol(strVal,NULL,0);
        //    dd->SRAMTestPass = (dd->SRAMTestData == 0xFFFFFF);
        //    dd->TempOffset = PIN.DATA[180] + 256 * (PIN.DATA[180] > 127);  // 8-bit signed value

        //    if (device_type == devtypeDP5) {
        //        for(idxVal=0;idxVal<10;idxVal++){
        //            dd->ADC_V[idxVal] = (float)((((PIN.DATA[5 + idxVal * 2] & 3) * 256) + PIN.DATA[6 + idxVal * 2]) * 2.44 / 1024.0 * DP5_ADC_Gain[idxVal]); // convert counts to engineering units (C or V)
        //        }
        //        dd->ADC_V[7] = dd->ADC_V[7] + dd->ADC_V[6] * (float)(1.0 - DP5_ADC_Gain[7]);  // -5.5V is a function of +5.5V
        //        dd->strTempRaw = String.Format("%   #.0f0C", dd->ADC_V[0] - 271.3);
        //        dd->strTempCal = String.Format("%   #.0f0C", (dd->ADC_V[0] - 280.0 + dd->TempOffset));
        //    } else if (device_type == devtypePX5) {
        //        for(idxVal=0;idxVal<11;idxVal++){
        //            dd->ADC_V[idxVal] = (float)((((PIN.DATA[5 + idxVal * 2] & 15) * 256) + PIN.DATA[6 + idxVal * 2]) * 3.0 / 4096.0 * PX5_ADC_Gain[idxVal]);   // convert counts to engineering units (C or V)
        //        }
        //        dd->ADC_V[11] = (float)((((PIN.DATA[5 + 11 * 2] & 3) * 256) + PIN.DATA[6 + 11 * 2]) * 3.0 / 1024.0 * PX5_ADC_Gain[11]);  // convert counts to engineering units (C or V)
        //        dd->ADC_V[5] = (float)(dd->ADC_V[5] - (3.0 * PX5_ADC_Gain[5]) + 3.0); // -5V uses +3VR
        //        dd->ADC_V[7] = (float)(dd->ADC_V[7] - (3.0 * PX5_ADC_Gain[7]) + 3.0); // -PA uses +3VR
        //        dd->strTempRaw = String.Format("%#.1fC", dd->ADC_V[11] - 271.3);
        //        dd->strTempCal = String.Format("%#.1fC", (dd->ADC_V[11] - 280.0 + dd->TempOffset));
        //    }	

        //    dd->PC5_PRESENT = FALSE;  // assume no PC5, then check to see if there are any non-zero bytes
        //    for(idxVal=25;idxVal<=38;idxVal++) {
        //        if (PIN.DATA[idxVal] > 0) {
        //            dd->PC5_PRESENT = TRUE;
        //            break;
        //        }
        //    }

        //    if (dd->PC5_PRESENT) {
        //        for(idxVal=0;idxVal<=2;idxVal++) {
        //            dd->PC5_V[idxVal] = (float)((((PIN.DATA[25 + idxVal * 2] & 15) * 256) + PIN.DATA[26 + idxVal * 2]) * 3.0 / 4096.0 * PC5_ADC_Gain[idxVal]); // convert counts to engineering units (C or V)
        //        }
        //        if (PIN.DATA[34] < 128) {
        //            dd->PC5_SN = (ULONG)GetULong(31, PIN.DATA);
        //        } else {
        //            dd->PC5_SN = -1; // no PC5 S/N
        //        }
        //        if ((PIN.DATA[35] == 255) && (PIN.DATA[36] == 255)) {
        //            dd->PC5Initialized = FALSE;
        //            dd->PC5DCAL = 0;
        //        } else {
        //            dd->PC5Initialized = TRUE;
        //            dd->PC5DCAL = (float)(((float)(PIN.DATA[35]) * 256.0 + (float)(PIN.DATA[36])) * 3.0 / 4096.0);
        //        }
        //        dd->IsPosHV = ((PIN.DATA[37] & 128) == 128);
        //        dd->Is8_5VPreAmp = ((PIN.DATA[37] & 64) == 64);
        //        dd->Sup9VOn = ((PIN.DATA[38] & 8) == 8);
        //        dd->PreAmpON = ((PIN.DATA[38] & 4) == 4);
        //        dd->HVOn = ((PIN.DATA[38] & 2) == 2);
        //        dd->TECOn = ((PIN.DATA[38] & 1) == 1);
        //    } else {
        //        for(idxVal=0;idxVal<=2;idxVal++) {
        //            dd->PC5_V[idxVal] = 0;
        //        }
        //        dd->PC5_SN = -1; // no PC5 S/N
        //        dd->PC5Initialized = FALSE;
        //        dd->PC5DCAL = 0;
        //        dd->IsPosHV = FALSE;
        //        dd->Is8_5VPreAmp = FALSE;
        //        dd->Sup9VOn = FALSE;
        //        dd->PreAmpON = FALSE;
        //        dd->HVOn = FALSE;
        //        dd->TECOn = FALSE;
        //    }
        //    for(idxVal=0;idxVal<=191;idxVal++) {
        //        dd->DiagData[idxVal] = PIN.DATA[idxVal + 39];
        //    }
        //    //string cstrData;
        //    //cstrData = DisplayBufferArray(PIN.DATA, 256);
        //    //SaveStringDataToFile(cstrData);
        //}

        //string CDP5Status::DiagnosticsToString(DiagDataType dd, int device_type)
        //{
        //    long idxVal;
        //    string cstrVal;
        //    string strDiag;

        //    strDiag = "Firmware: " + VersionToStr(dd.Firmware) + "\r\n";
        //    strDiag += "FPGA: " + VersionToStr(dd.FPGA) + "\r\n";
        //    strDiag += "SRAM Test: ";
        //    if (dd.SRAMTestPass) {
        //        strDiag += "PASS\r\n";
        //    } else {
        //        strDiag += "ERROR @ 0x" + FmtHex(dd.SRAMTestData, 6) + "\r\n";
        //    }

        //    if (device_type == devtypeDP5) {
        //        strDiag += "DP5 Temp (raw): " + dd.strTempRaw + "\r\n";
        //        strDiag += "DP5 Temp (cal'd): " + dd.strTempCal + "\r\n";
        //        strDiag += "PWR: " + FmtPc5Pwr(dd.ADC_V[2]) + "\r\n";
        //        strDiag += "3.3V: " + FmtPc5Pwr(dd.ADC_V[3]) + "\r\n";
        //        strDiag += "2.5V: " + FmtPc5Pwr(dd.ADC_V[4]) + "\r\n";
        //        strDiag += "1.2V: " + FmtPc5Pwr(dd.ADC_V[5]) + "\r\n";
        //        strDiag += "+5.5V: " + FmtPc5Pwr(dd.ADC_V[6]) + "\r\n";
        //        strDiag += "-5.5V: " + FmtPc5Pwr(dd.ADC_V[7]) + "\r\n";
        //        strDiag += "AN_IN: " + FmtPc5Pwr(dd.ADC_V[8]) + "\r\n";
        //        strDiag += "VREF_IN: " + FmtPc5Pwr(dd.ADC_V[9]) + "\r\n";

        //        strDiag += "\r\n";
        //        if (dd.PC5_PRESENT) {
        //            strDiag += "PC5: Present\r\n";
        //            cstrVal = String.Format("%dV",(int)(dd.PC5_V[0]));
        //            strDiag += "HV: " + cstrVal + "\r\n";
        //            cstrVal = String.Format("%#.1fK",dd.PC5_V[1]);
        //            strDiag += "Detector Temp: " + cstrVal + "\r\n";
        //            strDiag += "+8.5/5V: " + FmtPc5Pwr(dd.PC5_V[2]) + "\r\n";
        //            if (dd.PC5_SN > -1) {
        //                strDiag += "PC5 S/N: " + FmtLng(dd.PC5_SN) + "\r\n";
        //            } else {
        //                strDiag += "PC5 S/N: none\r\n";
        //            }
        //            if (dd.PC5Initialized) {
        //                strDiag += "PC5 DCAL: " + FmtPc5Pwr(dd.PC5DCAL) + "\r\n";
        //            } else {
        //                strDiag += "PC5 DCAL: Uninitialized\r\n";
        //            }
        //            strDiag += "PC5 Flavor: ";
        //            strDiag += IsAorB(dd.IsPosHV, "+HV, ", "-HV, ");
        //            strDiag += IsAorB(dd.Is8_5VPreAmp, "8.5V preamp", "5V preamp") + "\r\n";
        //            strDiag += "PC5 Supplies:\r\n";
        //            strDiag += "9V: " + OnOffStr(dd.Sup9VOn) + "\r\n";
        //            strDiag += "Preamp: " + OnOffStr(dd.PreAmpON) + "\r\n";
        //            strDiag += "HV: " + OnOffStr(dd.HVOn) + "\r\n";
        //            strDiag += "TEC: " + OnOffStr(dd.TECOn) + "\r\n";
        //        } else {
        //            strDiag += "PC5: Not Present\r\n";
        //        }
        //    } else if (device_type == devtypePX5) {
        //        strDiag += "PX5 Temp (raw): " + dd.strTempRaw + "\r\n";
        //        strDiag += "PX5 Temp (cal'd): " + dd.strTempCal + "\r\n";
        //        //strDiag += "PWR: " + FmtPc5Pwr(dd.ADC_V[0]) + "\r\n";
        //        strDiag += "9V: " + FmtPc5Pwr(dd.ADC_V[0]) + "\r\n";
        //        strDiag += "3.3V: " + FmtPc5Pwr(dd.ADC_V[1]) + "\r\n";
        //        strDiag += "2.5V: " + FmtPc5Pwr(dd.ADC_V[2]) + "\r\n";
        //        strDiag += "1.2V: " + FmtPc5Pwr(dd.ADC_V[3]) + "\r\n";
        //        strDiag += "+5V: " + FmtPc5Pwr(dd.ADC_V[4]) + "\r\n";
        //        strDiag += "-5V: " + FmtPc5Pwr(dd.ADC_V[5]) + "\r\n";
        //        strDiag += "+PA: " + FmtPc5Pwr(dd.ADC_V[6]) + "\r\n";
        //        strDiag += "-PA: " + FmtPc5Pwr(dd.ADC_V[7]) + "\r\n";
        //        strDiag += "TEC: " + FmtPc5Pwr(dd.ADC_V[8]) + "\r\n";
        //        strDiag += "ABS(HV): " + FmtHvPwr(dd.ADC_V[9]) + "\r\n";
        //        strDiag += "DET_TEMP: " + FmtPc5Temp(dd.ADC_V[10]) + "\r\n";
        //    }

        //    strDiag += "\r\nDiagnostic Data\r\n";
        //    strDiag += "---------------\r\n";
        //    for(idxVal=0;idxVal<=191;idxVal++) {
        //        if ((idxVal % 8) == 0) { 
        //            strDiag += FmtHex(idxVal, 2) + ":";
        //        }
        //        strDiag += FmtHex(dd.DiagData[idxVal], 2) + " ";
        //        if ((idxVal % 8) == 7) { 
        //            strDiag += "\r\n";
        //        }
        //    }
        //    return (strDiag);
        //}

        //string CDP5Status::FmtHvPwr(float fVal) 
        //{
        //    string cstrVal;
        //    cstrVal = String.Format("%#.1fV", fVal);	// "#.##0V"
        //    return cstrVal;
        //}

        //string CDP5Status::FmtPc5Pwr(float fVal) 
        //{
        //    string cstrVal;
        //    cstrVal = String.Format("%#.3fV", fVal);	// "#.##0V"
        //    return cstrVal;
        //}

        //string CDP5Status::FmtPc5Temp(float fVal) 
        //{
        //    string cstrVal;
        //    cstrVal = String.Format("%#.1fK", fVal);	// "#.##0V"
        //    return cstrVal;
        //}

        //string CDP5Status::FmtHex(long FmtHex, long HexDig) 
        //{
        //    string cstrHex;
        //    string cstrFmt;
        //    cstrFmt = String.Format("%d",HexDig);		// max size of 0 pad
        //    cstrFmt = "%0" + cstrFmt + "X";		// string format specifier
        //    cstrHex = String.Format(cstrFmt, FmtHex);	// create padded string
        //    return cstrHex;
        //}

        //string CDP5Status::FmtLng(long lVal) 
        //{
        //    string cstrVal;
        //    cstrVal = String.Format("%d", lVal);
        //    return cstrVal;
        //}

        public string VersionToStr(byte bVersion)
        {
            string cstrVerMajor = String.Format("{0:d}", ((bVersion & 0xF0) / 16));
            string cstrVerMinor = String.Format("{0:d2}", (bVersion & 0x0F)); ;
            string cstrVer;
            cstrVer = cstrVerMajor + "." + cstrVerMinor;
            return (cstrVer);
        }

        public string OnOffStr(bool bOn)
        {
            if (bOn)
            {
                return ("ON");
            }
            else
            {
                return ("OFF");
            }
        }

        string IsAorB(bool bIsA, string strA, string strB)
        {
            if (bIsA)
            {
                return (strA);
            }
            else
            {
                return (strB);
            }
        }

        public string GetDeviceNameFromVal(int DeviceTypeVal)
        {
            string cstrDeviceType;
            switch (DeviceTypeVal)
            {
                case 0:
                    cstrDeviceType = "DP5";
                    break;
                case 1:
                    cstrDeviceType = "PX5";
                    break;
                case 2:
                    cstrDeviceType = "DP5G";
                    break;
                case 3:
                    cstrDeviceType = "MCA8000D";
                    break;
                case 4:
                    cstrDeviceType = "TB5";
                    break;
                default:           //if unknown set to DP5
                    cstrDeviceType = "DP5";
                    break;
            }
            return cstrDeviceType;
        }

        //string CDP5Status::DisplayBufferArray(byte buffer[], ULONG bufSizeIn)
        //{
        //    ULONG i;
        //    string cstrVal("");
        //    string cstrMsg("");
        //    for(i=0;i<bufSizeIn;i++) {
        //        cstrVal = String.Format("%.2X ",buffer[i]);
        //        cstrMsg += cstrVal;
        //        //if (((i+1) % 16) == 0 ) { 
        //        //	cstrMsg += "\r\n";
        //        //} else 
        //        if (((i+1) % 8) == 0 ) {
        //        //	cstrMsg += "   ";
        //            //cstrMsg += "\r\n";
        //            cstrMsg += "\n";
        //        }
        //    }
        //    //cstrMsg += "\n";
        //    return cstrMsg;
        //}

        //void CDP5Status::SaveStringDataToFile(string strData)
        //{
        //   FILE  *out;
        //   string strFilename;
        //   string strError;

        //   strFilename = "vcDP5_Data.txt";

        //   if ( (out = fopen(strFilename,"w")) == (FILE *) NULL)
        //      strError = String.Format("Couldn't open %s for writing.\n", strFilename);
        //   else
        //   {
        //      fprintf(out,"%s\n",strData);
        //   }
        //   fclose(out);
        //}


        //#pragma once

        //#include "DP5Protocol.h"
        //#include "dpputilities.h"

        //typedef enum _PX5_OPTIONS
        //{
        //    PX5_OPTION_NONE,
        //    PX5_OPTION_HPGe_HVPS
        //} PX5_OPTIONS;


        //typedef struct _DiagDataType
        //{
        //    float ADC_V[11];
        //    float PC5_V[3];
        //    bool PC5_PRESENT;
        //    long PC5_SN;
        //    byte Firmware;
        //    byte FPGA;
        //    bool SRAMTestPass;
        //    long SRAMTestData;
        //    int TempOffset;
        //    string strTempRaw;
        //    string strTempCal;
        //    bool PC5Initialized;
        //    float PC5DCAL;
        //    bool IsPosHV;
        //    bool Is8_5VPreAmp;
        //    bool Sup9VOn;
        //    bool PreAmpON;
        //    bool HVOn;
        //    bool TECOn;
        //    byte DiagData[192];
        //} DiagDataType, *PDDiagDataType;






        //// calculate major.minor version from BYTE, convert/save to double
        //double CDppUtilities::BYTEVersionToDouble(BYTE Version)
        //{
        //    double dblVersion;
        //    CString strTemp;
        //    strTemp = String.Format("%d.%02d",((Version & 240) / 16),(Version & 15));
        //    dblVersion = atof(strTemp);
        //    return dblVersion;
        //}

        //// calculate major.minor version from BYTE, convert/save to CString
        //CString CDppUtilities::BYTEVersionToString(BYTE Version)
        //{
        //    CString strTemp;
        //    strTemp = String.Format("%d.%02d",((Version & 240) / 16),(Version & 15));
        //    return strTemp;
        //}

    }
}