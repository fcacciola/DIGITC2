using System;
using System.Diagnostics;
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

  public class ViewController
  {
    public double MinSamplesPerPixel { get ; set ; }  
    public double MaxSamplesPerPixel { get ; set ; }
    public int    Length             { get ; set ; }
    public double SamplesPerPixel    { get ; private set ; }
    public double PanStartSample     { get ; private set ; }

    public ControlledViews Views          { get ; set ; }

    public MeasureTimeTool MeasureTimeTool { get; set; } = null;

    public void UpdateSPP( double aSamplePerPixel )
    {
      SamplesPerPixel = aSamplePerPixel ;
      Invalidate();
    }

    public void UpdateSS( double aPanStartSample)
    {
      PanStartSample = aPanStartSample ;
      Invalidate();
    }

    public void Update( double aSamplePerPixel, double aPanStartSample)
    {
      SamplesPerPixel = aSamplePerPixel ;
      PanStartSample  = aPanStartSample ;
      Invalidate();
    }

    public void Invalidate()
    {
      Views.Invalidate();
    }

  }


  public class ControlledViews
  {
    public ControlledViews()
    {
    }

    public void Clear() { mViews.Clear(); } 

    public void Add( ControlledView aView) { mViews.Add( aView ); }

    public void Invalidate() { mViews.ForEach(v => v.InvalidateRender()); }

    public int Count => mViews.Count;

    List<ControlledView> mViews = new List<ControlledView>();
  }

  public partial class Form1 : Form
  {
    TabControl    mSessionsTabControl;
    Settings      mSettings  = null ;
    Config        mConfig    = null ;
    string        mConfigFile = null ;
    string        mInputFile = null ; 
    string        mSessionName = null ;
    string        mSessionFolder = null ; 
    MainWindowGUI mMWGUI     = null; 
    ControlledViews     mControlledViews = new ControlledViews();
    MeasureTimeTool mMeasureTimeTool = new MeasureTimeTool();

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

      // Setup measurement tool callback
      mMeasureTimeTool.OnSelectionChangedCallback = (tool) =>
      {
        UpdateTimeMeasureLabel();
      };
    }

    private void UpdateTimeMeasureLabel()
    {
      if (mMeasureTimeTool.HasSelection && mInputWave.Signal != null)
      {
        string formattedTime = mMeasureTimeTool.GetFormattedDuration(SIG.SamplingRate);
        
        this.TimeMeasureLabel.Text = formattedTime;
      }
    }

    static string InputFolder  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Varanormal\\Transgraphier\\Input");
    static string OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Varanormal\\Transgraphier\\Output");
    static string AppInputFolder = Path.Combine(AppContext.BaseDirectory,"Input");
    static string SettingsFile = $"{InputFolder}\\Settings.txt";

    void SetupZoomPanController( DiscreteSignal aSignal)
    {
      var availableWidth = Math.Max(1.0, mInputWave.Width);

      var lZPC = new ViewController(){ MinSamplesPerPixel = 2.0
                                        , MaxSamplesPerPixel = aSignal.Length / availableWidth
                                        , Views = mControlledViews  
      };
          
      lZPC.Update(lZPC.MaxSamplesPerPixel,0 ) ; // Zoom out the entire signal
      lZPC.Length = aSignal.Length;
      mInputWave.ViewController = lZPC; 
      mInputWave.ViewController.MeasureTimeTool = mMeasureTimeTool; 
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
        mSessionName = Path.GetFileNameWithoutExtension(mInputFile);

        mControlledViews.Clear();

        if ( Path.GetExtension(mInputFile) == ".txt" )
        {
          mInputLexicalSignal = new FileSignal(mInputFile);
        }
        else
        {
          var lInputSignal = SignalLoader.LoadSignal(mInputFile);

          SetupZoomPanController(lInputSignal);
          mInputWave.Signal = lInputSignal;
          mInputWave.Title = mSessionName ;
          mControlledViews.Add( mInputWave );
        }

        AddGeneralMessage($"Input Signal loaded: {mInputFile}");

        this.sessionName.Text = mSessionName;
        this.sessionName.Enabled = true;
        this.processButton.Enabled = true; 
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
      openFileDialog.Filter = "Wave Files (*.wav)|*.wav|Lexical Text Files (*.txt)|*.txt";
      openFileDialog.Title = "Select a File";
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

        mSessionFolder = $"{OutputFolder}\\{mSessionName}";  

        Clear();

        bool lSessionExists =  Directory.Exists( mSessionFolder );

        if ( lSessionExists )
        {
          AddGeneralMessage($"Session folder already exists: [{mSessionFolder}]");

          LoadSession();
        }
      }
    }

    void Clear()
    {
      ClearDecodedMessage();

      mSessionsTabControl.TabPages.Clear();

      mSessionsTabControl.Name = Path.GetFileNameWithoutExtension(mSessionFolder);

    }

    void RUN()
    {
      Clear();

      if ( mInputWave?.Signal == null && mInputLexicalSignal == null )
      {
        AddErrorMessage( "You must load an AUDIO or LEXICAL file first.");
        return;
      }

      var lSession = new Session(mInputFile, mSessionName, mSettings, mMWGUI);

      DContext.Setup( lSession ) ;

      try
      {

        mSessionFolder = lSession.CurrentOutputFolder ;

        AddGeneralMessage( $"Processing started. Session Folder: {mSessionFolder}. ");

        MainPipeline lPipeline = null;
        Signal       lStartSignal = null;

        if ( mInputWave?.Signal != null )
        {
          lStartSignal = new WaveSignal(mInputWave.Signal) ;

          lPipeline = PipelineFactory.FromAudioToTapCode().Then(PipelineFactory.FromTapCode()) ;
        }
        else
        {
          lStartSignal = mInputLexicalSignal;
          lPipeline = PipelineFactory.FromTapCode() ;
        }

        var lResult = Processor.Process( lSession, mSettings, lSession.Name, lPipeline, mConfig, lStartSignal);

        string lInputCopy = $"{lSession.CurrentOutputFolder}\\{lSession.Name}.wav";
        if ( ! File.Exists(lInputCopy) )
        {
          try
          {
            File.Copy( mInputFile, lInputCopy, true );
          }
          catch( Exception e )
          {
            AddErrorMessage(e.Message);
          }
        }

        lResult.Save()  ;

        AddGeneralMessage("...finished.");

        File.WriteAllText( $"{OutputFolder}\\LastSession.txt", mSessionName );

        LoadSession();
      }
      catch (Exception ex)
      {
        AddErrorMessage($"Processing FAILED!!!: {ex}");
      }

      DContext.Shutdown(); 
    }

    public class PipelineOutcome 
    {
      public PipelineOutcome( string aCurrFolder ) 
      { 
        mList.Add(aCurrFolder) ;  
      }

      public void Add( string aFolder ) 
      { 
        string lNew = $"{mList.Last()}\\{aFolder}";
        mList.Add(lNew) ; 
      }

      public PipelineOutcome Copy() => new PipelineOutcome(mList);

      PipelineOutcome( List<string> aList )
      {
        mList.AddRange(aList); 
      }

      public string Name { get ; set ; }

      public IEnumerable<string> Folders => mList ;

      public string LastFolder() => mList.Last() ;

      List<string> mList = new List<string>();
    }

    public class FilterOutcome
    {
      public string       FilterName          { get ; set ; } 
      public string       TextResultFileName  { get ; set ; }
      public List<string> WaveResultFileNames { get ; set ; } = new List<string>();
      public List<string> Summary             { get ; set ; } = new List<string>();
      public string       Message             { get ; set ; }
      public string       TimelineFileName    { get ; set ; } 

      public bool IsEmpty => string.IsNullOrEmpty(TextResultFileName) && WaveResultFileNames.Count == 0 && Summary.Count == 0 && string.IsNullOrEmpty(Message);  
    }

    List<PipelineOutcome>  mPipelineOutcomeList = new List<PipelineOutcome>();
    Stack<PipelineOutcome> mPipelineOutcomeStack = new Stack<PipelineOutcome>();

    void LoadSession()
    {
      if ( ! Directory.Exists( mSessionFolder ) ) 
      {
        AddErrorMessage($"Could not found session folder: {mSessionFolder}");
        return ;
      }

      AddGeneralMessage( $"Loading Session: [{mSessionFolder}]");

      if ( mInputFile == null )
      {
        mInputFile = Directory.GetFiles(mSessionFolder, "*.wav").FirstOrDefault();

        if ( mInputFile == null )
        {
          AddErrorMessage($"Input file not found in session: [{mSessionFolder}]");
          return;
        }

        LoadInputFile();
      }

      mPipelineOutcomeList .Clear();
      mPipelineOutcomeStack.Clear();

      var lHeadPipelineOutcome = new PipelineOutcome($"{mSessionFolder}\\Main");
      lHeadPipelineOutcome.Name = "Main";

      mPipelineOutcomeList .Add (lHeadPipelineOutcome);
      mPipelineOutcomeStack.Push(lHeadPipelineOutcome);

      while ( mPipelineOutcomeStack.Count > 0 ) 
      {
        var lCurrPipelineOutcome = mPipelineOutcomeStack.Pop(); 

        var lCurrFolder = lCurrPipelineOutcome.LastFolder();

        do
        {
          var lSubFolders = Directory.GetDirectories(lCurrFolder).ToList().ConvertAll( sf => Path.GetFileNameWithoutExtension(sf) ) ;

          // This is to make sure we process the Pipeline subfolder BEFORE the next subfolder

          List<string> lNewPipelines   = new List<string>();
          List<string> lNextSubFolders = new List<string>();

          foreach( var lSubFolder in lSubFolders )
          {
            if ( lSubFolder.StartsWith("Main") )
                 lNewPipelines  .Add( lSubFolder );
            else lNextSubFolders.Add( lSubFolder );
          }

          foreach( var lNewPipelineSubFolder in lNewPipelines )
          {
            var lNewPipelineOutcome = lCurrPipelineOutcome.Copy();
            lNewPipelineOutcome.Name = lNewPipelineSubFolder;
            lNewPipelineOutcome.Add(lNewPipelineSubFolder);
            mPipelineOutcomeList .Add (lNewPipelineOutcome) ;
            mPipelineOutcomeStack.Push(lNewPipelineOutcome);
          }
          
          if ( lNextSubFolders.Count == 1 )
          {
            lCurrPipelineOutcome.Add(lNextSubFolders[0]);
          }
          else break ;

          lCurrFolder = lCurrPipelineOutcome.LastFolder() ;
        }
        while ( true ) ;
      }

      foreach( var lPipelineFolder in mPipelineOutcomeList.OrderBy( p => p.Name ) )
      {
        LoadPipeline( lPipelineFolder );
      }

      if ( resultsTextBox.Text.Length == 0 )
      {
        AddEmptyDecodedMessage();
      }
    }


    void LoadPipeline( PipelineOutcome aPipelineOutcome )
    {
      // Create a scrollable container for the tab content
      var lRootTab = new TabPage { Name = aPipelineOutcome.Name, Text = aPipelineOutcome.Name };
      
      mSessionsTabControl.TabPages.Add(lRootTab);

      Panel scrollPanel = new Panel();
      scrollPanel.Dock = DockStyle.Fill;
      scrollPanel.AutoScroll = true;
      lRootTab.Controls.Add(scrollPanel);
      
      Panel contentPanel = new Panel();
      contentPanel.Dock = DockStyle.Top;
      contentPanel.Width = scrollPanel.Width;
      scrollPanel.Controls.Add(contentPanel);

      List<FilterOutcome> lFilterOutcomes = new List<FilterOutcome>();

      foreach (var lFolder in aPipelineOutcome.Folders )
      {
        string lMessageFile  = $"{lFolder}\\Message.txt";
        string lResultFile   = $"{lFolder}\\Result.txt"; 
        string lTimelineFile = $"{lFolder}\\Timeline.json";

        var lFilterOutcome = new FilterOutcome();

        if ( File.Exists(lMessageFile))
          lFilterOutcome.Message = File.ReadAllText(lMessageFile);  

        if ( File.Exists(lResultFile))
        {
          var lMainResultText = File.ReadAllLines(lResultFile);
          foreach( var lRLine in lMainResultText )
            lFilterOutcome.Summary.Add(lRLine);
        }

        var lLocalTextResult  = Directory.GetFiles(lFolder, "*.txt").FirstOrDefault();
        if ( lLocalTextResult != null) 
        {  
          lFilterOutcome.FilterName = Path.GetFileNameWithoutExtension(lLocalTextResult);
          lFilterOutcome.TextResultFileName = lLocalTextResult;
        }

        lFilterOutcome.WaveResultFileNames.AddRange( Directory.GetFiles(lFolder, "*.wav") ) ;

        if ( File.Exists(lTimelineFile))
          lFilterOutcome.TimelineFileName = lTimelineFile;

        if ( ! lFilterOutcome.IsEmpty )
          lFilterOutcomes.Add( lFilterOutcome ); 

      }

      int currentY = 0;

      foreach( var lFilterOutcome in lFilterOutcomes )
      {
        if ( ! string.IsNullOrEmpty(lFilterOutcome.Message) )
          AddDecodedMessage( aPipelineOutcome.Name, lFilterOutcome.Message );

        lFilterOutcome.Summary.ForEach( s => AddGeneralMessage(s) ) ;

        lFilterOutcome.WaveResultFileNames.Sort();

        foreach( var lWaveResult in lFilterOutcome.WaveResultFileNames)
        {
          string lWaveFilename = Path.GetFileNameWithoutExtension(lWaveResult);

          if ( lWaveFilename.Contains('_') )
            lWaveFilename = lWaveFilename.Split('_')[1];

          bool lColorCoded = lWaveFilename.Contains("ColorCoded") ;

          // Create WaveFormView for this file
          WaveView lWaveView = new WaveView(false, lColorCoded);
          lWaveView.Location = new Point(0, currentY);
          lWaveView.Width = scrollPanel.Width;
          lWaveView.Height = 150;
          lWaveView.Title = lWaveFilename;

          var lResultSignal = SignalLoader.LoadSignal(lWaveResult);

          lWaveView.ViewController = mInputWave.ViewController ; 

          lWaveView.Signal = lResultSignal;

          // Add waveform view to content panel
          contentPanel.Controls.Add(lWaveView);
          currentY += lWaveView.Height;

          mControlledViews.Add(lWaveView);
        }
      } 

      var lTlFile = lFilterOutcomes.Select(fo => fo.TimelineFileName).FirstOrDefault(tf => !string.IsNullOrEmpty(tf));
      if (!string.IsNullOrEmpty(lTlFile))
      {
        try
        {
          var lTimeline = Timeline.Load(lTlFile);
          TimelineView lTimelineView = new TimelineView();
          lTimelineView.Location = new Point(0, currentY);
          lTimelineView.Width = scrollPanel.Width;
          lTimelineView.Height = 150;
          lTimelineView.Title = "Timeline";
          lTimelineView.Timeline = lTimeline;
          lTimelineView.ViewController = mInputWave.ViewController;
          mControlledViews.Add(lTimelineView);
          contentPanel.Controls.Add(lTimelineView);
          currentY += lTimelineView.Height;
        }
        catch (Exception ex)
        {
          AddErrorMessage($"Error loading timeline from {lTlFile}: {ex.Message}");
        }
      }

      // DISABLE the LexicalViews for now.
      if ( false )
      {
        foreach ( var lFilterOutcome in lFilterOutcomes )
        {
          string lTextResult = lFilterOutcome.TextResultFileName;

          // Create LexicalView for this file
          LexicalView lLexicalView = new LexicalView(this);
          lLexicalView.Location = new Point(0, currentY);
          lLexicalView.Width = scrollPanel.Width;
          lLexicalView.Height = 300;
          lLexicalView.Title = lFilterOutcome.FilterName;
          lLexicalView.Parameters = GetParameters(lFilterOutcome.FilterName);

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
        } 
      }

      // Set the content panel dimensions to accommodate all controls with full width
      contentPanel.Size = new Size(scrollPanel.Width, currentY);

      mControlledViews.Invalidate();
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
      mInputFile = null ;
      mSessionFolder = GetLastSessionFolder();
      mSessionName = Path.GetFileNameWithoutExtension(mSessionFolder);
      Clear();
      LoadSession(); 
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

    public void ClearDecodedMessage()
    {
      this.resultsTextBox.Clear();
    }

    public void AddDecodedMessage( string aPipelineName, string aMsg, bool aNewLine = true )
    {
      AddMessage( $"DECODED TEXT from {aPipelineName}:{Environment.NewLine}" , Color.Black, FontStyle.Regular, aNewLine, this.resultsTextBox ) ;

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
      RUN();
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
          mSessionFolder =  $"{OutputFolder}\\{selectedSession}";
          Clear();
          LoadSession();
        }
      }
    }

    private void Import_Click(object sender, EventArgs e)
    {
    }

    private void Measure_Click(object sender, EventArgs e)
    {
      ClearMeasurementDisplay();

      // Toggle measurement tool on/off
      mMeasureTimeTool.IsActive = !mMeasureTimeTool.IsActive;
      mMeasureTimeTool.Reset();
    }

    private void ClearMeasurementDisplay()
    {
      this.TimeMeasureLabel.Text = "";
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

    private void Help_Click(object sender, EventArgs e)
    {
      // Put your PDF in your app folder (or another known location)
      string lPdfPath = Path.Combine(InputFolder, "Manual.pdf");

      try
      {
        var psi = new ProcessStartInfo
        {
          FileName = lPdfPath,
          UseShellExecute = true,  // key line: use OS file association
          Verb = "open"
        };

        Process.Start(psi);
      }
      catch (Exception ex)
      {
        AddErrorMessage($"Could not open the manual PDF.\n\n{ex.Message}");
      }
    }

  }

}
