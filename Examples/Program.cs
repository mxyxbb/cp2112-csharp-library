using System;
using System.Threading.Tasks;

namespace Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║     CP2112 Device Examples - Choose an Example           ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Available Examples:");
                Console.WriteLine("  1. Basic Device Operation");
                Console.WriteLine("  2. Multi-Device Management");
                Console.WriteLine("  3. Data Monitoring with Events");
                Console.WriteLine("  4. Robust Error Handling");
                Console.WriteLine("  5. Data Logging to CSV");
                Console.WriteLine("  6. Scan Slave Address on I2C Bus");
                Console.WriteLine("  0. Exit");
                Console.WriteLine();

                Console.Write("Select an example (0-6): ");
                var input = Console.ReadLine();

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════════════════════════════");

                try
                {
                    switch (input)
                    {
                        case "1":
                            await ExampleUsage.BasicDeviceOperationExample();
                            break;
                        case "2":
                            await ExampleUsage.MultiDeviceManagementExample();
                            break;
                        case "3":
                            await ExampleUsage.DataMonitoringExample();
                            break;
                        case "4":
                            await ExampleUsage.RobustErrorHandlingExample();
                            break;
                        case "5":
                            await ExampleUsage.DataLoggingExample();
                            break;
                        case "6":
                            await ExampleUsage.SlaveScanExample();
                            break;
                        case "0":
                            Console.WriteLine("Goodbye!");
                            return;
                        default:
                            Console.WriteLine("Invalid selection. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nError running example: {ex.Message}");
                    Console.WriteLine($"Details: {ex}");
                    Console.ResetColor();
                }

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════════════════════════════");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Console.Clear();

                Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║     CP2112 Device Examples - Choose an Example           ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
                Console.WriteLine();
            }
        }
    }
}
