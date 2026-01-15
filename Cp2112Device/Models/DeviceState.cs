using System;

namespace Cp2112Sdk.Models
{
    /// <summary>
    /// Device connection state
    /// </summary>
    public enum DeviceState
    {
        /// <summary>Device is closed and not initialized</summary>
        Closed,

        /// <summary>Device is opening</summary>
        Opening,

        /// <summary>Device is open and ready</summary>
        Open,

        /// <summary>Device is configured</summary>
        Configured,

        /// <summary>Device is busy transferring data</summary>
        Busy,

        /// <summary>Device has encountered an error</summary>
        Error,

        /// <summary>Device is closing</summary>
        Closing
    }

    /// <summary>
    /// SMBus configuration parameters
    /// </summary>
    public class SmbusConfig
    {
        /// <summary>Bit rate in Hz (default: 100kHz)</summary>
        public uint BitRate { get; set; } = 100000;

        /// <summary>ACK address (default: 0x02)</summary>
        public byte AckAddress { get; set; } = 0x02;

        /// <summary>Auto read respond (default: false)</summary>
        public bool AutoReadRespond { get; set; } = false;

        /// <summary>Write timeout in ms (default: 10ms)</summary>
        public ushort WriteTimeout { get; set; } = 10;

        /// <summary>Read timeout in ms (default: 10ms)</summary>
        public ushort ReadTimeout { get; set; } = 10;

        /// <summary>SCL low timeout (default: true)</summary>
        public bool SclLowTimeout { get; set; } = true;

        /// <summary>Transfer retries (default: 1)</summary>
        public ushort TransferRetries { get; set; } = 1;

        /// <summary>Response timeout in ms (default: 100ms)</summary>
        public uint ResponseTimeout { get; set; } = 100;

        /// <summary>Create default configuration</summary>
        public static SmbusConfig Default => new SmbusConfig();

        /// <summary>Create high-speed configuration (400kHz)</summary>
        public static SmbusConfig HighSpeed => new SmbusConfig
        {
            BitRate = 400000
        };

        /// <summary>Create low-speed configuration (10kHz)</summary>
        public static SmbusConfig LowSpeed => new SmbusConfig
        {
            BitRate = 10000,
            WriteTimeout = 100,
            ReadTimeout = 100,
            ResponseTimeout = 500
        };
    }

    /// <summary>
    /// Device information
    /// </summary>
    public class DeviceInfo
    {
        public uint DeviceNumber { get; set; }
        public ushort VendorId { get; set; }
        public ushort ProductId { get; set; }
        public string SerialNumber { get; set; }
        public string Manufacturer { get; set; }
        public string Product { get; set; }

        public override string ToString()
        {
            return $"Device {DeviceNumber}: VID=0x{VendorId:X4}, PID=0x{ProductId:X4}, SN={SerialNumber}";
        }
    }

    /// <summary>
    /// Transfer status
    /// </summary>
    public class TransferStatus
    {
        public byte Status0 { get; set; }
        public byte Status1 { get; set; }
        public ushort NumRetries { get; set; }
        public ushort BytesRead { get; set; }
        public bool IsComplete => Status0 == Native.NativeMethods.HID_SMBUS_S0_COMPLETE;
        public bool IsBusy => Status0 == Native.NativeMethods.HID_SMBUS_S0_BUSY;
        public bool IsError => Status0 == Native.NativeMethods.HID_SMBUS_S0_ERROR;
        public bool IsIdle => Status0 == Native.NativeMethods.HID_SMBUS_S0_IDLE;
    }
}
