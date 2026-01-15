using System;
using System.Threading.Tasks;
using Cp2112Sdk;
using Cp2112Sdk.Models;
using Cp2112Sdk.Events;
using Cp2112Sdk.Exceptions;

namespace ConsoleDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("CP2112 Device Console Demo");
            Console.WriteLine("============================");
            Console.WriteLine();

            Console.WriteLine("This demo will attempt to:");
            Console.WriteLine("1. Scan for CP2112 devices");
            Console.WriteLine("2. Open the first device found");
            Console.WriteLine("3. Configure the device");
            Console.WriteLine("4. Perform a simple read operation");
            Console.WriteLine();

            try
            {
                using var device = new Cp2112Device();

                // Subscribe to events
                device.StateChanged += (s, e) =>
                {
                    Console.WriteLine($"[Event] State changed: {e.PreviousState} -> {e.CurrentState}");
                };

                device.ErrorOccurred += (s, e) =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Event] Error: {e.ErrorMessage}");
                    Console.ResetColor();
                };

                // Open device
                Console.WriteLine("Opening device...");
                await device.OpenAsync();
                Console.WriteLine($"Device opened successfully!");
                Console.WriteLine($"  Serial Number: {device.SerialNumber}");
                Console.WriteLine($"  Device Number: {device.DeviceNumber}");
                Console.WriteLine();

                // Configure device
                Console.WriteLine("Configuring device...");
                var config = SmbusConfig.Default;
                await device.ConfigureAsync(config);
                Console.WriteLine($"Device configured with bitrate: {config.BitRate} Hz");
                Console.WriteLine();

                byte slave_address = 0xC0;
                // Try to read from a register (this is example-specific)
                Console.WriteLine("Attempting to read from register 0x8D...");
                try
                {
                    // two byte read ºÄÊ±Ô¼4ms
                    var data = await device.ReadAsync(slave_address, 2, 0x8D);
                    Console.WriteLine($"Read {data.Length} bytes: {BitConverter.ToString(data)}");
                    data = await device.ReadAsync(slave_address, 2, 0x8D);
                    Console.WriteLine($"Read {data.Length} bytes: {BitConverter.ToString(data)}");
                    data = await device.ReadAsync(slave_address, 2, 0x8D);
                    Console.WriteLine($"Read {data.Length} bytes: {BitConverter.ToString(data)}");
                    data = await device.ReadAsync(slave_address, 2, 0x8D);
                    Console.WriteLine($"Read {data.Length} bytes: {BitConverter.ToString(data)}");
                    data = await device.ReadAsync(slave_address, 2, 0x8D);
                    Console.WriteLine($"Read {data.Length} bytes: {BitConverter.ToString(data)}");
                    data = await device.ReadAsync(slave_address, 2, 0x8D);
                    Console.WriteLine($"Read {data.Length} bytes: {BitConverter.ToString(data)}");
                    data = await device.ReadAsync(slave_address, 2, 0x8D);
                    Console.WriteLine($"Read {data.Length} bytes: {BitConverter.ToString(data)}");

                }
                catch (DeviceNotFoundException)
                {
                    Console.WriteLine("Note: No device found (this is expected if no CP2112 device is connected)");
                }
                catch (ReadTimeoutException)
                {
                    Console.WriteLine("Note: Read timed out at slave address 0x{0:X} (this is expected if no SMBus device is connected)", slave_address>>1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Read operation completed with status: {ex.Message}");
                }

                Console.WriteLine();
                Console.WriteLine("Demo completed successfully!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (DeviceNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: No CP2112 device found!");
                Console.WriteLine("Please ensure:");
                Console.WriteLine("  - The CP2112 device is connected via USB");
                Console.WriteLine("  - The drivers are installed");
                Console.WriteLine("  - The CP2112 device is not used by other software");

                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.WriteLine($"Details: {ex}");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
