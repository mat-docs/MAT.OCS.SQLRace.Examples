namespace MESL.SqlRace.Examples.Sessions.CSharp
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.createSession = new System.Windows.Forms.Button();
            this.createSessionHelp = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.blahToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadSession = new System.Windows.Forms.Button();
            this.loadSessionHelp = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.display = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.dataGrid = new System.Windows.Forms.DataGridView();
            this.resultMessage = new System.Windows.Forms.Label();
            this.findAllSessions = new System.Windows.Forms.Button();
            this.FindAllSessionsHelp = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.findByItems = new System.Windows.Forms.Button();
            this.findByItemHelp = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.connectionStringTextBox = new System.Windows.Forms.TextBox();
            this.connection = new System.Windows.Forms.Label();
            this.databaseGroup = new System.Windows.Forms.GroupBox();
            this.dataResult = new System.Windows.Forms.Label();
            this.summary = new System.Windows.Forms.Label();
            this.helpToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.createSessionHelp.SuspendLayout();
            this.loadSessionHelp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.display)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
            this.FindAllSessionsHelp.SuspendLayout();
            this.findByItemHelp.SuspendLayout();
            this.databaseGroup.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // createSession
            // 
            this.createSession.ContextMenuStrip = this.createSessionHelp;
            this.createSession.Location = new System.Drawing.Point(15, 248);
            this.createSession.Name = "createSession";
            this.createSession.Size = new System.Drawing.Size(155, 29);
            this.createSession.TabIndex = 2;
            this.createSession.Text = "Create Session";
            this.helpToolTip.SetToolTip(this.createSession, resources.GetString("createSession.ToolTip"));
            this.createSession.UseVisualStyleBackColor = true;
            this.createSession.Click += new System.EventHandler(this.OnCreateSessionClick);
            // 
            // createSessionHelp
            // 
            this.createSessionHelp.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.blahToolStripMenuItem});
            this.createSessionHelp.Name = "contextMenuStrip1";
            this.createSessionHelp.Size = new System.Drawing.Size(107, 26);
            // 
            // blahToolStripMenuItem
            // 
            this.blahToolStripMenuItem.Name = "blahToolStripMenuItem";
            this.blahToolStripMenuItem.Size = new System.Drawing.Size(106, 22);
            this.blahToolStripMenuItem.Text = "Help";
            // 
            // loadSession
            // 
            this.loadSession.ContextMenuStrip = this.loadSessionHelp;
            this.loadSession.Location = new System.Drawing.Point(202, 248);
            this.loadSession.Name = "loadSession";
            this.loadSession.Size = new System.Drawing.Size(155, 29);
            this.loadSession.TabIndex = 3;
            this.loadSession.Text = "Load Session";
            this.helpToolTip.SetToolTip(this.loadSession, "Creates a session in the database, closes it and loads it back. Reads the Paramet" +
        "ers, Channels, Conversions, Constants etc. Reads the data using the ParameterDat" +
        "aAccess");
            this.loadSession.UseVisualStyleBackColor = true;
            this.loadSession.Click += new System.EventHandler(this.OnLoadSessionClick);
            // 
            // loadSessionHelp
            // 
            this.loadSessionHelp.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.loadSessionHelp.Name = "contextMenuStrip1";
            this.loadSessionHelp.Size = new System.Drawing.Size(107, 26);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(106, 22);
            this.toolStripMenuItem1.Text = "Help";
            // 
            // display
            // 
            this.display.BorderlineColor = System.Drawing.Color.Transparent;
            chartArea2.Area3DStyle.Inclination = 50;
            chartArea2.Area3DStyle.PointDepth = 50;
            chartArea2.AxisX2.IsMarginVisible = false;
            chartArea2.AxisY2.IsMarginVisible = false;
            chartArea2.Name = "ChartArea1";
            this.display.ChartAreas.Add(chartArea2);
            legend2.Enabled = false;
            legend2.Name = "Legend1";
            this.display.Legends.Add(legend2);
            this.display.Location = new System.Drawing.Point(379, 71);
            this.display.Margin = new System.Windows.Forms.Padding(0);
            this.display.Name = "display";
            this.display.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Bright;
            series2.BorderColor = System.Drawing.Color.Red;
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series2.Color = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            series2.Legend = "Legend1";
            series2.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Diamond;
            series2.Name = "Series1";
            series2.ShadowColor = System.Drawing.Color.White;
            this.display.Series.Add(series2);
            this.display.Size = new System.Drawing.Size(578, 269);
            this.display.TabIndex = 8;
            this.display.Text = "chart1";
            // 
            // dataGrid
            // 
            this.dataGrid.AllowUserToAddRows = false;
            this.dataGrid.AllowUserToDeleteRows = false;
            this.dataGrid.BackgroundColor = System.Drawing.Color.Gray;
            this.dataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGrid.GridColor = System.Drawing.Color.Gray;
            this.dataGrid.Location = new System.Drawing.Point(492, 366);
            this.dataGrid.Name = "dataGrid";
            this.dataGrid.Size = new System.Drawing.Size(465, 175);
            this.dataGrid.TabIndex = 12;
            // 
            // resultMessage
            // 
            this.resultMessage.AutoSize = true;
            this.resultMessage.BackColor = System.Drawing.Color.Gray;
            this.resultMessage.Location = new System.Drawing.Point(12, 366);
            this.resultMessage.MinimumSize = new System.Drawing.Size(465, 175);
            this.resultMessage.Name = "resultMessage";
            this.resultMessage.Size = new System.Drawing.Size(465, 175);
            this.resultMessage.TabIndex = 10;
            // 
            // findAllSessions
            // 
            this.findAllSessions.ContextMenuStrip = this.FindAllSessionsHelp;
            this.findAllSessions.Location = new System.Drawing.Point(15, 293);
            this.findAllSessions.Name = "findAllSessions";
            this.findAllSessions.Size = new System.Drawing.Size(155, 29);
            this.findAllSessions.TabIndex = 5;
            this.findAllSessions.Text = "Find all session in DB";
            this.helpToolTip.SetToolTip(this.findAllSessions, "Adds a session to the database and finds all sessions in a given SQL Race databas" +
        "e");
            this.findAllSessions.UseVisualStyleBackColor = true;
            this.findAllSessions.Click += new System.EventHandler(this.OnFindAllSessionsClick);
            // 
            // FindAllSessionsHelp
            // 
            this.FindAllSessionsHelp.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem3});
            this.FindAllSessionsHelp.Name = "contextMenuStrip1";
            this.FindAllSessionsHelp.Size = new System.Drawing.Size(100, 26);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(106, 22);
            this.toolStripMenuItem3.Text = "Help";
            // 
            // findByItems
            // 
            this.findByItems.ContextMenuStrip = this.findByItemHelp;
            this.findByItems.Location = new System.Drawing.Point(202, 293);
            this.findByItems.Name = "findByItems";
            this.findByItems.Size = new System.Drawing.Size(155, 29);
            this.findByItems.TabIndex = 6;
            this.findByItems.Text = "Find Sessions by Items";
            this.helpToolTip.SetToolTip(this.findByItems, "Adds a session to the database and searches for the added session based on the Se" +
        "ssionDataItem");
            this.findByItems.UseVisualStyleBackColor = true;
            this.findByItems.Click += new System.EventHandler(this.OnFindByItemsClick);
            // 
            // findByItemHelp
            // 
            this.findByItemHelp.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem5});
            this.findByItemHelp.Name = "contextMenuStrip1";
            this.findByItemHelp.Size = new System.Drawing.Size(100, 26);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(106, 22);
            this.toolStripMenuItem5.Text = "Help";
            // 
            // connectionStringTextBox
            // 
            this.connectionStringTextBox.Location = new System.Drawing.Point(116, 19);
            this.connectionStringTextBox.MinimumSize = new System.Drawing.Size(725, 25);
            this.connectionStringTextBox.Name = "connectionStringTextBox";
            this.connectionStringTextBox.Size = new System.Drawing.Size(725, 20);
            this.connectionStringTextBox.TabIndex = 1;
            // 
            // connection
            // 
            this.connection.AutoSize = true;
            this.connection.ForeColor = System.Drawing.Color.Blue;
            this.connection.Location = new System.Drawing.Point(19, 22);
            this.connection.Name = "connection";
            this.connection.Size = new System.Drawing.Size(94, 13);
            this.connection.TabIndex = 0;
            this.connection.Text = "Connection String:";
            // 
            // databaseGroup
            // 
            this.databaseGroup.Controls.Add(this.connection);
            this.databaseGroup.Controls.Add(this.connectionStringTextBox);
            this.databaseGroup.Location = new System.Drawing.Point(15, 12);
            this.databaseGroup.Name = "databaseGroup";
            this.databaseGroup.Size = new System.Drawing.Size(886, 56);
            this.databaseGroup.TabIndex = 0;
            this.databaseGroup.TabStop = false;
            this.databaseGroup.Text = "Database Settings";
            // 
            // dataResult
            // 
            this.dataResult.AutoSize = true;
            this.dataResult.ForeColor = System.Drawing.Color.Blue;
            this.dataResult.Location = new System.Drawing.Point(489, 350);
            this.dataResult.Name = "dataResult";
            this.dataResult.Size = new System.Drawing.Size(71, 13);
            this.dataResult.TabIndex = 11;
            this.dataResult.Text = "Data Results:";
            // 
            // summary
            // 
            this.summary.AutoSize = true;
            this.summary.ForeColor = System.Drawing.Color.Blue;
            this.summary.Location = new System.Drawing.Point(9, 350);
            this.summary.Name = "summary";
            this.summary.Size = new System.Drawing.Size(91, 13);
            this.summary.TabIndex = 9;
            this.summary.Text = "Summary Results:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(15, 74);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(342, 148);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Steps to run Examples:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.Blue;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.MaximumSize = new System.Drawing.Size(325, 0);
            this.label1.MinimumSize = new System.Drawing.Size(325, 100);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(325, 130);
            this.label1.TabIndex = 15;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(982, 550);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.dataResult);
            this.Controls.Add(this.summary);
            this.Controls.Add(this.databaseGroup);
            this.Controls.Add(this.resultMessage);
            this.Controls.Add(this.dataGrid);
            this.Controls.Add(this.display);
            this.Controls.Add(this.createSession);
            this.Controls.Add(this.findAllSessions);
            this.Controls.Add(this.loadSession);
            this.Controls.Add(this.findByItems);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.Text = "SQL Race Examples in C#";
            this.createSessionHelp.ResumeLayout(false);
            this.loadSessionHelp.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.display)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
            this.FindAllSessionsHelp.ResumeLayout(false);
            this.findByItemHelp.ResumeLayout(false);
            this.databaseGroup.ResumeLayout(false);
            this.databaseGroup.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button loadSession;
        public System.Windows.Forms.Button createSession;
        public System.Windows.Forms.Label resultMessage;
        public System.Windows.Forms.DataGridView dataGrid;
        public System.Windows.Forms.Button findAllSessions;
        public System.Windows.Forms.Button findByItems;
        private System.Windows.Forms.TextBox connectionStringTextBox;
        private System.Windows.Forms.Label connection;
        private System.Windows.Forms.GroupBox databaseGroup;
        private System.Windows.Forms.Label dataResult;
        private System.Windows.Forms.Label summary;
        private System.Windows.Forms.ToolTip helpToolTip;
        private System.Windows.Forms.ContextMenuStrip createSessionHelp;
        private System.Windows.Forms.ToolStripMenuItem blahToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip loadSessionHelp;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ContextMenuStrip FindAllSessionsHelp;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ContextMenuStrip findByItemHelp;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataVisualization.Charting.Chart display;
    }
}