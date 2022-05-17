<img src="/images/malogo.png" width="300" align="right" /><br><br><br>

# McLaren Applied **SQLRace API Sample Code**.

Collection of example code and best practices to use the SQLRace API.

The SQLRace API is used to read and write time-series data stored in supported database and file formats. It provides mechanisms for querying sessions, extracting and analysing data, statistical operations and custom defined maths functions, as well as recording live data.

SQLRace API is available as a Nuget package to registered users from the **[McLaren Applied Nuget Repository](https://github.com/mat-docs/packages)**. 

See the [API Documentation](https://mat-docs.github.io/)

## Notes on upgrading MESL.SQLRace.API package from versions prior to 2.1.22127.1

Version 2.1.22127.1 of MESL.SQLRace.API upgrades System.Reactive from 3.1.1 to 4.4.1. If you are developing your solution in Visual Studio then be advised that the Nuget upgrade process does not automatically remove any previously dependent packages that are no longer required. 
In order to reduce the likelihood of runtime errors we strongly recommend you manually remove (in the order specified) these redundant packages following the upgrade:

1. System.Reactive.Interfaces 3.1.1
2. System.Reactive.Linq 3.1.1
3. System.Reactive.PlatformServices 3.1.1
4. System.Reactive.Core 3.1.1
5. System.Reactive.Windows.Threading 3.1.1


