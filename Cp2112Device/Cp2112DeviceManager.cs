using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cp2112Sdk.Models;
using Cp2112Sdk.Native;
using Cp2112Sdk.Exceptions;

namespace Cp2112Sdk
{
    /// <summary>
    /// Device manager for handling multiple CP2112 devices
    /// </summary>
    public class Cp2112DeviceManager : IDisposable
    {
        #region Private Fields

        private readonly Dictionary<uint, Cp2112Device> _devices = new Dictionary<uint, Cp2112Device>();
        private readonly object _lock = new object();
        private readonly ushort _vendorId;
        private readonly ushort _productId;
        private bool _disposed = false;

        #endregion

        #region Events

        /// <summary>Event raised when a device is added</summary>
        public event EventHandler<DeviceEventArgs> DeviceAdded;

        /// <summary>Event raised when a device is removed</summary>
        public event EventHandler<DeviceEventArgs> DeviceRemoved;

        /// <summary>Event raised when device list changes</summary>
        public event EventHandler DeviceListChanged;

        #endregion

        #region Properties

        /// <summary>Number of managed devices</summary>
        public int DeviceCount
        {
            get
            {
                lock (_lock)
                {
                    return _devices.Count;
                }
            }
        }

        /// <summary>Get device by number</summary>
        public Cp2112Device this[uint deviceNumber]
        {
            get
            {
                lock (_lock)
                {
                    return _devices.TryGetValue(deviceNumber, out var device) ? device : null;
                }
            }
        }

        /// <summary>Get all managed devices</summary>
        public IEnumerable<Cp2112Device> Devices
        {
            get
            {
                lock (_lock)
                {
                    return _devices.Values.ToList();
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Create device manager with default VID/PID
        /// </summary>
        public Cp2112DeviceManager() : this(0x10C4, 0xEA90)
        {
        }

        /// <summary>
        /// Create device manager with specific VID/PID
        /// </summary>
        public Cp2112DeviceManager(ushort vendorId, ushort productId)
        {
            _vendorId = vendorId;
            _productId = productId;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Scan for available devices
        /// </summary>
        public async Task<List<DeviceInfo>> ScanDevicesAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var status = NativeMethods.HidSmbus_GetNumDevices(out uint numDevices, _vendorId, _productId);
                if (status != NativeMethods.HID_SMBUS_SUCCESS)
                {
                    throw new Cp2112Exception(status, "Failed to get number of devices");
                }

                var devices = new List<DeviceInfo>();

                for (uint i = 0; i < numDevices; i++)
                {
                    try
                    {
                        var deviceInfo = new DeviceInfo
                        {
                            DeviceNumber = i,
                            VendorId = _vendorId,
                            ProductId = _productId
                        };

                        // Get serial number
                        var sb = new StringBuilder(260);
                        NativeMethods.HidSmbus_GetString(i, _vendorId, _productId, sb,
                            NativeMethods.HID_SMBUS_GET_SERIAL_STR);
                        deviceInfo.SerialNumber = sb.ToString();

                        // Get manufacturer
                        sb = new StringBuilder(260);
                        NativeMethods.HidSmbus_GetString(i, _vendorId, _productId, sb,
                            NativeMethods.HID_SMBUS_GET_MANUFACTURER_STR);
                        deviceInfo.Manufacturer = sb.ToString();

                        // Get product name
                        sb = new StringBuilder(260);
                        NativeMethods.HidSmbus_GetString(i, _vendorId, _productId, sb,
                            NativeMethods.HID_SMBUS_GET_PRODUCT_STR);
                        deviceInfo.Product = sb.ToString();

                        devices.Add(deviceInfo);
                    }
                    catch
                    {
                        // Skip devices that can't be queried
                    }
                }

                return devices;
            }, cancellationToken);
        }

        /// <summary>
        /// Open and manage a specific device
        /// </summary>
        public async Task<Cp2112Device> OpenDeviceAsync(uint deviceNumber, SmbusConfig config = null,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_devices.ContainsKey(deviceNumber))
                {
                    return _devices[deviceNumber];
                }
            }

            var device = new Cp2112Device(_vendorId, _productId);
            await device.OpenAsync(cancellationToken);

            if (config != null)
            {
                await device.ConfigureAsync(config, cancellationToken);
            }

            lock (_lock)
            {
                // Check again in case another thread added it
                if (_devices.ContainsKey(deviceNumber))
                {
                    // This device was already added, close the new one
                    device.Dispose();
                    return _devices[deviceNumber];
                }

                _devices[deviceNumber] = device;
                device.StateChanged += OnDeviceStateChanged;
                device.ErrorOccurred += OnDeviceError;
            }

            OnDeviceAdded(new DeviceEventArgs(device, "Device opened"));
            OnDeviceListChanged();

            return device;
        }

        /// <summary>
        /// Open all available devices
        /// </summary>
        public async Task<List<Cp2112Device>> OpenAllDevicesAsync(SmbusConfig config = null,
            CancellationToken cancellationToken = default)
        {
            var deviceInfos = await ScanDevicesAsync(cancellationToken);
            var devices = new List<Cp2112Device>();

            foreach (var deviceInfo in deviceInfos)
            {
                try
                {
                    var device = await OpenDeviceAsync(deviceInfo.DeviceNumber, config, cancellationToken);
                    devices.Add(device);
                }
                catch (Exception ex)
                {
                    // Log but continue opening other devices
                    System.Diagnostics.Debug.WriteLine($"Failed to open device {deviceInfo.DeviceNumber}: {ex.Message}");
                }
            }

            return devices;
        }

        /// <summary>
        /// Close and remove a specific device
        /// </summary>
        public async Task CloseDeviceAsync(uint deviceNumber)
        {
            Cp2112Device device = null;

            lock (_lock)
            {
                if (_devices.TryGetValue(deviceNumber, out device))
                {
                    _devices.Remove(deviceNumber);
                    device.StateChanged -= OnDeviceStateChanged;
                    device.ErrorOccurred -= OnDeviceError;
                }
            }

            if (device != null)
            {
                await device.CloseAsync();
                device.Dispose();
                OnDeviceRemoved(new DeviceEventArgs(device, "Device closed"));
                OnDeviceListChanged();
            }
        }

        /// <summary>
        /// Close all devices
        /// </summary>
        public async Task CloseAllDevicesAsync()
        {
            List<Cp2112Device> devicesToClose;

            lock (_lock)
            {
                devicesToClose = _devices.Values.ToList();
                _devices.Clear();
            }

            foreach (var device in devicesToClose)
            {
                device.StateChanged -= OnDeviceStateChanged;
                device.ErrorOccurred -= OnDeviceError;
            }

            foreach (var device in devicesToClose)
            {
                try
                {
                    await device.CloseAsync();
                    device.Dispose();
                }
                catch
                {
                    // Ignore errors during close
                }
            }

            OnDeviceListChanged();
        }

        /// <summary>
        /// Get device by serial number
        /// </summary>
        public Cp2112Device GetDeviceBySerial(string serialNumber)
        {
            lock (_lock)
            {
                return _devices.Values.FirstOrDefault(d => d.SerialNumber == serialNumber);
            }
        }

        /// <summary>
        /// Check if a device is managed
        /// </summary>
        public bool IsDeviceManaged(uint deviceNumber)
        {
            lock (_lock)
            {
                return _devices.ContainsKey(deviceNumber);
            }
        }

        /// <summary>
        /// Get all device serial numbers
        /// </summary>
        public List<string> GetDeviceSerialNumbers()
        {
            lock (_lock)
            {
                return _devices.Values.Select(d => d.SerialNumber).ToList();
            }
        }

        #endregion

        #region Event Handlers

        private void OnDeviceStateChanged(object sender, Events.DeviceStateEventArgs e)
        {
            if (e.CurrentState == Models.DeviceState.Closed)
            {
                // Auto-remove closed devices
                if (sender is Cp2112Device device)
                {
                    Task.Run(async () =>
                    {
                        await CloseDeviceAsync(device.DeviceNumber);
                    });
                }
            }
        }

        private void OnDeviceError(object sender, Events.ErrorEventArgs e)
        {
            // Handle device errors - can be extended for logging, etc.
            if (e.IsFatal)
            {
                // Fatal error - remove device
                if (sender is Cp2112Device device)
                {
                    Task.Run(async () =>
                    {
                        await CloseDeviceAsync(device.DeviceNumber);
                    });
                }
            }
        }

        #endregion

        #region Event Invokers

        protected virtual void OnDeviceAdded(DeviceEventArgs e)
        {
            DeviceAdded?.Invoke(this, e);
        }

        protected virtual void OnDeviceRemoved(DeviceEventArgs e)
        {
            DeviceRemoved?.Invoke(this, e);
        }

        protected virtual void OnDeviceListChanged()
        {
            DeviceListChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region IDisposable

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
                    // Close all devices synchronously
                    try
                    {
                        CloseAllDevicesAsync().Wait();
                    }
                    catch
                    {
                        // Ignore exceptions during dispose
                    }
                }

                _disposed = true;
            }
        }

        ~Cp2112DeviceManager()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for device manager events
    /// </summary>
    public class DeviceEventArgs : EventArgs
    {
        public Cp2112Device Device { get; }
        public string Message { get; }

        public DeviceEventArgs(Cp2112Device device, string message = null)
        {
            Device = device;
            Message = message ?? $"Device {device.DeviceNumber}";
        }

        public override string ToString()
        {
            return $"{Message}: {Device.State}";
        }
    }
}
