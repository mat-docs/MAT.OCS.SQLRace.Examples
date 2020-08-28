// <copyright file="FindAllSessionsExample.cs" company="McLaren Applied Technologies Ltd">
// Copyright (c) McLaren Applied Technologies Ltd</copyright>

using System;
using System.Collections.Generic;

using MESL.SqlRace.Domain;

namespace MESL.SqlRace.Examples.Sessions.CSharp
{
    /// <summary>
    /// Class for finding all the sessions in the SQL Race Database.
    /// </summary>
    public class FindAllSessionsExample
    {
        /// <summary>
        /// Session Manager instance
        /// </summary>
        private readonly SessionManager sessionManager;

        /// <summary>
        /// Connection string for SQL Server
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        /// Initialises a new instance of the <see cref="FindAllSessionsExample"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        public FindAllSessionsExample(string connectionString)
        {
            Core.LicenceProgramName = "SQLRace";
            Core.Initialize();
            this.sessionManager = SessionManager.CreateSessionManager();
            this.connectionString = connectionString;
        }

        /// <summary>
        /// An example demonstrating how to find all the sessions in a SQL race database.
        /// </summary>
        /// <returns>String value, an output of the example run</returns>
        public string FindAllSessionsInDataBase()
        {
            try
            {
                // Create a session and add data first
                var sessionCreation = new SessionCreationExample(this.connectionString);
                sessionCreation.CreateSessionAndAddData();
                MainForm mainForm = MainForm.Main();

                // Search for all sessions in the database
                IList<SessionSummary> sessions = this.sessionManager.FindBySessionItems(null, this.connectionString);
                if (sessions.Count > 0)
                {
                    mainForm.dataGrid.Columns.Add("guidp", "Session GUID");
                    mainForm.dataGrid.Columns.Add("startTime", "Start timestamp");
                    mainForm.dataGrid.Columns.Add("endTime", "End timestamp");
                    mainForm.dataGrid.Columns.Add("laps", "Laps");
                    mainForm.dataGrid.Columns.Add("dateOfRecording", "Date of recording");

                    foreach (SessionSummary sessionSummary in sessions)
                    {
                        mainForm.dataGrid.Rows.Add(new object[]
                                                       {
                                                           sessionSummary.Key, 
                                                           sessionSummary.StartTime, 
                                                           sessionSummary.EndTime, 
                                                           sessionSummary.Laps.Count, 
                                                           sessionSummary.TimeOfRecording
                                                       });
                    }
                }

                return string.Format("The given connection string found {0} sessions in the Database", sessions.Count);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}