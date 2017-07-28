using System;
using System.Collections.Generic;
using System.Text;

namespace csRepeat
{
    public abstract class DPDevice
    {
        public enum InterfaceTypes
        {
            ethernet,
            rs232,
            usb
        }

        //public InterfaceTypes InterfaceType { get; set; }
        private InterfaceTypes _InterfaceType;
        public InterfaceTypes InterfaceType {
            get { return _InterfaceType; }
            set { _InterfaceType = value; }
        }

        //public DeviceTypes DeviceType { get; set; }
        private DeviceTypes _DeviceType;
        public DeviceTypes DeviceType {
            get { return _DeviceType; }
            set { _DeviceType = value; }
        }

        public Version FirmwareVersion;
        public Version FpgaVersion;

        //public string SerialNumber { get; set; }
        private string _SerialNumber;
        public string SerialNumber {
            get { return _SerialNumber; }
            set { _SerialNumber = value; }
        }

        public DPDevice(InterfaceTypes interfaceType,
                        DeviceTypes deviceType,
                        Version firmwareVersion,
                        Version fpgaVersion,
                        string serialNumber)
        {
            this.InterfaceType = interfaceType;
            this.DeviceType = deviceType;
            this.FirmwareVersion = firmwareVersion;
            this.FpgaVersion = fpgaVersion;
            this.SerialNumber = serialNumber;

            // default to Normal state
            FpgaState = FpgaStates.Normal;

            // see if we saw an encryption failure
            if (fpgaVersion.Equals(FpgaEncryptionFailureVersion))
            {
                FpgaState = FpgaStates.EncryptionFailure;
            }
        }

        /// <summary>
        /// Possible fpga states
        /// </summary>
        public enum FpgaStates
        {
            /// <summary>
            /// Normal fpga state
            /// </summary>
            Normal,

            /// <summary>
            /// Fpga reports an encryption failure
            /// </summary>
            EncryptionFailure,

            /// <summary>
            /// Fpga image is unprogrammed
            /// </summary>
            Unprogrammed
        }

        /// <summary>
        /// 14.14 is fpga encryption failure
        /// </summary>
        public static readonly Version FpgaEncryptionFailureVersion = new Version(14, 14);

        /// <summary>
        /// Fpga states, defaults to 'Normal'
        /// </summary>
        
        private FpgaStates _FpgaState;
        public FpgaStates FpgaState
        {
            get { return _FpgaState; }
            internal set { _FpgaState = value; }
        }

        public delegate void UploadProgressUpdateDelegate(long currentByteOffset, long totalBytes);

        public delegate bool HasFirmwareLoadBeenCancelledDelegate();
        public HasFirmwareLoadBeenCancelledDelegate HasFirmwareLoadBeenCancelled;

        public static List<DPDevice> PerformDeviceDetection()
        {
            List<DPDevice> devices = new List<DPDevice>();
            List<DPDevice> fw6UsbDevices = FW6.DPDeviceUsb.DetectDevices();
            List<DPDevice> fw6SerialDevices = FW6.DPDeviceSerialFW6.DetectDevices();
            List<DPDevice> fw6EthernetDevices = FW6.DPDeviceEthernet.DetectDevices();
            
            if (fw6EthernetDevices != null)
            {
                foreach (DPDevice a in fw6EthernetDevices)
                {
                    devices.Add(a);
                }
            }

            if (fw6SerialDevices != null)
            {
                foreach (DPDevice a in fw6SerialDevices)
                {
                    devices.Add(a);
                }
            }

            if (fw6UsbDevices != null)
            {
                foreach (DPDevice a in fw6UsbDevices)
                {
                    devices.Add(a);
                }
            }
            return devices;
        }


        public override string ToString()
        {
            return String.Format("Amptek {0} - S/N {1} - uC Ver {2}  FPGA Ver {3} - {4}",
                                 DeviceType, SerialNumber, DisplayVersion.VersionString(this, FirmwareVersion, false), DisplayVersion.VersionString(this, FpgaVersion, true), InterfaceType);
        }
    }
}
