using System;
using System.IO;
using System.Windows.Forms;
//using System.Windows.Controls;

using DIGITC2_ENGINE ;
using NWaves.Signals ;

namespace Transgraphier_1_0_App
{

  public class MainWindowGUI : GUI
  {
    public MainWindowGUI( Form1 aMainWindow)
    { 
      mMainWindow = aMainWindow ; 
    }

    public override void AddMessage     ( string aMsg ) => mMainWindow.AddGeneralMessage(aMsg,true);
    public override void AddErrorMessage( string aMsg ) => mMainWindow.AddErrorMessage  (aMsg,true);
   
    Form1 mMainWindow; 
  }

  public class ZoomPanController
  {
    public double    MinSamplesPerPixel { get ; set ; }  
    public double    MaxSamplesPerPixel { get ; set ; }
    public WaveViews WaveViews          { get ; set ; }
    
    public double    SamplesPerPixel    { get ; private set ; }
    public double    StartSample        { get ; private set ; }

    public void UpdateSPP( double aSamplePerPixel )
    {
      SamplesPerPixel = aSamplePerPixel ;
      Invalidate();
    }

    public void UpdateSS( double aStartSample)
    {
      StartSample = aStartSample ;
      Invalidate();
    }

    public void Update( double aSamplePerPixel, double aStartSample)
    {
      SamplesPerPixel = aSamplePerPixel ;
      StartSample     = aStartSample ;
      Invalidate();
    }

    public void Invalidate()
    {
      WaveViews.Invalidate();
    }

  }


  public class WaveViews
  {
    public WaveViews()
    {
    }

    public void Clear() { mViews.Clear(); } 

    public void Add( WaveView aView) { mViews.Add( aView ); }

    public void Invalidate() { mViews.ForEach(v => v.InvalidateRender()); }

    public int Count => mViews.Count;

    List<WaveView> mViews = new List<WaveView>();
  }

  public class SessionResult
  {
    public string       FilterName          { get ; set ; } 
    public string       TextResultFileName  { get ; set ; }
    public List<string> WaveResultFileNames { get ; set ; } = new List<string>();
  }

  public partial class Form1 : Form
  {
    TabControl    mSessionsTabControl;
    Settings      mSettings  = null ;
    Config        mConfig    = null ;
    string        mConfigFile = null ;
    string        mInputFile = null ; 
    string        mSessionName = null ;
    MainWindowGUI mMWGUI     = null; 
    WaveViews     mWaveViews = new WaveViews();

    public Form1()
    {
      InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      mMWGUI = new MainWindowGUI(this); 

      mSettings = Settings.FromFile(SettingsFile);

      mSettings.Set("InputFolder" , InputFolder);  
      mSettings.Set("OutputFolder", OutputFolder);  

      if ( ! Directory.Exists( OutputFolder ))
        Directory.CreateDirectory( OutputFolder );  

      mConfig = LoadConfig();  

      AddGeneralMessage("Transgraphier 1.0 started.");
      AddGeneralMessage($"Input Folder: {InputFolder}.");
      AddGeneralMessage($"Output Folder: {OutputFolder}.");
      AddGeneralMessage($"Input WAV Samples Folder: {mSettings.GetPath("SamplesFolder") ?? InputFolder}.");

      if ( File.Exists($"{OutputFolder}\\LastSession.txt") )
        LoadLastSessionButton.Enabled = true; 
    }

    static string InputFolder  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Input");
    static string OutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
    static string SettingsFile = $"{InputFolder}\\Settings.txt";

    void SetupZoomPanController( DiscreteSignal aSignal)
    {
      var availableWidth = Math.Max(1.0, mInputWave.Width);

      var lZPC = new ZoomPanController(){ MinSamplesPerPixel = 2.0
                                        , MaxSamplesPerPixel = aSignal.Length / availableWidth
                                        , WaveViews = mWaveViews  
      };
          
      lZPC.Update(lZPC.MaxSamplesPerPixel,0 ) ; // Zoom out the entire signal

      mInputWave.ZoomPanController = lZPC; 
    }

    Config LoadConfig()
    {
      mConfigFile =$"{InputFolder}\\config.txt";

      var rConfig = Config.FromFile(mConfigFile); 

      return rConfig;  
    }

    Dictionary<string,string> GetParameters( string aFilter )
    {
      return mConfig.GetSection(aFilter).Map;
    }


    void LoadInputFile()
    {
      try
      {
        var lInputSignal = SignalLoader.LoadSignal(mInputFile);

        SetupZoomPanController(lInputSignal);
        mInputWave.Signal = lInputSignal;
        mInputWave.Title = "Input Signal";  
        mInputWave.Height = 300;
        mWaveViews.Clear();
        mWaveViews.Add( mInputWave );

        AddGeneralMessage($"Input Signal loaded: {mInputFile}");

        mSessionName = Path.GetFileNameWithoutExtension(mInputFile);
        this.sessionName.Text = mSessionName;
        this.sessionName.Enabled = true;
        this.processButton .Enabled = true; 
      }
      catch (Exception ex)
      {
        AddErrorMessage($"Failed to load Input Signal: {mInputFile}");
      }

    }

    void LoadEVP()
    {
      string lSamplesFolder = mSettings.GetPath("SamplesFolder") ?? InputFolder ;

      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.InitialDirectory = lSamplesFolder;
      openFileDialog.Filter = "Wave Files (*.wav)|*.wav";
      openFileDialog.Title = "Select a Wave File";
      openFileDialog.CheckFileExists = true;

      if (openFileDialog.ShowDialog() == DialogResult.OK)
      {
        mInputFile = openFileDialog.FileName;

        string lFolder = Path.GetDirectoryName(mInputFile);
        if ( lFolder != lSamplesFolder )
        {
          mSettings.Set("SamplesFolder", lFolder );  
          mSettings.Save(SettingsFile);
        }

        LoadInputFile();

        string lSessionName = Path.GetFileNameWithoutExtension(mInputFile);

        this.sessionName.Text = lSessionName; 

        mSessionName = lSessionName ;

        string lSessionFolder = $"{OutputFolder}\\{mSessionName}";  

        bool lSessionExists =  Directory.Exists( lSessionFolder );

        if ( lSessionExists )
        {
          AddGeneralMessage($"Session folder already exists: [{lSessionFolder}]");
          processButton.Text = "Re-Process";
        }

      }
    }

    void Process()
    {
      if ( mInputWave.Signal == null)
      {
        AddErrorMessage( "You must load an AUDIO file first.");
        return;
      }

      var lSession = new Session(mInputFile, mSessionName, mSettings, mMWGUI);

      DContext.Setup( lSession ) ;

      try
      {

        AddGeneralMessage("Processing started...");

        var lSignal = new WaveSignal(mInputWave.Signal) ;

        var lPipeline = PipelineFactory.FromAudioToBits_ByTapCode().Then( PipelineFactory.FromBits() ) ;

        var lResult = Processor.Process( lSession, mSettings, lSession.Name, lPipeline, mConfig, lSignal);

        File.Copy( mInputFile, $"{lSession.CurrentOutputFolder}\\{lSession.Name}.wav", true );

        lResult.Save()  ;

        AddGeneralMessage("Processing finished.");

        LoadLastSession();

        File.WriteAllText( $"{OutputFolder}\\LastSession.txt", mSessionName );

        this.ExportButton.Enabled = true;
        this.LoadLastSessionButton.Enabled = true;
      }
      catch (Exception ex)
      {
        AddErrorMessage($"Processing FAILED!!!: {ex}");
      }

      DContext.Shutdown(); 
    }

    void ShowSession( string aSessionFolder )
    {
      if ( ! Directory.Exists( aSessionFolder ) ) 
      {
        AddErrorMessage($"Could not found session folder: {aSessionFolder}");
        return ;
      }

      // Clear existing tabs
      mSessionsTabControl.TabPages.Clear();

      mSessionsTabControl.Name = Path.GetFileNameWithoutExtension(aSessionFolder);

      AddGeneralMessage( $"Loading Session: [{aSessionFolder}]");

      mInputFile = Directory.GetFiles(aSessionFolder, "*.wav").FirstOrDefault();

      if ( mInputFile == null )
      {
        AddErrorMessage($"Input file not found in session: [{aSessionFolder}]");
        return;
      }

      LoadInputFile();

      // Create a scrollable container for the tab content
      var lRootTab = new TabPage { Name = Path.GetFileNameWithoutExtension(aSessionFolder) };
      
      mSessionsTabControl.TabPages.Add(lRootTab);

      Panel scrollPanel = new Panel();
      scrollPanel.Dock = DockStyle.Fill;
      scrollPanel.AutoScroll = true;
      lRootTab.Controls.Add(scrollPanel);
      
      Panel contentPanel = new Panel();
      contentPanel.Dock = DockStyle.Top;
      contentPanel.Width = scrollPanel.Width;
      scrollPanel.Controls.Add(contentPanel);

      var lResultFolderSequence = new List<string>();
      var lCurrFolder = $"{aSessionFolder}\\Pipeline_0";

      string lMessageFile = $"{lCurrFolder}\\Message.txt";
      string lResultFile = $"{lCurrFolder}\\Result.txt"; 

      if ( File.Exists(lMessageFile))
      {
        var lMessage = File.ReadAllText(lMessageFile);  

        AddDecodedMessage(lMessage);
      }
      else
      {
        AddEmptyDecodedMessage();
      }

      if ( File.Exists(lResultFile))
      {
        var lMainResultText = File.ReadAllLines(lResultFile);
        foreach( var lRLine in lMainResultText )
          AddGeneralMessage(lRLine);
      }
      else
      {
        AddErrorMessage($"Main result file not found: {lResultFile}");
      }

      while (true)
      {
        lResultFolderSequence.Add(lCurrFolder);
        var children = Directory.GetDirectories(lCurrFolder).OrderBy(x => x).ToArray();
        if (children.Length == 1)
        {
          lCurrFolder = children[0];
          continue;
        }
        break;
      }

      List<SessionResult> lSessionResults = new List<SessionResult>();  

      foreach (var lResultFolder in lResultFolderSequence)
      {
        var lLocalTextResult  = Directory.GetFiles(lResultFolder, "*.txt").FirstOrDefault();
        if ( lLocalTextResult != null) 
        {  
          var lLocalWaveResults = Directory.GetFiles(lResultFolder, "*.wav");

          var lSessionResult = new SessionResult(){
            FilterName         = Path.GetFileNameWithoutExtension(lLocalTextResult),
            TextResultFileName = lLocalTextResult,
            WaveResultFileNames= lLocalWaveResults.ToList()
          };

          lSessionResults.Add( lSessionResult ); 
        }
      }

      int currentY = 0;

      foreach( var lSessionResult in lSessionResults )
      {
       string lTextResult = lSessionResult.TextResultFileName;

        // Create LexicalView for this file
        LexicalView lLexicalView = new LexicalView(this);
        lLexicalView.Location = new Point(0, currentY);
        lLexicalView.Width = scrollPanel.Width;
        lLexicalView.Height = 300;
        lLexicalView.Title = lSessionResult.FilterName;
        lLexicalView.Parameters = GetParameters(lSessionResult.FilterName);

        try
        {
          string textContent = File.ReadAllText(lTextResult);
          lLexicalView.TextContent = textContent;
          lLexicalView.Invalidate();
          lLexicalView.Refresh();
        }
        catch (Exception ex) 
        {
          AddErrorMessage($"Error reading text file {lTextResult}: {ex.Message}");
        }

        // Add lexical view to content panel
        contentPanel.Controls.Add(lLexicalView);
        currentY += lLexicalView.Height;

        foreach( var lWaveResult in lSessionResult.WaveResultFileNames)
        {
          string lWaveFilename = Path.GetFileNameWithoutExtension(lWaveResult);

          // Create WaveFormView for this file
          WaveView lWaveView = new WaveView();
          lWaveView.Location = new Point(0, currentY);
          lWaveView.Width = scrollPanel.Width;
          lWaveView.Height = 150;
          lWaveView.Title = lWaveFilename;

          var lResultSignal = SignalLoader.LoadSignal(lWaveResult);

          lWaveView.ZoomPanController = mInputWave.ZoomPanController ; 

          lWaveView.Signal = lResultSignal;

          // Add waveform view to content panel
          contentPanel.Controls.Add(lWaveView);
          currentY += lWaveView.Height;

          mWaveViews.Add(lWaveView);
        }

      } 

      // Set the content panel dimensions to accommodate all controls with full width
      contentPanel.Size = new Size(scrollPanel.Width, currentY);

      mWaveViews.Invalidate();
      AddGeneralMessage("Session Results loaded");

      this.ExportButton.Enabled = true ;

      this.Refresh();
    }

    string GetLastSessionFolder()
    {
      if (!Directory.Exists(OutputFolder))
      {
        AddErrorMessage($"Output folder not found: [{OutputFolder}]");
        return "";
      }

      var lLastSessionFile = $"{OutputFolder}\\LastSession.txt";
      if ( File.Exists(lLastSessionFile) )
      {
        var lLastSessionName = File.ReadAllText(lLastSessionFile);

        return $"{OutputFolder}\\{lLastSessionName}";
      }
      else
      {
        AddErrorMessage($"Last Session File not found: [{lLastSessionFile}]");
        return "";
      }

    }

    void LoadLastSession()
    {
      ShowSession(GetLastSessionFolder()); 
    }

    private void AddMessage(string aMessage, Color color, FontStyle style, bool aNewLine,  RichTextBox aTextBox )
    {
      aTextBox.SelectionStart = aTextBox.TextLength;
      aTextBox.SelectionLength = 0;
      aTextBox.SelectionColor = color;
      aTextBox.SelectionFont = new Font(aTextBox.Font, style);
      aTextBox.AppendText(aMessage + (aNewLine ? Environment.NewLine : ""));
      aTextBox.ScrollToCaret(); // This scrolls to bottom
      aTextBox.Invalidate();
      aTextBox.Refresh(); 
      resultsPanel.Invalidate();
      resultsPanel.Refresh ();
   }

    public void AddGeneralMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage( aMsg, Color.Black, FontStyle.Regular, aNewLine, logTextBox ) ;
    }

    public void AddErrorMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage("ERROR: " + aMsg, Color.Red, FontStyle.Bold, aNewLine, logTextBox ) ;
    }

    public void AddLogMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage( aMsg, Color.Blue, FontStyle.Regular, aNewLine, logTextBox ) ;
    }

    public void AddDecodedMessage( string aMsg, bool aNewLine = true )
    {
      AddMessage( "DECODED TEXT:" + Environment.NewLine, Color.Black, FontStyle.Regular, aNewLine, this.resultsTextBox ) ;

      AddMessage( aMsg, Color.Magenta, FontStyle.Bold, aNewLine, this.resultsTextBox ) ;
    }

    public void AddEmptyDecodedMessage()
    {
      AddMessage( ">>> NO MESSAGE COULD BE DECODED <<<", Color.DarkOliveGreen, FontStyle.Italic, true, this.resultsTextBox ) ;
    }

    public void AddNewLine()
    {
      AddMessage( "", Color.Black, FontStyle.Regular, true, logTextBox ) ;
    }


    public void ParametersChanged()
    {
      this.SaveNewParamsButton.Enabled     = true;
      this.RestoreOldParamsButton.Enabled  = true;
    }

    private void LoadEVP_Click(object sender, EventArgs e)
    {
      LoadEVP();
    }

    private void Process_Click(object sender, EventArgs e)
    {
      Process();
    }

    private void LoadLastSession_Click(object sender, EventArgs e)
    {
      LoadLastSession();
    }

    private void LoadSession_Click(object sender, EventArgs e)
    {
      // Get all session folders
      var sessionFolders = Directory.GetDirectories(OutputFolder).ToList().ConvertAll( s => Path.GetFileNameWithoutExtension(s) ).ToArray() ;

      if (sessionFolders.Length == 0)
      {
        AddErrorMessage("No sessions found in the output folder.");
        return;
      }

      // Create a simple selection dialog
      using (Form dialog = new Form())
      {
        dialog.Text = "Select Session";
        dialog.Width = 600;
        dialog.Height = 660;
        dialog.StartPosition = FormStartPosition.CenterParent;

        ListBox listBox = new ListBox();
        listBox.Dock = DockStyle.Top;
        listBox.Height = 530;
        listBox.Items.AddRange(sessionFolders);

        Button okButton = new Button();
        okButton.Text = "OK";
        okButton.Height = 70;
        okButton.Dock = DockStyle.Top;
        okButton.DialogResult = DialogResult.OK;

        dialog.Controls.Add(okButton);
        dialog.Controls.Add(listBox);
        dialog.AcceptButton = okButton;

        if (dialog.ShowDialog(this) == DialogResult.OK && listBox.SelectedItem != null)
        {
          string selectedSession = listBox.SelectedItem.ToString();
          ShowSession( $"{OutputFolder}\\{selectedSession}" );
        }
      }
    }

    private void Import_Click(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void Export_Click(object sender, EventArgs e)
    {
      try
      {
        string lastSessionFolder = GetLastSessionFolder();
        if (string.IsNullOrEmpty(lastSessionFolder) || !Directory.Exists(lastSessionFolder))
        {
          AddErrorMessage("No valid last session folder found to export.");
          return;
        }

        string sessionName = Path.GetFileName(lastSessionFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string zipPath = Path.Combine(OutputFolder, sessionName + ".zip");

        // Delete existing zip if present
        if (File.Exists(zipPath))
          File.Delete(zipPath);

        // Create the zip file, including the folder itself
        System.IO.Compression.ZipFile.CreateFromDirectory(
            lastSessionFolder,
            zipPath,
            System.IO.Compression.CompressionLevel.Optimal,
            includeBaseDirectory: true
        );

        AddGeneralMessage($"Exported session to: {zipPath}");
      }
      catch (Exception ex)
      {
        AddErrorMessage($"Export failed: {ex.Message}");
      }
    }


    private void RestoreOldParameters_Click(object sender, EventArgs e)
    {
      File.Copy(mConfigFile + ".backup", mConfigFile, true);
      LoadConfig();
    }

    private void SaveNewParameters_Click(object sender, EventArgs e)
    {
      File.Copy(mConfigFile, mConfigFile + ".backup", true);
      mConfig.Save(mConfigFile);
    }

  }

}
