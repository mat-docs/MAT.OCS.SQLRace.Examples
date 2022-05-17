<img src="/images/malogo.png" width="300" align="right" /><br><br><br>

# McLaren Applied **SQLRace API Sample Code**.

Collection of example code and best practices to use the SQLRace API.

The SQLRace API is used to read and write time-series data stored in supported database and file formats. It provides mechanisms for querying sessions, extracting and analysing data, statistical operations and custom defined maths functions, as well as recording live data.

SQLRace API is available as a Nuget package to registered users from the **[McLaren Applied Nuget Repository](https://github.com/mat-docs/packages)**. 

See the [API Documentation](https://mat-docs.github.io/)

## Notes on upgrading Atlas.DisplayAPI package from versions prior to 11.1.2.344

Version 11.1.2.344 of Atlas.DisplayAPI upgrades System.Reactive from 3.1.1 to 4.4.1. If you are developing your solution in Visual Studio then be advised that the Nuget upgrade process does not automatically remove any previously dependent packages that are no longer required. 
In order to reduce the likelihood of runtime errors we strongly recommend you manually remove the following redundant package following the upgrade:

1. System.Reactive.Interfaces 3.1.1


