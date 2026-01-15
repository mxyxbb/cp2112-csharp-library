using System;

namespace Cp2112Sdk.Exceptions
{
    /// <summary>
    /// Base exception for CP2112 device errors
    /// </summary>
    public class Cp2112Exception : Exception
    {
        public int ErrorCode { get; }

        public Cp2112Exception(int errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public Cp2112Exception(int errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when device is not found
    /// </summary>
    public class DeviceNotFoundException : Cp2112Exception
    {
        public DeviceNotFoundException()
            : base(Native.NativeMethods.HID_SMBUS_DEVICE_NOT_FOUND,
                  "CP2112 device not found. Please ensure the device is connected.")
        {
        }

        public DeviceNotFoundException(string message)
            : base(Native.NativeMethods.HID_SMBUS_DEVICE_NOT_FOUND, message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when device handle is invalid
    /// </summary>
    public class InvalidHandleException : Cp2112Exception
    {
        public InvalidHandleException()
            : base(Native.NativeMethods.HID_SMBUS_INVALID_HANDLE,
                  "Invalid device handle. Device may not be opened properly.")
        {
        }
    }

    /// <summary>
    /// Exception thrown when a parameter is invalid
    /// </summary>
    public class InvalidParameterException : Cp2112Exception
    {
        public InvalidParameterException(string paramName)
            : base(Native.NativeMethods.HID_SMBUS_INVALID_PARAMETER,
                  $"Invalid parameter: {paramName}")
        {
        }
    }

    /// <summary>
    /// Exception thrown when a read operation fails
    /// </summary>
    public class ReadException : Cp2112Exception
    {
        public ReadException(string message)
            : base(Native.NativeMethods.HID_SMBUS_READ_ERROR, message)
        {
        }

        public ReadException(string message, Exception innerException)
            : base(Native.NativeMethods.HID_SMBUS_READ_ERROR, message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a read operation times out
    /// </summary>
    public class ReadTimeoutException : Cp2112Exception
    {
        public ReadTimeoutException()
            : base(Native.NativeMethods.HID_SMBUS_READ_TIMED_OUT,
                  "Read operation timed out.")
        {
        }

        public ReadTimeoutException(string message)
            : base(Native.NativeMethods.HID_SMBUS_READ_TIMED_OUT, message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a write operation fails
    /// </summary>
    public class WriteException : Cp2112Exception
    {
        public WriteException(string message)
            : base(Native.NativeMethods.HID_SMBUS_WRITE_ERROR, message)
        {
        }

        public WriteException(string message, Exception innerException)
            : base(Native.NativeMethods.HID_SMBUS_WRITE_ERROR, message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a write operation times out
    /// </summary>
    public class WriteTimeoutException : Cp2112Exception
    {
        public WriteTimeoutException()
            : base(Native.NativeMethods.HID_SMBUS_WRITE_TIMED_OUT,
                  "Write operation timed out.")
        {
        }

        public WriteTimeoutException(string message)
            : base(Native.NativeMethods.HID_SMBUS_WRITE_TIMED_OUT, message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when device I/O fails
    /// </summary>
    public class DeviceIOException : Cp2112Exception
    {
        public DeviceIOException(string message)
            : base(Native.NativeMethods.HID_SMBUS_DEVICE_IO_FAILED, message)
        {
        }

        public DeviceIOException(string message, Exception innerException)
            : base(Native.NativeMethods.HID_SMBUS_DEVICE_IO_FAILED, message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when device operation fails
    /// </summary>
    public class DeviceAccessException : Cp2112Exception
    {
        public DeviceAccessException(string message)
            : base(Native.NativeMethods.HID_SMBUS_DEVICE_ACCESS_ERROR, message)
        {
        }
    }
}
