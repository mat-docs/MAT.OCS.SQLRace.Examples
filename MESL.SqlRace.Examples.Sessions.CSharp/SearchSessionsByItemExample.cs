// <copyright file="SearchSessionsByItemExample.cs" company="McLaren Applied Technologies Ltd">
// Copyright (c) McLaren Applied Technologies Ltd</copyright>

using System;
using System.Collections.Generic;

using MAT.OCS.Core;

using MESL.SqlRace.Domain;

namespace MESL.SqlRace.Examples.Sessions.CSharp
{
    /// <summary>
    /// A Class demonstrating searching of SQL Race database based on SessionDataItems, without loading the session.
    /// </summary>
    public class SearchSessionsByItemExample
    {
        private readonly SessionManager sessionManager;
        private readonly string connectionString;

        /// <summary>
        /// Initialises a new instance of the <see cref="SearchSessionsByItemExample"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        public SearchSessionsByItemExample(string connectionString)
        {
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();
            this.sessionManager = SessionManager.CreateSessionManager();
            this.connectionString = connectionString;
        }

        /// <summary>
        /// An example demonstrating how to find particular session in the Database based on session detail/item.
        /// </summary>
        /// <returns>String value, an output of the example run</returns>
        public string FindSessionsByItems()
        {
            try
            {
                // Create a session and add data first
                var sessionCreation = new SessionCreationExample(this.connectionString);
                sessionCreation.CreateSessionAndAddData();
                var clientSession = this.sessionManager.CreateSession(this.connectionString, SessionKey.NewKey(), "NewSession", DateTime.Now, "TAG-310");
                var session = clientSession.Session;

                session.Items.Add(new SessionDataItem("Driver Name", "xxxxxxxxxx"));
                session.Items.Add(new SessionDataItem("Driver Height", 170.6));
                clientSession.Close();
                MainForm mainForm = MainForm.Main();

                // Search for only sessions with particular session items 
                var items = new List<SessionDataItem>
                                                  {
                                                      new SessionDataItem("Driver Name", "xxxxxxxxxx"),
                                                      new SessionDataItem("Driver Height", 170.6)
                                                  };
                IList<SessionSummary> sessions = this.sessionManager.FindBySessionItems(items, this.connectionString);
                if (sessions.Count > 0)
                {
                    mainForm.dataGrid.Columns.Add("guidp", "Session GUID");
                    mainForm.dataGrid.Columns.Add("SessionItem1", "SessionItem1");
                    mainForm.dataGrid.Columns.Add("SessionItem2", "SessionItem2");
                    mainForm.dataGrid.Columns.Add("startTime", "Start timestamp");
                    mainForm.dataGrid.Columns.Add("endTime", "End timestamp");
                    mainForm.dataGrid.Columns.Add("laps", "Laps");
                    mainForm.dataGrid.Columns.Add("dateOfRecording", "Date of recording");
                    foreach (SessionSummary sessionSummary in sessions)
                    {
                        // Session summary object has properties such as Items, Start/End time, Laps etc.
                        mainForm.dataGrid.Rows.Add(new object[]
                                                       {
                                                           sessionSummary.Key,
                                                           string.Format("{0} = {1}", sessionSummary.Items[0].Name, sessionSummary.Items[0].Value), 
                                                           string.Format("{0} = {1}", sessionSummary.Items[1].Name, sessionSummary.Items[1].Value),
                                                           sessionSummary.StartTime,
                                                           sessionSummary.EndTime, 
                                                           sessionSummary.Laps.Count, 
                                                           sessionSummary.TimeOfRecording, 
                                                       });
                    }
                }

                return string.Format("The given Session details found {0} sessions in the Database", sessions.Count);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}