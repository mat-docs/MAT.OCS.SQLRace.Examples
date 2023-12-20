using MAT.OCS.Core;
using MESL.SqlRace.Common.Extensions;
using MESL.SqlRace.Domain;
using System;
using System.Linq;
using System.Threading;
using System.Net;


namespace MAT.SqlRace.ServerListenerLive
{
    /// <summary>
    /// Record live data to a local SqlRace (Sqlite)
    ///
    /// NOTE: 
    ///     If the session is in the state LiveNotInServer, make sure Server Listener port in ATLAS is different to the one specified here. 
    ///     Additionally, ensure UDP and TCP packets are allowed through the firewall settings on the Server Listener Port configured. 
    /// </summary>
    internal class Program
    {
        // The server listener configuration
        private const int ServerListenerPortNumber = 6565;
        private const string ServerListenerIpAddress = "127.0.0.1";

        public static void Main(string[] args)
        {
            var dataSource = @"C:\temp\livesession.ssndb";
            Console.WriteLine(dataSource);

            /// connection strings are case and whitespace sensitive, the following format must be strictly followed for the Server Listener Protocol to successfully establish.
            /// SQLite: "DbEngine=SQLite;Data Source={dataSource};Pooling=false;"
            /// SQLServer: "server={dataSource};Initial Catalog={database};Trusted_Connection=True;"
            var connectionString = $@"DbEngine=SQLite;Data Source={dataSource};Pooling=false;";
            string recorderDbEngine = "SQLite"; // SQLite or SQLServer
            var sessionIdentifier = "Server Listener Live Demo";

            Console.WriteLine("Initialising");
            Core.Initialize();

            Console.WriteLine("Setting up Server Listener Instance");
            Core.ConfigureServer(true, new IPEndPoint(IPAddress.Parse(ServerListenerIpAddress), ServerListenerPortNumber));
            var sessionManager = SessionManager.CreateSessionManager();
            var recordersConfiguration = RecordersConfiguration.GetRecordersConfiguration();
            recordersConfiguration.AddConfiguration(Guid.NewGuid(), recorderDbEngine, dataSource, dataSource, connectionString, false);


            Console.WriteLine("Creating new Session");
            var clientSession = sessionManager.CreateSession(connectionString, SessionKey.NewKey(), sessionIdentifier, DateTime.Now, "Session");
            var session = clientSession.Session;
            try
            {
                var parameter = SessionHelper.CreateSessionConfigurationForOneParameter(session);

                for (int i = 0; i < 1000; i++)
                {
                    var newTimestamp = DateTime.Now.ToNanoseconds();
                    var newValue = Math.Sin(i / 360.0);

                    session.AddChannelData(
                        parameter.ChannelIds.FirstOrDefault(),
                        newTimestamp,
                        1, //value of 1 param
                        BitConverter.GetBytes(newValue));
                    Thread.Sleep(100);
                    Console.WriteLine($"Written sample. Timestamp: {newTimestamp.ToTimeString()} Value:{newValue}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                session.EndData();
                clientSession.Close();
            }
        }


    }
}
