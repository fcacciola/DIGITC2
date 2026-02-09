

namespace Transgraphier_1_0_App
{
  partial class Form1
  {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      buttonPanel = new Panel();
      loadEVPButton = new Button();
      sessionLabel = new Label();
      sessionName = new TextBox();
      processButton = new Button();
      SaveNewParamsButton = new Button();
      RestoreOldParamsButton = new Button();
      LoadLastSessionButton = new Button();
      LoadSessionButton = new Button();
      ExportButton = new Button();
      ImportButton = new Button();
      HelpButton = new Button();
      statusTextBox = new TextBox();
      statusPanel = new Panel();
      resultsTextBox = new RichTextBox();
      logTextBox = new RichTextBox();
      resultsPanel = new Panel();
      mInputWave = new WaveView();
      buttonPanel.SuspendLayout();
      SuspendLayout();

      // 
      // buttonPanel
      // 
      buttonPanel.Controls.Add(HelpButton);
      buttonPanel.Controls.Add(ImportButton);
      buttonPanel.Controls.Add(ExportButton);
      buttonPanel.Controls.Add(LoadSessionButton);
      buttonPanel.Controls.Add(LoadLastSessionButton);
      buttonPanel.Controls.Add(RestoreOldParamsButton);
      buttonPanel.Controls.Add(SaveNewParamsButton);
      buttonPanel.Controls.Add(processButton);
      buttonPanel.Controls.Add(sessionName);
      buttonPanel.Controls.Add(sessionLabel);
      buttonPanel.Controls.Add(loadEVPButton);
      buttonPanel.Dock = DockStyle.Top;
      buttonPanel.Location = new Point(0, 0);
      buttonPanel.Name = "buttonPanel";
      buttonPanel.Size = new Size(1920, 50);
      buttonPanel.TabIndex = 1;

      // 
      // loadButton
      // 
      loadEVPButton.Dock = DockStyle.Left;
      loadEVPButton.Name = "loadButton";
      loadEVPButton.Size = new Size(100, 30);
      loadEVPButton.TabIndex = 0;
      loadEVPButton.Text = "Load EVP";
      loadEVPButton.Click += LoadEVP_Click;

      // mTitle
      sessionLabel.Text = "Session";
      sessionLabel.Dock = DockStyle.Left;
      sessionLabel.ForeColor = Color.Black;
      sessionLabel.TextAlign = ContentAlignment.MiddleLeft;
      sessionLabel.Padding = new Padding(5, 0, 0, 0);
      sessionLabel.Size = new Size(100, 30);

      // 
      // statusTextBox
      // 
      sessionName.Dock = DockStyle.Left;
      sessionName.BorderStyle = BorderStyle.None;
      sessionName.Multiline = false;
      sessionName.Name = "sessionName";
      sessionName.ReadOnly = false;
      sessionName.ScrollBars = ScrollBars.None;
      sessionName.Size = new Size(450, 40);
      sessionName.TabIndex = 0;
      sessionName.Enabled = false ;

      // 
      // processButton
      // 
      processButton.Dock = DockStyle.Left;
      processButton.Name = "processButton";
      processButton.Size = new Size(150, 30);
      processButton.TabIndex = 1;
      processButton.Text = "Process";
      processButton.Click += Process_Click;
      processButton.Enabled = false;
      // 
      // processButton
      // 
      SaveNewParamsButton.Dock = DockStyle.Left;
      SaveNewParamsButton.Name = "SaveNewParamsButton";
      SaveNewParamsButton.Size = new Size(200, 30);
      SaveNewParamsButton.TabIndex = 1;
      SaveNewParamsButton.Text = "Save New Parameters";
      SaveNewParamsButton.Click += SaveNewParameters_Click;
      SaveNewParamsButton.Enabled = false;
      // 
      // processButton
      // 
      RestoreOldParamsButton.Dock = DockStyle.Left;
      RestoreOldParamsButton.Name = "RestoreOldParamsButton";
      RestoreOldParamsButton.Size = new Size(220, 30);
      RestoreOldParamsButton.TabIndex = 1;
      RestoreOldParamsButton.Text = "Restore Old Parameters";
      RestoreOldParamsButton.Click += RestoreOldParameters_Click;
      RestoreOldParamsButton.Enabled = false;
      // 
      // showButton
      // 
      LoadLastSessionButton.Dock = DockStyle.Left;
      LoadLastSessionButton.Name = "LoadLastSessionButton";
      LoadLastSessionButton.Size = new Size(200, 30);
      LoadLastSessionButton.TabIndex = 2;
      LoadLastSessionButton.Text = "Load Last Session";
      LoadLastSessionButton.Click += LoadLastSession_Click;
      LoadLastSessionButton.Enabled = false;
      // 
      // showButton
      // 
      LoadSessionButton.Dock = DockStyle.Left;
      LoadSessionButton.Name = "LoadSessionButton";
      LoadSessionButton.Size = new Size(200, 30);
      LoadSessionButton.TabIndex = 2;
      LoadSessionButton.Text = "Load Session";
      LoadSessionButton.Click += LoadSession_Click;
      // 
      // showButton
      // 
      ExportButton.Dock = DockStyle.Left;
      ExportButton.Name = "ExportButton";
      ExportButton.Size = new Size(200, 30);
      ExportButton.TabIndex = 2;
      ExportButton.Text = "Export Session";
      ExportButton.Click += Export_Click;
      ExportButton.Enabled = false;
      // 
      // showButton
      // 
      ImportButton.Dock = DockStyle.Left;
      ImportButton.Name = "ImportButton";
      ImportButton.Size = new Size(200, 30);
      ImportButton.TabIndex = 2;
      ImportButton.Text = "Import Session";
      ImportButton.Click += Import_Click;
      // 
      // showButton
      // 
      HelpButton.Dock = DockStyle.Left;
      HelpButton.Name = "HelpButton";
      HelpButton.Size = new Size(200, 30);
      HelpButton.TabIndex = 2;
      HelpButton.Text = "Help";
      HelpButton.Click += Help_Click;
      // 
      // resultsPanel
      // 
      resultsPanel.BackColor = Color.Black;
      resultsPanel.Controls.Add(resultsTextBox);
      resultsPanel.Controls.Add(logTextBox);
      resultsPanel.Dock = DockStyle.Top;
      resultsPanel.Location = new Point(0, 50);
      resultsPanel.Name = "resultsPanel";
      resultsPanel.Padding = new Padding(2);
      resultsPanel.Size = new Size(1920, 300);
      resultsPanel.TabIndex = 3;
      // 
      // logTextBox
      // 
      logTextBox.Dock = DockStyle.Left;
      logTextBox.Name = "logTextBox";
      logTextBox.ReadOnly = true;
      logTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
      logTextBox.Size = new Size(2000, 300);
      logTextBox.TabIndex = 1;
      logTextBox.Text = "";
      // 
      // resultsTextBox
      // 
      resultsTextBox.Dock = DockStyle.Fill;
      resultsTextBox.Name = "resultsTextBox";
      resultsTextBox.ReadOnly = true;
      resultsTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
      //            resultsTextBox.Size = new Size(500, 300);
      resultsTextBox.Height = 300;
      resultsTextBox.TabIndex = 1;
      resultsTextBox.Text = "";
      // 
      // inputWave
      // 
      mInputWave.Dock = DockStyle.Top;
      mInputWave.Location = new Point(8, 350);
      mInputWave.Name = "inputWave";
      mInputWave.Size = new Size(1920, 300);
      mInputWave.TabIndex = 4;
      mInputWave.Title = "";
      // 
      // statusPanel
      // 
      statusPanel.BackColor = Color.Black;
      statusPanel.Controls.Add(statusTextBox);
      statusPanel.Dock = DockStyle.Bottom;
      statusPanel.Location = new Point(0, 980);
      statusPanel.Name = "statusPanel";
      statusPanel.Padding = new Padding(2);
      statusPanel.Size = new Size(1920, 100);
      statusPanel.TabIndex = 2;
      // 
      // statusTextBox
      // 
      statusTextBox.Dock = DockStyle.Fill;
      statusTextBox.Location = new Point(2, 2);
      statusTextBox.Multiline = true;
      statusTextBox.Name = "statusTextBox";
      statusTextBox.ReadOnly = true;
      statusTextBox.ScrollBars = ScrollBars.Vertical;
      statusTextBox.Size = new Size(1916, 96);
      statusTextBox.TabIndex = 0;

      mSessionsTabControl = new TabControl();
      mSessionsTabControl.Dock = DockStyle.Fill;
      mSessionsTabControl.Appearance = TabAppearance.Buttons;
      mSessionsTabControl.Padding = new Point(12, 6);
      mSessionsTabControl.SizeMode = TabSizeMode.Normal;
      mSessionsTabControl.ItemSize = new Size(20, 40);

      // Insert the tab control in the middle (between results panel and input wave)
      Controls.Add(mSessionsTabControl);
      //Controls.SetChildIndex(mSessionsTabControl, this.Controls.GetChildIndex(mInputWave));

      // 
      // Form1
      // 
      AutoScaleDimensions = new SizeF(10F, 25F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(1920, 1080);
      Controls.Add(statusPanel);
      Controls.Add(mInputWave);
      Controls.Add(resultsPanel);
      Controls.Add(buttonPanel);
      Name = "Form1";
      Text = "Transgraphier 1.0";
      WindowState = FormWindowState.Maximized;
      buttonPanel.ResumeLayout(false);
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Panel buttonPanel;
    private Button loadEVPButton;
    private Label sessionLabel;
    private TextBox sessionName;
    private Button processButton;
    private Button SaveNewParamsButton;
    private Button RestoreOldParamsButton;
    private Button LoadLastSessionButton;
    private Button LoadSessionButton;
    private Button ExportButton;
    private Button ImportButton;
    private Button HelpButton;
    private Panel statusPanel;
    private TextBox statusTextBox;
    private Panel resultsPanel;
    private RichTextBox resultsTextBox;
    private RichTextBox logTextBox;
    private WaveView mInputWave;
  }
}
