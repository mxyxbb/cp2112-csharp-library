using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Cp2112Sdk.Models;
using Cp2112Sdk.Events;
using Cp2112Sdk.Exceptions;
using Cp2112Sdk.Native;

namespace Cp2112Sdk
{
    /// <summary>
    /// CP2112 SMBus Device Controller
    /// Thread-safe, disposable device controller with async support
    /// </summary>
    public class Cp2112Device : IDisposable
    {
        #region Constants

        private const ushort DEFAULT_VID = 0x10C4;
        private const ushort DEFAULT_PID = 0xEA90;
        private const int MAX_RETRY_ATTEMPTS = 1;
        private const int RETRY_DELAY_MS = 50;

        #endregion

        #region Private Fields

        private IntPtr _deviceHandle = IntPtr.Zero;
        private readonly object _lock = new object();
        private readonly SemaphoreSlim _operationLock = new SemaphoreSlim(1, 1);
        private DeviceState _state = DeviceState.Closed;
        private uint _deviceNumber;
        private SmbusConfig _config;
        private bool _disposed = false;

        #endregion

        #region Events

        /// <summary>Event raised when device state changes</summary>
        public event EventHandler<DeviceStateEventArgs> StateChanged;

        /// <summary>Event raised when data is received</summary>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>Event raised when data is sent</summary>
        public event EventHandler<DataSentEventArgs> DataSent;

        /// <summary>Event raised when an error occurs</summary>
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        /// <summary>Event raised when transfer status changes</summary>
        public event EventHandler<TransferStatusEventArgs> TransferStatusChanged;

        #endregion

        #region Properties

        /// <summary>Current device state</summary>
        public DeviceState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    var previousState = _state;
                    _state = value;
                    OnStateChanged(new DeviceStateEventArgs(previousState, _state));
                }
            }
        }

        /// <summary>Device number</summary>
        public uint DeviceNumber => _deviceNumber;

        /// <summary>Vendor ID</summary>
        public ushort VendorId { get; private set; }

        /// <summary>Product ID</summary>
        public ushort ProductId { get; private set; }

        /// <summary>Serial number</summary>
        public string SerialNumber { get; private set; }

        /// <summary>Is device open</summary>
        public bool IsOpen => _state != DeviceState.Closed && _state != DeviceState.Closing;

        /// <summary>Is device ready for operations</summary>
        public bool IsReady => State == DeviceState.Configured || State == DeviceState.Open;

        /// <summary>Current configuration</summary>
        public SmbusConfig Configuration => _config;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new CP2112 device instance
        /// </summary>
        public Cp2112Device()
        {
            VendorId = DEFAULT_VID;
            ProductId = DEFAULT_PID;
            _config = SmbusConfig.Default;
        }

        /// <summary>
        /// Create a new CP2112 device instance with specific VID/PID
        /// </summary>
        public Cp2112Device(ushort vid, ushort pid)
        {
            VendorId = vid;
            ProductId = pid;
            _config = SmbusConfig.Default;
        }

        #endregion

        #region Public Methods - Device Management

        /// <summary>
        /// Open the device
        /// </summary>
        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                OpenInternal();
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Close the device
        /// </summary>
        public async Task CloseAsync()
        {
            await _operationLock.WaitAsync().ConfigureAwait(false);
            try
            {
                CloseInternal();
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Configure the device
        /// </summary>
        public async Task ConfigureAsync(SmbusConfig config, CancellationToken cancellationToken = default)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ConfigureInternal(config);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Reset the device
        /// </summary>
        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ResetInternal();
            }
            finally
            {
                _operationLock.Release();
            }
        }

        #endregion

        #region Public Methods - Data Transfer

        /// <summary>
        /// Read data from SMBus device asynchronously
        /// </summary>
        public async Task<byte[]> ReadAsync(
            byte slaveAddress,
            ushort numBytesToRead,
            byte targetAddressSize,
            byte[] targetAddress,
            CancellationToken cancellationToken = default)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return ReadInternal(slaveAddress, numBytesToRead, targetAddressSize, targetAddress);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Read data from SMBus device with 1-byte address
        /// </summary>
        public async Task<byte[]> ReadAsync(
            byte slaveAddress,
            ushort numBytesToRead,
            byte targetAddress,
            CancellationToken cancellationToken = default)
        {
            return await ReadAsync(slaveAddress, numBytesToRead, 1, new[] { targetAddress }, cancellationToken);
        }

        /// <summary>
        /// Read data from SMBus slave device without target address
        /// </summary>
        public async Task<byte[]> ReadSlaveAsync(
            byte slaveAddress,
            ushort numBytesToRead,
            CancellationToken cancellationToken = default)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return ReadSlaveInternal(slaveAddress, numBytesToRead);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Write data to SMBus device asynchronously
        /// </summary>
        public async Task WriteAsync(
            byte slaveAddress,
            byte[] buffer,
            CancellationToken cancellationToken = default)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                WriteInternal(slaveAddress, buffer);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Write register address and data to SMBus device
        /// </summary>
        public async Task WriteRegisterAsync(
            byte slaveAddress,
            byte registerAddress,
            byte[] data,
            CancellationToken cancellationToken = default)
        {
            var buffer = new byte[data.Length + 1];
            buffer[0] = registerAddress;
            Array.Copy(data, 0, buffer, 1, data.Length);
            await WriteAsync(slaveAddress, buffer, cancellationToken);
        }

        /// <summary>
        /// Read 16-bit value from register
        /// </summary>
        public async Task<ushort> ReadUInt16Async(
            byte slaveAddress,
            byte registerAddress,
            CancellationToken cancellationToken = default)
        {
            var data = await ReadAsync(slaveAddress, 2, registerAddress, cancellationToken);
            return (ushort)(data[0] | (data[1] << 8));
        }

        /// <summary>
        /// Read 16-bit signed value from register
        /// </summary>
        public async Task<short> ReadInt16Async(
            byte slaveAddress,
            byte registerAddress,
            CancellationToken cancellationToken = default)
        {
            var raw = await ReadUInt16Async(slaveAddress, registerAddress, cancellationToken);
            return unchecked((short)raw);
        }

        /// <summary>
        /// Write 16-bit value to register
        /// </summary>
        public async Task WriteUInt16Async(
            byte slaveAddress,
            byte registerAddress,
            ushort value,
            CancellationToken cancellationToken = default)
        {
            var buffer = new byte[] { registerAddress, (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF) };
            await WriteAsync(slaveAddress, buffer, cancellationToken);
        }

        #endregion

        #region Public Methods - Device Information

        /// <summary>
        /// Get device information
        /// </summary>
        public DeviceInfo GetDeviceInfo()
        {
            lock (_lock)
            {
                return new DeviceInfo
                {
                    DeviceNumber = _deviceNumber,
                    VendorId = VendorId,
                    ProductId = ProductId,
                    SerialNumber = SerialNumber
                };
            }
        }

        /// <summary>
        /// Get SMBus configuration
        /// </summary>
        public async Task<SmbusConfig> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var status = NativeMethods.HidSmbus_GetSmbusConfig(
                    _deviceHandle,
                    out uint bitRate,
                    out byte address,
                    out bool autoReadRespond,
                    out ushort writeTimeout,
                    out ushort readTimeout,
                    out bool sclLowTimeout,
                    out ushort transferRetries);

                CheckStatus(status);

                return new SmbusConfig
                {
                    BitRate = bitRate,
                    AckAddress = address,
                    AutoReadRespond = autoReadRespond,
                    WriteTimeout = writeTimeout,
                    ReadTimeout = readTimeout,
                    SclLowTimeout = sclLowTimeout,
                    TransferRetries = transferRetries
                };
            }
            finally
            {
                _operationLock.Release();
            }
        }

        #endregion

        #region Private Methods - Implementation

        private void OpenInternal()
        {
            if (IsOpen)
            {
                throw new InvalidOperationException("Device is already open");
            }

            State = DeviceState.Opening;

            try
            {
                // Find device
                var status = NativeMethods.HidSmbus_GetNumDevices(out uint numDevices, VendorId, ProductId);
                CheckStatus(status);

                if (numDevices == 0)
                {
                    throw new DeviceNotFoundException();
                }

                // Get first available device
                bool deviceFound = false;
                for (uint i = 0; i < numDevices; i++)
                {
                    var sb = new StringBuilder(260);
                    status = NativeMethods.HidSmbus_GetString(i, VendorId, ProductId, sb, NativeMethods.HID_SMBUS_GET_SERIAL_STR);
                    if (status == NativeMethods.HID_SMBUS_SUCCESS)
                    {
                        _deviceNumber = i;
                        SerialNumber = sb.ToString();
                        deviceFound = true;
                        break;
                    }
                }

                if (!deviceFound)
                {
                    throw new DeviceNotFoundException("No accessible device found");
                }

                // Open device
                status = NativeMethods.HidSmbus_Open(out _deviceHandle, _deviceNumber, VendorId, ProductId);
                CheckStatus(status);

                // Get additional info
                if (string.IsNullOrEmpty(SerialNumber))
                {
                    var sb = new StringBuilder(260);
                    NativeMethods.HidSmbus_GetOpenedString(_deviceHandle, sb, NativeMethods.HID_SMBUS_GET_SERIAL_STR);
                    SerialNumber = sb.ToString();
                }

                State = DeviceState.Open;
            }
            catch (Exception ex)
            {
                State = DeviceState.Error;
                OnError(new ErrorEventArgs($"Failed to open device: {ex.Message}", 0, ex, true));
                throw;
            }
        }

        private void CloseInternal()
        {
            if (!IsOpen)
            {
                return;
            }

            State = DeviceState.Closing;

            try
            {
                if (_deviceHandle != IntPtr.Zero)
                {
                    var status = NativeMethods.HidSmbus_Close(_deviceHandle);
                    _deviceHandle = IntPtr.Zero;

                    if (status != NativeMethods.HID_SMBUS_SUCCESS)
                    {
                        OnError(new ErrorEventArgs($"Warning: Close returned status 0x{status:X2}", status));
                    }
                }

                State = DeviceState.Closed;
            }
            catch (Exception ex)
            {
                State = DeviceState.Error;
                OnError(new ErrorEventArgs($"Error closing device: {ex.Message}", 0, ex));
            }
        }

        private void ConfigureInternal(SmbusConfig config)
        {
            EnsureReady();

            try
            {
                var status = NativeMethods.HidSmbus_SetSmbusConfig(
                    _deviceHandle,
                    config.BitRate,
                    config.AckAddress,
                    config.AutoReadRespond,
                    config.WriteTimeout,
                    config.ReadTimeout,
                    config.SclLowTimeout,
                    config.TransferRetries);

                CheckStatus(status);

                status = NativeMethods.HidSmbus_SetTimeouts(_deviceHandle, config.ResponseTimeout);
                CheckStatus(status);

                _config = config;
                State = DeviceState.Configured;
            }
            catch (Exception ex)
            {
                State = DeviceState.Error;
                OnError(new ErrorEventArgs($"Configuration failed: {ex.Message}", 0, ex));
                throw;
            }
        }

        private void ResetInternal()
        {
            EnsureOpen();

            var status = NativeMethods.HidSmbus_Reset(_deviceHandle);
            CheckStatus(status);
        }


        private byte[] ReadInternal(byte slaveAddress, ushort numBytesToRead, byte targetAddressSize, byte[] targetAddress)
        {
            EnsureReady();

            if (numBytesToRead > NativeMethods.HID_SMBUS_MAX_READ_RESPONSE_SIZE)
            {
                throw new InvalidParameterException(nameof(numBytesToRead));
            }

            if (targetAddressSize > NativeMethods.HID_SMBUS_MAX_TARGET_ADDRESS_SIZE)
            {
                throw new InvalidParameterException(nameof(targetAddressSize));
            }

            byte[] buffer = new byte[NativeMethods.HID_SMBUS_MAX_READ_RESPONSE_SIZE];
            int attempt = 0;
            Exception lastException = null;

            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    // Issue read request
                    var status = NativeMethods.HidSmbus_AddressReadRequest(
                        _deviceHandle,
                        slaveAddress,
                        numBytesToRead,
                        targetAddressSize,
                        targetAddress);

                    CheckStatus(status);

                    // Force read response
                    status = NativeMethods.HidSmbus_ForceReadResponse(_deviceHandle, numBytesToRead);
                    CheckStatus(status);

                    // Wait for response
                    int totalBytesRead = 0;
                    while (totalBytesRead < numBytesToRead)
                    {
                        status = NativeMethods.HidSmbus_GetReadResponse(
                            _deviceHandle,
                            out byte responseStatus,
                            buffer,
                            NativeMethods.HID_SMBUS_MAX_READ_RESPONSE_SIZE,
                            out byte numBytesRead);

                        CheckStatus(status);

                        if (responseStatus == NativeMethods.HID_SMBUS_S0_ERROR)
                        {
                            throw new ReadException("Read transfer failed with error status");
                        }

                        totalBytesRead += numBytesRead;
                    }

                    // Trim and return data
                    byte[] result = new byte[numBytesToRead];
                    Array.Copy(buffer, 0, result, 0, numBytesToRead);

                    OnDataReceived(new DataReceivedEventArgs(slaveAddress, result, numBytesToRead));
                    return result;
                }
                catch (Exception ex) when (attempt < MAX_RETRY_ATTEMPTS - 1)
                {
                    lastException = ex;
                    attempt++;
                    Thread.Sleep(RETRY_DELAY_MS);
                }
            }

            throw new ReadException($"Read failed after {MAX_RETRY_ATTEMPTS} attempts", lastException);
        }

        private byte[] ReadSlaveInternal(byte slaveAddress, ushort numBytesToRead)
        {
            EnsureReady();

            if (numBytesToRead > NativeMethods.HID_SMBUS_MAX_READ_RESPONSE_SIZE)
            {
                throw new InvalidParameterException(nameof(numBytesToRead));
            }

            byte[] buffer = new byte[NativeMethods.HID_SMBUS_MAX_READ_RESPONSE_SIZE];
            int attempt = 0;
            Exception lastException = null;

            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    // Issue read request
                    var status = NativeMethods.HidSmbus_ReadRequest(
                        _deviceHandle,
                        slaveAddress,
                        numBytesToRead);

                    CheckStatus(status);

                    // Force read response
                    status = NativeMethods.HidSmbus_ForceReadResponse(_deviceHandle, numBytesToRead);
                    CheckStatus(status);

                    // Wait for response
                    int totalBytesRead = 0;
                    while (totalBytesRead < numBytesToRead)
                    {
                        status = NativeMethods.HidSmbus_GetReadResponse(
                            _deviceHandle,
                            out byte responseStatus,
                            buffer,
                            NativeMethods.HID_SMBUS_MAX_READ_RESPONSE_SIZE,
                            out byte numBytesRead);

                        CheckStatus(status);

                        if (responseStatus == NativeMethods.HID_SMBUS_S0_ERROR)
                        {
                            throw new ReadException("Read transfer failed with error status");
                        }

                        totalBytesRead += numBytesRead;
                    }

                    // Trim and return data
                    byte[] result = new byte[numBytesToRead];
                    Array.Copy(buffer, 0, result, 0, numBytesToRead);

                    OnDataReceived(new DataReceivedEventArgs(slaveAddress, result, numBytesToRead));
                    return result;
                }
                catch (Exception ex) when (attempt < MAX_RETRY_ATTEMPTS - 1)
                {
                    lastException = ex;
                    attempt++;
                    Thread.Sleep(RETRY_DELAY_MS);
                }
            }

            throw new ReadException($"Read failed after {MAX_RETRY_ATTEMPTS} attempts", lastException);
        }

        private void WriteInternal(byte slaveAddress, byte[] buffer)
        {
            EnsureReady();

            if (buffer == null || buffer.Length == 0)
            {
                throw new InvalidParameterException(nameof(buffer));
            }

            if (buffer.Length > NativeMethods.HID_SMBUS_MAX_WRITE_REQUEST_SIZE)
            {
                throw new InvalidParameterException("Buffer too large");
            }

            int attempt = 0;
            Exception lastException = null;

            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    // Issue write request
                    var status = NativeMethods.HidSmbus_WriteRequest(
                        _deviceHandle,
                        slaveAddress,
                        buffer,
                        (byte)buffer.Length);

                    CheckStatus(status);

                    // Wait for completion
                    byte status0;
                    byte status1;
                    do
                    {
                        status = NativeMethods.HidSmbus_TransferStatusRequest(_deviceHandle);
                        CheckStatus(status);

                        status = NativeMethods.HidSmbus_GetTransferStatusResponse(
                            _deviceHandle,
                            out status0,
                            out status1,
                            out ushort numRetries,
                            out ushort bytesRead);

                        CheckStatus(status);

                        OnTransferStatusChanged(new TransferStatusEventArgs(new TransferStatus
                        {
                            Status0 = status0,
                            Status1 = status1,
                            NumRetries = numRetries,
                            BytesRead = bytesRead
                        }));

                    } while (status0 != NativeMethods.HID_SMBUS_S0_COMPLETE &&
                             status0 != NativeMethods.HID_SMBUS_S0_ERROR);

                    if (status0 == NativeMethods.HID_SMBUS_S0_ERROR)
                    {
                        throw new WriteException($"Write failed with S1 status: 0x{status1:X2}");
                    }

                    OnDataSent(new DataSentEventArgs(slaveAddress, buffer, buffer.Length));
                    return;
                }
                catch (Exception ex) when (attempt < MAX_RETRY_ATTEMPTS - 1)
                {
                    lastException = ex;
                    attempt++;
                    Thread.Sleep(RETRY_DELAY_MS);
                }
            }

            throw new WriteException($"Write failed after {MAX_RETRY_ATTEMPTS} attempts", lastException);
        }

        #endregion

        #region Helper Methods

        private void EnsureOpen()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Device is not open");
            }
        }

        private void EnsureReady()
        {
            if (!IsReady)
            {
                throw new InvalidOperationException("Device is not ready. Open and configure the device first.");
            }
        }

        private void CheckStatus(int status)
        {
            if (status == NativeMethods.HID_SMBUS_SUCCESS)
            {
                return;
            }

            switch (status)
            {
                case NativeMethods.HID_SMBUS_DEVICE_NOT_FOUND:
                    throw new DeviceNotFoundException();
                case NativeMethods.HID_SMBUS_INVALID_HANDLE:
                    throw new InvalidHandleException();
                case NativeMethods.HID_SMBUS_INVALID_PARAMETER:
                    throw new InvalidParameterException("Unknown");
                case NativeMethods.HID_SMBUS_READ_ERROR:
                    throw new ReadException("Read error");
                case NativeMethods.HID_SMBUS_READ_TIMED_OUT:
                    throw new ReadTimeoutException();
                case NativeMethods.HID_SMBUS_WRITE_ERROR:
                    throw new WriteException("Write error");
                case NativeMethods.HID_SMBUS_WRITE_TIMED_OUT:
                    throw new WriteTimeoutException();
                case NativeMethods.HID_SMBUS_DEVICE_IO_FAILED:
                    throw new DeviceIOException("I/O operation failed");
                case NativeMethods.HID_SMBUS_DEVICE_ACCESS_ERROR:
                    throw new DeviceAccessException("Device access error");
                default:
                    throw new Cp2112Exception(status, $"Unknown error: 0x{status:X2}");
            }
        }

        #endregion

        #region Event Invokers

        protected virtual void OnStateChanged(DeviceStateEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        protected virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        protected virtual void OnDataSent(DataSentEventArgs e)
        {
            DataSent?.Invoke(this, e);
        }

        protected virtual void OnError(ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual void OnTransferStatusChanged(TransferStatusEventArgs e)
        {
            TransferStatusChanged?.Invoke(this, e);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose the device and release resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Close device synchronously
                    try
                    {
                        if (IsOpen)
                        {
                            CloseInternal();
                        }
                    }
                    catch
                    {
                        // Ignore exceptions during dispose
                    }
                }

                _operationLock?.Dispose();
                _disposed = true;
            }
        }

        ~Cp2112Device()
        {
            Dispose(false);
        }

        #endregion
    }
}
