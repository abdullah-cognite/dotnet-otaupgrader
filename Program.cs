using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OTAUpdate;
class Program
{
    static async Task Main(string[] args)
    {
        string upgradeFileName = "upgrade.bin";
        string serviceNameInRegistry = "my-service";

        string existingServiceFileNameWindows = "my-service.exe";
        string existingServiceFileNameUnix = "my-service";
        string existingServiceFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? existingServiceFileNameWindows : existingServiceFileNameUnix;
        
        OTAUpdateHandler.CreateUpgrader( upgradeFileName, existingServiceFileName, serviceNameInRegistry);

        while (true)
        {
            Console.WriteLine("Checking for updates...");
            
            // Check if the upgrade file exists
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, upgradeFileName)))
            {
                Console.WriteLine("Update found! Restarting the application...");
                try{
                    OTAUpdateHandler.PerformUpgrade();
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Update failed: {ex.Message}");
                }
            }

            // Delay before checking again
            await Task.Delay(10000); // Wait for 10 seconds
        }
    }
}
