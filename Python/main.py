import os
import time
import clr # Pythonnet
import subprocess
import threading

# The path to the main SQL Race DLL. This is the default location when installed with Atlas 10
sql_race_dll_path = r"C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll"
# The paths to Automation API DLL files. 
automation_api_dll_path = r"C:\Program Files\McLaren Applied Technologies\ATLAS 10\MAT.Atlas.Automation.Api.dll"
automation_client_dll_path = r"C:\Program Files\McLaren Applied Technologies\ATLAS 10\MAT.Atlas.Automation.Client.dll"
# The connection string to the database containing our sessions
connection_string = r"Server=CLU-LDF01\LOCAL;Initial Catalog=SQLRACE01_2019;Trusted_Connection=True;"

# Configure Pythonnet and reference the required assemblies for dotnet and SQL Race
clr.AddReference("System.Collections")
clr.AddReference("System.Core")
clr.AddReference("System.IO")

if not os.path.isfile(sql_race_dll_path):
    raise Exception("Couldn't find SQL Race DLL at " + sql_race_dll_path + " please check that Atlas 10 is installed")

clr.AddReference(sql_race_dll_path)

if not os.path.isfile(automation_api_dll_path):
    raise Exception(f"Couldn't find Automation API DLL at {automation_api_dll_path}.")

clr.AddReference(automation_api_dll_path)

if not os.path.isfile(automation_client_dll_path):
    raise Exception(f"Couldn't find Automation Client DLL at {automation_client_dll_path}.")

clr.AddReference(automation_client_dll_path)


from System.Linq import Enumerable
from MESL.SqlRace.Domain import *
from MESL.SqlRace.Domain.Query import *
from MESL.SqlRace.Domain.Sort import *
from MESL.SqlRace.Domain.Infrastructure.Enumerators import *
from MAT.Atlas.Automation.Client.Services import *
from MAT.Atlas.Automation.Api.Models import *
from System.IO import Path
from System import AppDomain

keys = []

def initialise_sql_race():
    # Tell SQL Race which part of our licence we are using then initialise it
    print('Initialising SQL Race...')
    Core.LicenceProgramName = 'SQLRace'
    Core.Initialize()

def extract_data(sessionSummary):
    # The SessionManager is used to load the sessions
    sessionManager = SessionManager.CreateSessionManager()

    # This is the point at which the session is actually loaded. Note that if the Key of the session is
    # already known (e.g. by inspection through the Session Browser in A10) then the previous stage of
    # querying the database is not required.
    print("Loading session " + sessionSummary.Identifier)
    clientSession = sessionManager.Load(sessionSummary.Key, sessionSummary.GetConnectionString())
    session = clientSession.Session

    # Load the configuration associated with the session to decode the parameters, events etc.
    session.LoadConfiguration()

    # A list of parameters that we are going to load in turn
    parameters = ["vCar:Chassis", "gLat:Chassis"]

    for parameter in parameters:
        # Create a ParameterDataAccess object to extract data for this parameter from the session
        pda = session.CreateParameterDataAccess(parameter)

        # Go to the start of the second lap
        pda.GoTo(session.Laps.get_Item(1).StartTime)

        # Get the next n samples 
        samples = pda.GetNextSamples(20, StepDirection.Forward)

        # Print the samples that we have just extracted to the console
        print(parameter)
        for i in range(samples.SampleCount):
            print(f"{samples.Timestamp[i]} {samples.Data[i]}")

        # Release resources from the PDA
        pda.Dispose()
    # Release the resources from the Session. Failure to do this could result in a memory leak.
    clientSession.Dispose()

def get_event_details(sessionSummary):
    # Dictionary to keep event definitions alogn with event definition id
    conversions = {}

    # The SessionManager is used to load the sessions
    sessionManager = SessionManager.CreateSessionManager()

    # This is the point at which the session is actually loaded. Note that if the Key of the session is
    # already known (e.g. by inspection through the Session Browser in A10) then the previous stage of
    # querying the database is not required.
    print("Loading session " + sessionSummary.Identifier)
    clientSession = sessionManager.Load(sessionSummary.Key, sessionSummary.GetConnectionString())
    session = clientSession.Session

    # Creating an event definitions dictionary
    definitions = session.EventDefinitions
    for definition in definitions:
       conversions.update({definition.EventDefinitionId : definition.Description})
      
    # Getting event data from events collection since session start till the session end
    eventCollection = session.Events
    events = eventCollection.GetEventData(session.StartTime, session.EndTime)

    # Iterating through the first 50 session events and printing details
    for event in events[:50]:
        # Getting an event description using event definition key from conversion dictionary
        eventDescription = conversions.get(event.EventDefinitionKey)
        # Getting event status text from event collection
        statusText = eventCollection.GetDisplayText(event)
        print(f'{event.TimeStamp}: {event.Key} | {event.GroupName} | {eventDescription} | {statusText}')
       

    clientSession.Dispose()


def find_sessions():
    print("Finding sessions...")
    # Create a filter to find all sessions within a given date range
    #
    #  Available SessionFieldIdentifiers to filter on are StartTime, EndTime, LapCount, FastestLapTime, SessionIdentifier,
    # SessionType, SessionState, SessionStartDateTime, SessionKey
    #
    # CompositeFilters can be combined hierarchically with other CompositeFilters to allow complex queries to be created
    filter = CompositeFilter(CombineType.AND)
    filter.Add(ScalarFilter(SessionFieldIdentifiers.SessionStartDateTime, MatchingRule.GreaterThanOrEquals, "2019-08-04 00:00:00", True))
    filter.Add(ScalarFilter(SessionFieldIdentifiers.SessionStartDateTime, MatchingRule.LessThan, "2019-08-05 00:00:00", True))

    # Execute the query against the database. Note that this returns SessionSummary objects which just contain session metadata,
    # the session has not been loaded at this point.
    qm = QueryManager.CreateQueryManager(connection_string)
    qm.Filter = filter
    queryResult = qm.ExecuteQuery()

    # Process each of the resulting sessions in turn
    for sessionSummary in Enumerable.ToList[SessionSummary](queryResult):
        print("Found session " + sessionSummary.Identifier)

        # Adding found session key to the list of sessions to be loaded using API
        keys.append(sessionSummary.Key.ToString("D"))

        # Skip this session if it doesn't have a second lap
        if sessionSummary.Laps.Count < 2:
            continue

        extract_data(sessionSummary)
        get_event_details(sessionSummary)

def set_cursor(keys, connection_strings, timestamp):
    # Function to set cursor in ATLAS with loaded session to specific timestamp

    app = ApplicationServiceClient()
    workbookServiceClient =  WorkbookServiceClient()
    setServiceClient = SetServiceClient()

    # Open ATLAS10 from default installation location
    subprocess.Popen(r"C:\Program Files\McLaren Applied Technologies\ATLAS 10\MAT.ATLAS.Host.exe")
    print("Waiting for ATLAS to open")

    # set up multiprocessing.Lock to be use to block until client is connected.
    connect = threading.Lock()
    connect.acquire()

    def client_connected(client_name):
        # event handler for OnClientConnected
        print('\nATLAS Client connected.')
        print(f'ATLAS version: {app.GetVersion()}')
        connect.release()

    app.OnClientConnected += client_connected
    app.Connect(Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName));
    
    # Wait until client is connected. The next line cannot run until connect is released by the handeler function.
    connect.acquire()
    connect.release()


    setsList = workbookServiceClient.GetSets()
    
    load = threading.Lock()
    load.acquire()

    def session_loaded(session_loaded):
        # event handeler for OnSessionLoaded
           print('Session loaded.')
           load.release()
    app.OnSessionLoaded += session_loaded

    # Loading session from SQL Race into specified set, using session keys found in SQLRace API and connection string to SQL Race
    print('\nLoading session...')
    app.LoadSqlRaceSessions(setsList[0].Id, keys, connection_strings)

    # wait for session to load
    load.acquire()
    load.release()

    # Getting through the APIs to get the already loaded session ID 
    sessionServiceClient = SessionServiceClient()
    sessions = setServiceClient.GetCompositeSessions(setsList[0].Id)
    session_id = sessions[0].Id


    # Getting current session timebase for session ID
    timebase = sessionServiceClient.GetSessionTimeBase(session_id)
    timeBaseService = TimeBaseServiceClient(timebase.Id)

    # Setting cursor in A10 to specific timestamp
    timeBaseService.SetCursor(timebase.Id, timestamp)

def main():
    connection_strings = []
    # Building connection strings list for LoadingSQLRaceSession
    connection_strings.append(connection_string)

    initialise_sql_race()
    find_sessions()

    set_cursor(keys, connection_strings, 47718380000000)
    

if __name__ ==  "__main__": main()
