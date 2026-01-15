using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Cp2112Sdk;
using Cp2112Sdk.Models;
using Cp2112Sdk.Events;
using Cp2112Sdk.Exceptions;

namespace Examples
{
    /// <summary>
    /// Usage examples for CP2112 device in WPF applications
    /// </summary>
    public class ExampleUsage
    {
        #region Example 1: Basic Device Operation

        /// <summary>
        /// Example 1: Basic open, configure, read, write, and close
        /// </summary>
        public static async Task BasicDeviceOperationExample()
        {
            Console.WriteLine("=== Example 1: Basic Device Operation ===");

            using (var device = new Cp2112Device())
            {
                try
                {
                    // Subscribe to events
                    device.StateChanged += (s, e) =>
                    {
                        Console.WriteLine($"State changed: {e.PreviousState} -> {e.CurrentState}");
                    };

                    device.ErrorOccurred += (s, e) =>
                    {
                        Console.WriteLine($"Error: {e.ErrorMessage}");
                    };

                    // Open device
                    await device.OpenAsync();
                    Console.WriteLine($"Device opened: {device.SerialNumber}");

                    // Configure device
                    var config = SmbusConfig.Default;
                    await device.ConfigureAsync(config);
                    Console.WriteLine("Device configured");

                    // Read from register (example: 0x8D)
                    var data = await device.ReadAsync(0xC8, 2, 0x8D);
                    Console.WriteLine($"Read {data.Length} bytes from register 0x8D");

                    // Write to register
                    var writeData = new byte[] { 0xEA, 0x00, 0x00 };
                    await device.WriteAsync(0xC8, writeData);
                    Console.WriteLine($"Wrote {writeData.Length} bytes");

                    // Read 16-bit value
                    ushort value = await device.ReadUInt16Async(0xC8, 0x8D);
                    Console.WriteLine($"Read 16-bit value: {value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Example 2: Multi-Device Management

        /// <summary>
        /// Example 2: Managing multiple devices
        /// </summary>
        public static async Task MultiDeviceManagementExample()
        {
            Console.WriteLine("\n=== Example 2: Multi-Device Management ===");

            using (var manager = new Cp2112DeviceManager())
            {
                // Subscribe to manager events
                manager.DeviceAdded += (s, e) =>
                {
                    Console.WriteLine($"Device added: {e.Device.SerialNumber}");
                };

                manager.DeviceRemoved += (s, e) =>
                {
                    Console.WriteLine($"Device removed: {e.Device.SerialNumber}");
                };

                // Scan for devices
                var devices = await manager.ScanDevicesAsync();
                Console.WriteLine($"Found {devices.Count} devices");

                // Open all devices
                var openedDevices = await manager.OpenAllDevicesAsync(SmbusConfig.Default);
                Console.WriteLine($"Opened {openedDevices.Count} devices");

                // Get device by serial number
                foreach (var deviceInfo in devices)
                {
                    var device = manager.GetDeviceBySerial(deviceInfo.SerialNumber);
                    if (device != null)
                    {
                        Console.WriteLine($"Device {deviceInfo.SerialNumber} is ready");

                        // Perform operations on each device
                        try
                        {
                            var data = await device.ReadAsync(0xC8, 2, 0x8D);
                            Console.WriteLine($"  Read {data.Length} bytes from device");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  Error: {ex.Message}");
                        }
                    }
                }

                // Close all devices
                await manager.CloseAllDevicesAsync();
                Console.WriteLine("All devices closed");
            }
        }

        #endregion

        #region Example 3: Data Monitoring with Events

        /// <summary>
        /// Example 3: Continuous monitoring with events
        /// </summary>
        public static async Task DataMonitoringExample()
        {
            Console.WriteLine("\n=== Example 3: Data Monitoring ===");

            using (var device = new Cp2112Device())
            {
                // Subscribe to events for monitoring
                device.DataReceived += (s, e) =>
                {
                    Console.WriteLine($"Data received: {e.BytesRead} bytes from 0x{e.SlaveAddress:X2}");
                };

                device.DataSent += (s, e) =>
                {
                    Console.WriteLine($"Data sent: {e.BytesSent} bytes to 0x{e.SlaveAddress:X2}");
                };

                device.TransferStatusChanged += (s, e) =>
                {
                    Console.WriteLine($"Transfer: {e.Status}");
                };

                try
                {
                    await device.OpenAsync();
                    await device.ConfigureAsync(SmbusConfig.Default);

                    // Monitor loop
                    var cts = new CancellationTokenSource();
                    var monitorTask = MonitorDeviceData(device, cts.Token);

                    Console.WriteLine("Monitoring... Press Enter to stop");
                    Console.ReadLine();

                    cts.Cancel();
                    await monitorTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static async Task MonitorDeviceData(Cp2112Device device, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read various registers (example from CP2112 demo)
                    var temp1 = await device.ReadInt16Async(0xC8, 0x8D, token);
                    float temp1C = temp1 / 32.0f - 40.0f;

                    var voltage = await device.ReadInt16Async(0xC8, 0x88, token);
                    float voltageV = voltage / 32.0f;

                    Console.WriteLine($"Temp: {temp1C:F2}°C, Voltage: {voltageV:F2}V");

                    await Task.Delay(500, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Monitor error: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }

        #endregion

        #region Example 4: WPF ViewModel Integration

        /// <summary>
        /// Example 4: WPF ViewModel pattern for device control
        /// </summary>
        public class DeviceViewModel : System.ComponentModel.INotifyPropertyChanged
        {
            private readonly Cp2112Device _device;
            private string _status;
            private bool _isConnected;
            private float _temperature;
            private float _voltage;

            public DeviceViewModel()
            {
                _device = new Cp2112Device();
                _device.StateChanged += OnDeviceStateChanged;
                _device.ErrorOccurred += OnDeviceError;
            }

            public string Status
            {
                get => _status;
                set
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }

            public bool IsConnected
            {
                get => _isConnected;
                set
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }

            public float Temperature
            {
                get => _temperature;
                set
                {
                    _temperature = value;
                    OnPropertyChanged();
                }
            }

            public float Voltage
            {
                get => _voltage;
                set
                {
                    _voltage = value;
                    OnPropertyChanged();
                }
            }

            public async Task ConnectAsync()
            {
                try
                {
                    Status = "Connecting...";
                    await _device.OpenAsync();
                    await _device.ConfigureAsync(SmbusConfig.Default);
                    IsConnected = true;
                    Status = $"Connected: {_device.SerialNumber}";
                }
                catch (Exception ex)
                {
                    Status = $"Error: {ex.Message}";
                }
            }

            public async Task DisconnectAsync()
            {
                try
                {
                    await _device.CloseAsync();
                    IsConnected = false;
                    Status = "Disconnected";
                }
                catch (Exception ex)
                {
                    Status = $"Error: {ex.Message}";
                }
            }

            public async Task ReadDataAsync()
            {
                if (!IsConnected) return;

                try
                {
                    // Read temperature (register 0x8D)
                    var tempRaw = await _device.ReadInt16Async(0xC8, 0x8D);
                    Temperature = tempRaw / 32.0f - 40.0f;

                    // Read voltage (register 0x88)
                    var voltRaw = await _device.ReadInt16Async(0xC8, 0x88);
                    Voltage = voltRaw / 32.0f;

                    Status = $"Updated: T={Temperature:F2}°C, V={Voltage:F2}V";
                }
                catch (Exception ex)
                {
                    Status = $"Error: {ex.Message}";
                }
            }

            private void OnDeviceStateChanged(object sender, DeviceStateEventArgs e)
            {
                Status = $"State: {e.CurrentState}";
            }

            private void OnDeviceError(object sender, ErrorEventArgs e)
            {
                Status = $"Error: {e.ErrorMessage}";
            }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }

            public async Task DisposeAsync()
            {
                await _device.CloseAsync();
                _device.Dispose();
            }
        }

        #endregion

        #region Example 5: Error Handling and Retry

        /// <summary>
        /// Example 5: Robust error handling
        /// </summary>
        public static async Task RobustErrorHandlingExample()
        {
            Console.WriteLine("\n=== Example 5: Robust Error Handling ===");

            using (var device = new Cp2112Device())
            {
                device.ErrorOccurred += (s, e) =>
                {
                    if (e.IsFatal)
                    {
                        Console.WriteLine($"FATAL ERROR: {e.ErrorMessage}");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: {e.ErrorMessage}");
                    }
                };

                try
                {
                    await device.OpenAsync();
                    await device.ConfigureAsync(SmbusConfig.LowSpeed); // Use slower, more reliable config

                    // Perform operation with timeout
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try
                    {
                        var data = await device.ReadAsync(0xC8, 2, 0x8D, cts.Token);
                        Console.WriteLine($"Read successful: {data.Length} bytes");
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Operation timed out");
                    }
                }
                catch (DeviceNotFoundException)
                {
                    Console.WriteLine("Device not found - check connections");
                }
                catch (Cp2112Exception ex)
                {
                    Console.WriteLine($"Device error (0x{ex.ErrorCode:X2}): {ex.Message}");
                }
            }
        }

        #endregion

        #region Example 6: Writing to CSV File

        /// <summary>
        /// Example 6: Data logging to CSV (similar to C demo)
        /// </summary>
        public static async Task DataLoggingExample()
        {
            Console.WriteLine("\n=== Example 6: Data Logging ===");

            using (var device = new Cp2112Device())
            using (var writer = new System.IO.StreamWriter("outputLogging.csv"))
            {
                try
                {
                    await device.OpenAsync();
                    await device.ConfigureAsync(SmbusConfig.Default);

                    // Write CSV header
                    await writer.WriteLineAsync("HV_V,LV_V,I1_A,I2_A,Temp1_C,Temp2_C,I1_CNT,DUT_Status,Timestamp");

                    var cts = new CancellationTokenSource();
                    Console.WriteLine("Logging... Press Enter to stop");

                    // Start logging task
                    var logTask = Task.Run(async () =>
                    {
                        int count = 0;
                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                // Read all registers (from original C demo)
                                var temp1 = await device.ReadInt16Async(0xC8, 0x8D, cts.Token);
                                float temp1C = temp1 / 32.0f - 40.0f;

                                var temp2 = await device.ReadInt16Async(0xC8, 0x8E, cts.Token);
                                float temp2C = temp2 / 32.0f - 40.0f;

                                var hv = await device.ReadInt16Async(0xC8, 0x88, cts.Token);
                                float hvV = hv / 32.0f;

                                var lv = await device.ReadInt16Async(0xC8, 0x8B, cts.Token);
                                float lvV = lv / 32.0f;

                                var i2 = await device.ReadInt16Async(0xC8, 0x8C, cts.Token);
                                float i2A = i2 / 32.0f;

                                var i1 = await device.ReadInt16Async(0xC8, 0x90, cts.Token);
                                float i1A = i1 / 32.0f;

                                var i1Cnt = await device.ReadUInt16Async(0xC8, 0xCD, cts.Token);

                                var status = await device.ReadUInt16Async(0xC8, 0x79, cts.Token);

                                // Write to CSV
                                var line = $"{hvV:F2},{lvV:F2},{i1A:F2},{i2A:F2},{temp1C:F2},{temp2C:F2},{i1Cnt},0x{status:X},{DateTime.Now:O}";
                                await writer.WriteLineAsync(line);
                                await writer.FlushAsync();

                                count++;
                                Console.WriteLine($"[{count}] {line}");

                                await Task.Delay(500, cts.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error: {ex.Message}");
                                await Task.Delay(1000, cts.Token);
                            }
                        }
                    });

                    Console.ReadLine();
                    cts.Cancel();
                    await logTask;

                    Console.WriteLine("Data logging complete");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Example 7: Slave Address Scan

        /// <summary>
        /// Example 7: Slave address scan, 7-bit address, scan from 0x03 to 0x7F
        /// </summary>
        public static async Task SlaveScanExample()
        {
            Console.WriteLine("\n=== Example 7: Slave Address Scan ===");

            using (var device = new Cp2112Device())
            using (var writer = new System.IO.StreamWriter("outputSlave.csv"))
            {
                try
                {
                    await device.OpenAsync();
                    await device.ConfigureAsync(SmbusConfig.Default);

                    // Write CSV header
                    await writer.WriteLineAsync("No.,Address(7-bit),Address(8-bit)");

                    Console.WriteLine("Scanning I2C slave addresses...");

                    int count = 0;
                    // Scan from 0x03 to 0x7F (7-bit addresses)
                    for (byte addr7bit = 0x03; addr7bit <= 0x7F; addr7bit++)
                    {
                        byte addr8bit = (byte)(addr7bit << 1); // Convert to 8-bit address

                        try
                        {
                            // Try to read 1 byte from the slave
                            await device.ReadSlaveAsync(addr8bit, 1);

                            // No exception means a device is present
                            count++;
                            var line = $"{count},0x{addr7bit:X2},0x{addr8bit:X2}";
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("Found:");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(line);
                            await writer.WriteLineAsync(line);
                        }
                        catch (Exception)
                        {
                            var line = $"0x{addr7bit:X2},0x{addr8bit:X2}";
                            Console.Write($"Not found at {line}. ");
                            // Exception means no device at this address, skip
                            continue;
                        }
                    }

                    await writer.FlushAsync();
                    Console.WriteLine($"\nScan complete. Found {count} device(s).");
                    Console.WriteLine($"Results saved to outputSlave.csv");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        #endregion
    }
}
