using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Cp2112Sdk.Native
{
    /// <summary>
    /// P/Invoke declarations for SLABHIDtoSMBus.dll
    /// </summary>
    internal static class NativeMethods
    {
        private const string DLL_NAME = "SLABHIDtoSMBus.dll";

        #region Return Codes

        public const int HID_SMBUS_SUCCESS = 0x00;
        public const int HID_SMBUS_DEVICE_NOT_FOUND = 0x01;
        public const int HID_SMBUS_INVALID_HANDLE = 0x02;
        public const int HID_SMBUS_INVALID_DEVICE_OBJECT = 0x03;
        public const int HID_SMBUS_INVALID_PARAMETER = 0x04;
        public const int HID_SMBUS_INVALID_REQUEST_LENGTH = 0x05;
        public const int HID_SMBUS_READ_ERROR = 0x10;
        public const int HID_SMBUS_WRITE_ERROR = 0x11;
        public const int HID_SMBUS_READ_TIMED_OUT = 0x12;
        public const int HID_SMBUS_WRITE_TIMED_OUT = 0x13;
        public const int HID_SMBUS_DEVICE_IO_FAILED = 0x14;
        public const int HID_SMBUS_DEVICE_ACCESS_ERROR = 0x15;
        public const int HID_SMBUS_DEVICE_NOT_SUPPORTED = 0x16;
        public const int HID_SMBUS_UNKNOWN_ERROR = 0xFF;

        #endregion

        #region Transfer Status Codes

        public const byte HID_SMBUS_S0_IDLE = 0x00;
        public const byte HID_SMBUS_S0_BUSY = 0x01;
        public const byte HID_SMBUS_S0_COMPLETE = 0x02;
        public const byte HID_SMBUS_S0_ERROR = 0x03;

        public const byte HID_SMBUS_S1_BUSY_ADDRESS_ACKED = 0x00;
        public const byte HID_SMBUS_S1_BUSY_ADDRESS_NACKED = 0x01;
        public const byte HID_SMBUS_S1_BUSY_READING = 0x02;
        public const byte HID_SMBUS_S1_BUSY_WRITING = 0x03;

        #endregion

        #region SMBus Constants

        public const ushort HID_SMBUS_MIN_READ_REQUEST_SIZE = 1;
        public const ushort HID_SMBUS_MAX_READ_REQUEST_SIZE = 512;
        public const byte HID_SMBUS_MIN_TARGET_ADDRESS_SIZE = 1;
        public const byte HID_SMBUS_MAX_TARGET_ADDRESS_SIZE = 16;
        public const byte HID_SMBUS_MAX_READ_RESPONSE_SIZE = 61;
        public const byte HID_SMBUS_MIN_WRITE_REQUEST_SIZE = 1;
        public const byte HID_SMBUS_MAX_WRITE_REQUEST_SIZE = 61;

        #endregion

        #region String Types

        public const uint HID_SMBUS_GET_SERIAL_STR = 0x04;
        public const uint HID_SMBUS_GET_MANUFACTURER_STR = 0x05;
        public const uint HID_SMBUS_GET_PRODUCT_STR = 0x06;

        #endregion

        #region DLL Imports

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_GetNumDevices(
            out uint numDevices,
            ushort vid,
            ushort pid);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_GetString(
            uint deviceNum,
            ushort vid,
            ushort pid,
            StringBuilder deviceString,
            uint options);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_GetOpenedString(
            IntPtr device,
            StringBuilder deviceString,
            uint options);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_Open(
            out IntPtr device,
            uint deviceNum,
            ushort vid,
            ushort pid);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_Close(
            IntPtr device);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_IsOpened(
            IntPtr device,
            out bool opened);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_ReadRequest(
            IntPtr device,
            byte slaveAddress,
            ushort numBytesToRead);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_AddressReadRequest(
            IntPtr device,
            byte slaveAddress,
            ushort numBytesToRead,
            byte targetAddressSize,
            byte[] targetAddress);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_ForceReadResponse(
            IntPtr device,
            ushort numBytesToRead);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_GetReadResponse(
            IntPtr device,
            out byte status,
            byte[] buffer,
            byte bufferSize,
            out byte numBytesRead);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_WriteRequest(
            IntPtr device,
            byte slaveAddress,
            byte[] buffer,
            byte numBytesToWrite);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_TransferStatusRequest(
            IntPtr device);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_GetTransferStatusResponse(
            IntPtr device,
            out byte status0,
            out byte status1,
            out ushort numRetries,
            out ushort bytesRead);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_CancelTransfer(
            IntPtr device);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_SetTimeouts(
            IntPtr device,
            uint responseTimeout);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_SetSmbusConfig(
            IntPtr device,
            uint bitRate,
            byte address,
            bool autoReadRespond,
            ushort writeTimeout,
            ushort readTimeout,
            bool sclLowTimeout,
            ushort transferRetries);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_GetSmbusConfig(
            IntPtr device,
            out uint bitRate,
            out byte address,
            out bool autoReadRespond,
            out ushort writeTimeout,
            out ushort readTimeout,
            out bool sclLowTimeout,
            out ushort transferRetries);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_Reset(
            IntPtr device);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HidSmbus_GetLibraryVersion(
            out byte major,
            out byte minor,
            out bool release);

        #endregion
    }
}
