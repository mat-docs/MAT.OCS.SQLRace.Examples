// <copyright file="Program.cs" company="McLaren Applied Technologies Ltd">
// Copyright (c) McLaren Applied Technologies Ltd</copyright>

using System;
using System.Windows.Forms;

namespace MESL.SqlRace.Examples.Sessions.CSharp
{
    /// <summary>
    /// Main entry point of the application
    /// </summary>
    public static class Program
    {   
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(MainForm.Main());
        }
    }
}