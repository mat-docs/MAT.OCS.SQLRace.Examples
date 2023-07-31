<img src="/images/malogo.png" width="300" align="right" /><br><br><br>

# McLaren Applied **SQLRace API Sample Code**.

Collection of example code and best practices to use the SQLRace API.

The SQLRace API is used to read and write time-series data stored in supported database and file formats. It provides mechanisms for querying sessions, extracting and analysing data, statistical operations and custom defined maths functions, as well as recording live data.

SQLRace API is available as a Nuget package to registered users from the **[McLaren Applied Nuget Repository](https://github.com/mat-docs/packages)**. 

See the [API Documentation](https://mat-docs.github.io/)

## Notes on upgrading MESL.SQLRace.API package from versions prior to 2.1.22127.1

Version 2.1.22127.1 of MESL.SQLRace.API upgrades System.Reactive from 3.1.1 to 4.4.1. If you are developing your solution in Visual Studio then be advised that the Nuget upgrade process does not automatically remove any previously dependent packages that are no longer required. 
In order to reduce the likelihood of runtime errors we strongly recommend you manually remove (in the order specified) these redundant packages following the upgrade:

1. System.Reactive.Windows.Threading 3.1.1
2. System.Reactive.PlatformServices 3.1.1
3. System.Reactive.Linq 3.1.1
4. System.Reactive.Core 3.1.1
5. System.Reactive.Interfaces 3.1.1



# ATLAS 10 COM API Migration GUIDE!

* **9.2.3 APIs and Libraries**
This example references the following libraries:
• MAT_Atlas_Automation_Client
• MAT_Atlas_Automation_Api
N.B. before these can be used they first must be registered. It is planned for this to be part of the installer once officially released. See Registering Automation API DLLs.

# Registering Automation API DLLs

The following DLLs need to be registered to allow the WCF API to be usable from COM (and thus
VBA):
• MAT.Atlas.Automation.Client
• MAT.Atlas.Automation.Api

If you are upgrading ATLAS 10 and plan to use the latest automation DLLs you must unregister previous registrations first. N.B. registration is not required to use the WCF API from C# or MATLAB.
### Register-> .NET Framework:
a. Run cmd.exe as administrator
b. Change directory to the location of regasm.exe (Registration Assembly Tool)
N.B. this can be found in the .Net Framework 4 installation folder. This version number may
vary depending upon the exact .Net version installed.
c. Register MAT.Atlas.Automation.Api.dll with the command below.
d. Register MAT.Atlas.Automation.Client.dll with the command below.

### Register-> .NET 6.0:
a. Download dscom.exe from the release page (https://github.com/dspace-group/dscom, with this library you can register assemblies and classes for COM and programmatically generate TLBs at runtime)
b. Run cmd.exe as administrator, change directory location to  dscom.exe location
c. Register MAT.Atlas.Automation.Api.dll with the commands below.

 1. dscom.exe tlbexport "{{full file path to MAT.Atlas.Automation.Api.dll}}"
 2. dscom.exe tlbregister "{{full file path to newly created tbl file}}"

d. Register MAT.Atlas.Automation.Client.dll with the command below.
 1. dscom.exe tlbexport "{{full file path to MAT.Atlas.Automation.Client.dll}}"
 2. dscom.exe tlbregister "{{full file path to newly created tbl file}}"

### Unregister-> .NET Framework
a. Run cmd.exe as administrator
b. Change directory to the location of regasm.exe (Registration Assembly Tool)
N.B. this can be found in the .Net Framework 4 installation folder. This version number may
vary depending upon the exact .Net version installed.
c. Register MAT.Atlas.Automation.Api.dll with the command below.
d. Register MAT.Atlas.Automation.Client.dll with the command below.

### Unregister-> .NET 6.0:
a. Run cmd.exe as administrator, change directory location to  dscom.exe location
b. Change directory to the location of regasm.exe (Registration Assembly Tool)

c. UnRegister MAT.Atlas.Automation.Api.dll and MAT.Atlas.Automation.Client.dll with the commands below.

 1. dscom.exe tlbunregister "{{full file path to MAT.Atlas.Automation.Api.dll}}"
 2. dscom.exe tlbunregister "{{full file path to MAT.Atlas.Automation.Client.dll}}"

### Debug and Run
a. Launch ATLAS 10
b. Load a Session into Set 1 via the Session Browser
c. Open HelloWorld.Vba.xlsm
d. Go to the Developer tab
e. Select Visual Basic
f. Double click the HelloWord under Modules to open
g. Add breakpoints in and click the Atlas 10 Hello World button or press F5 to start debugging




