## Proof of Concept: .NET-Based OTA Upgrader

### Overview

This PoC demonstrates a simple .NET-based over-the-air (OTA) upgrader for projects that run as services on Windows or Unix systems.

### How It Works

1. This OTAUpdate Helper generates an `upgrader.sh` (for Unix) or `upgrader.bat` (for Windows) file in your project's installation binary folder.

2. The upgrader script is triggered by the entrypoint (e.g `Program.cs`) once the conditions for an upgrade are met (e.g., a new service binary has been downloaded).

3. When invoked, the script first requests the system's service manager to stop the running service.

4. Once the service is stopped, control is released by the main binary, and the service is no longer running.

5. The script then replaces the existing service binary with the new one and starts the updated service using the system's service manager.

6. After starting the upgraded binary, the upgrade process is complete, and the program is successfully updated. 🎉
