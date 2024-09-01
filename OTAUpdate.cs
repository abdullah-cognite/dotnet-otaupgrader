using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OTAUpdate
{
    public static class OTAUpdateHandler
    {

        public static string upgradeScriptPath = GetCrossPlatformScriptPath(AppDomain.CurrentDomain.BaseDirectory);

public static string WindowsScriptTemplate = @"
param (
    [string]$ServiceName = ""${SERVICE_NAME}""
    [string]$UpgradeFileName = ""${UPGRADE_FILENAME}""
    [string]$ExistingServiceFileName = ""${UPGRADE_FILENAME}""
)

if (-not $ServiceName) {
    Write-Host 'Error: No service name provided.'
    Write-Host 'Usage: .\restart_service.ps1 <service-name>'
    exit 1
}

if (-not $UpgradeFileName) {
    Write-Host 'Error: No upgrade file name provided.'
    Write-Host 'Usage: .\restart_service.ps1 <service-name> <upgrade-file-name>'
    exit 1
}

if (-not $ExistingServiceFileName) {
    Write-Host 'Error: No existing service file name provided.'
    Write-Host 'Usage: .\restart_service.ps1 <service-name> <upgrade-file-name> <existing-service-file-name>'
    exit 1
}

try {
    # Stop the service
    Write-Host 'Stopping Windows service: $ServiceName'
    Stop-Service -Name $ServiceName -Force

    # Wait for 10 seconds
    Write-Host 'Waiting 10 seconds before copying the new service...'
    Start-Sleep -Seconds 10

    # Replace the service binary with the new version
    Write-Host 'Replacing the service binary with the new version...'
    Copy-Item -Path $UpgradeFileName -Destination $ExistingServiceFileName -Force

    # wait for 10 seconds
    Write-Host 'Waiting 10 seconds before starting the service...'

    # Start the service
    Write-Host 'Starting Windows service: $ServiceName'
    Start-Service -Name $ServiceName

    Write-Host 'Service $ServiceName restarted successfully.'
} catch {
    Write-Host 'Error: $_'
    exit 1
}
";


public static string UnixScriptTemplate = @"
#!/bin/bash

# Definitions
SERVICE_NAME=\""${SERVICE_NAME}\""
UPGRADE_FILENAME=\""${UPGRADE_FILENAME}\"" # Must be in the same folder as this script
EXISTING_SERVICE_FILENAME=\""${EXISTING_SERVICE_FILENAME}\"" # Must be in the same folder as this script

# Determine the operating system
if [[ ""$OSTYPE"" == ""darwin""* ]]; then
    # macOS
    SERVICE_PLIST=""/Library/LaunchDaemons/${SERVICE_NAME}.plist""
    if [ -f ""$SERVICE_PLIST"" ]; then
        echo ""Stopping macOS service: $SERVICE_NAME""
        sudo launchctl unload ""$SERVICE_PLIST""
        
        echo ""Waiting 10 seconds before copying the new service...""
        sleep 10

        echo ""Replacing the service binary with the new version...""
        cp ""$UPGRADE_FILENAME"" ""$EXISTING_SERVICE_FILENAME""

        echo ""Waiting 10 seconds before starting the service...""
        sleep 10
        
        echo ""Starting macOS service: $SERVICE_NAME""
        sudo launchctl load ""$SERVICE_PLIST""
    else
        echo ""Error: Service plist file not found: $SERVICE_PLIST""
        exit 1
    fi
elif [[ ""$OSTYPE"" == ""linux-gnu""* ]]; then
    # Linux
    echo ""Stopping Linux service: $SERVICE_NAME""
    sudo systemctl stop ""$SERVICE_NAME""
    
    echo ""Waiting 10 seconds before copying the service...""
    sleep 10

    echo ""Replacing the service binary with the new version...""
    cp ""$UPGRADE_FILENAME"" ""$EXISTING_SERVICE_FILENAME""

    echo ""Waiting 10 seconds before starting the service...""
    sleep 10
    
    echo ""Starting Linux service: $SERVICE_NAME""
    sudo systemctl start ""$SERVICE_NAME""
else
    echo ""Error: Unsupported operating system: $OSTYPE""
    exit 1
fi

echo ""Service $SERVICE_NAME restarted successfully.""
";

        public static string GetCrossPlatformScriptPath(string basedir) {
            return Path.Combine(basedir, "upgrade" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".ps1" : ".sh"));
        }

        public static void CreateUpgrader( string upgradeFileName, string existingServiceFileName, string serviceName)
        {
            string scriptContent ;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                scriptContent = WindowsScriptTemplate.Replace("${SERVICE_NAME}", serviceName);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                scriptContent = UnixScriptTemplate.Replace("${SERVICE_NAME}", serviceName);
            }
            scriptContent = UnixScriptTemplate.Replace("${SERVICE_NAME}", serviceName);
            scriptContent = scriptContent.Replace("${UPGRADE_FILENAME}", upgradeFileName);
            scriptContent = scriptContent.Replace("${EXISTING_SERVICE_FILENAME}", existingServiceFileName);
            CreateScript(upgradeScriptPath, scriptContent);
        }

        public static void CreateScript(string scriptPath, string scriptContent)
        {
            
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }

            // Write the script to the file
            File.WriteAllText(scriptPath, scriptContent);

            // Ensure the script is executable (if running on Unix-like systems)
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var chmodProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{scriptPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                chmodProcess.Start();
                chmodProcess.WaitForExit();
            }
        }

        public static void PerformUpgrade() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-File \"{upgradeScriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (error == null)
                    {
                        Console.WriteLine("Script executed successfully.");
                    }
                    else
                    {
                        throw new Exception("Error executing the script. " + output);
                    }
                }
            } else {
                // Run the update script
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"\"{upgradeScriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    // Optional: Read the script output
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (error == null)
                    {
                        Console.WriteLine("Script executed successfully.");
                    }
                    else
                    {
                        throw new Exception("Error executing the script. " + output);
                    }
                }
            }

            
        }
    }
}
