// <copyright file="MainForm.cs" company="McLaren Applied Technologies Ltd">
// Copyright (c) McLaren Applied Technologies Ltd</copyright>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;

using MAT.OCS.Core;

namespace MESL.SqlRace.Examples.Sessions.CSharp
{
    /// <summary>
    /// Main form of the application
    /// </summary>
    public partial class MainForm : Form
    {
        private static MainForm main;

        private MainForm()
        {
            this.InitializeComponent();
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            this.connectionStringTextBox.Text = connectionString;
            this.EnableAllContextMenu();
        }

        /// <summary>
        /// Main form is a singleton object
        /// </summary>
        /// <returns>Singleton instance of the Main form</returns>
        public static MainForm Main()
        {
            return main ?? (main = new MainForm());
        }

        /// <summary>
        /// Method for clicking the CreateSession button
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnCreateSessionClick(object sender, EventArgs e)
        {
            this.resultMessage.Text = " ";
            this.dataGrid.Rows.Clear();
            this.display.Series[0].Points.Clear();
            this.DisableAllButtons();

            // Call the Create Session example method to create session and add data
            var sessionCreationExample = new SessionCreationExample(this.connectionStringTextBox.Text);
            var key = sessionCreationExample.CreateSessionAndAddData();
            this.resultMessage.Text = key != SessionKey.Empty ? 
                                        string.Format("\nSession with the Key {0} created successfully.", key) 
                                        : "\nError creating the session";

            this.EnableAllButtons();
        }

        /// <summary>
        /// Method for clicking the Load session button
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnLoadSessionClick(object sender, EventArgs e)
        {
            this.resultMessage.Text = string.Empty;
            this.dataGrid.Rows.Clear();
            this.dataGrid.Columns.Clear();
            this.display.Series[0].Points.Clear();
            this.DisableAllButtons();

            // Call the example method which creates a session and adds data and then loads the session
            var loadSessionExample = new LoadSessionExample(this.connectionStringTextBox.Text);
            var message = loadSessionExample.LoadSessionAndReadData();
            if (message != null)
            {
                this.resultMessage.Text = message;
                this.display.Series[0].Points.DataBindXY(ConvertTimeStampToDateTime(loadSessionExample.TimeStamps), loadSessionExample.Data);
                this.dataGrid.Columns.Add("timeStamp", "Time stamp");
                this.dataGrid.Columns.Add("data", "Samples");
                this.dataGrid.Columns.Add("status", "Data Status");
                for (int i = 0; i < loadSessionExample.Data.Length; i++)
                {
                    this.dataGrid.Rows.Add(new object[]
                                          {
                                              loadSessionExample.TimeStamps[i], loadSessionExample.Data[i],
                                              loadSessionExample.DataStatus[i]
                                          });
                }
            }
            else
            {
                this.resultMessage.Text = "\nError loading the session";
            }

            this.EnableAllButtons();
        }

        /// <summary>
        /// Method for clicking the Find All sessions example button
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnFindAllSessionsClick(object sender, EventArgs e)
        {
            this.resultMessage.Text = string.Empty;
            this.dataGrid.Rows.Clear();
            this.dataGrid.Columns.Clear();
            this.display.Series[0].Points.Clear();
            this.DisableAllButtons();

            // Call the example method which creates a session and finds all the sessions in the database
            var findAllSessionsExample = new FindAllSessionsExample(this.connectionStringTextBox.Text);
            var message = findAllSessionsExample.FindAllSessionsInDataBase();
            this.resultMessage.Text = message ?? "Error finding all the sessions in the Database";

            this.EnableAllButtons();
        }

        /// <summary>
        /// Method for clicking the Find session by Items button
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnFindByItemsClick(object sender, EventArgs e)
        {
            this.resultMessage.Text = string.Empty;
            this.dataGrid.Rows.Clear();
            this.dataGrid.Columns.Clear();
            this.display.Series[0].Points.Clear();
            this.DisableAllButtons();

            // Call the example method which creates a session and adds items and then searches for the created session
            var findSessionsByItems = new SearchSessionsByItemExample(this.connectionStringTextBox.Text);
            string message = findSessionsByItems.FindSessionsByItems();
            this.resultMessage.Text = message ?? "Error finding the sessions by Items";

            this.EnableAllButtons();
        }
        
        /// <summary>
        /// Enables all buttons in the form
        /// </summary>
        private void EnableAllButtons()
        {
            this.loadSession.Enabled = true;
            this.createSession.Enabled = true;
            this.findAllSessions.Enabled = true;
            this.findByItems.Enabled = true;
        }

        /// <summary>
        /// Disables all the buttons while running the examples
        /// </summary>
        private void DisableAllButtons()
        {
            this.loadSession.Enabled = false;
            this.createSession.Enabled = false;
            this.findAllSessions.Enabled = false;
            this.findByItems.Enabled = false;
        }

        private void EnableAllContextMenu()
        {
            this.createSessionHelp.Items[0].Text = this.helpToolTip.GetToolTip(this.createSession);
            this.loadSessionHelp.Items[0].Text = this.helpToolTip.GetToolTip(this.loadSession);
            this.FindAllSessionsHelp.Items[0].Text = this.helpToolTip.GetToolTip(this.findAllSessions);
            this.findByItemHelp.Items[0].Text = this.helpToolTip.GetToolTip(this.findByItems);
        }

        /// <summary>
        /// Convert the time stamp to date time string for graphical display
        /// </summary>
        /// <param name="timeStamps">Array of long time stamps</param>
        /// <returns>Converted data time strings</returns>
        private static IEnumerable<string> ConvertTimeStampToDateTime(IEnumerable<long> timeStamps)
        {
            var dates = new List<string>();
            foreach (long timeStamp in timeStamps)
            {
                var hr = Convert.ToInt32(timeStamp / 3600000000000);
                var min = Convert.ToInt32((timeStamp - (hr * 3600000000000)) / 60000000000);
                var sec = (timeStamp - ((hr * 3600000000000) + (min * 60000000000))) / (double)1000000000;
                var time = DateTime.Parse(string.Format("{0}:{1}:{2}", hr, min, sec));
                dates.Add(time.ToString("hh:mm:ss.fff"));
            }

            return dates;
        }
    }
}